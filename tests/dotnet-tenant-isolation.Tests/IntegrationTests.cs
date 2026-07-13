#nullable enable

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using TenantIsolation.Constants;
using TenantIsolation.Data;
using TenantIsolation.Models;
using TenantIsolation.Services;
using Xunit;

namespace TenantIsolation.Tests;

/// <summary>
/// Integration tests demonstrating real-world scenarios and workflows
/// </summary>
public class IntegrationTests : IAsyncLifetime
{
    private readonly string _databaseName = $"IntegrationTests_{Guid.NewGuid()}";
    private readonly TenantDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly TenantRepository _tenantRepository;
    private readonly ConfigurationService _configService;
    private readonly Mock<IDynamicTenantStore> _mockTenantStore;
    private readonly TenantService _tenantService;
    private readonly Mock<ILogger<TenantService>> _mockTenantServiceLogger;
    private readonly Mock<ILogger<ConfigurationService>> _mockConfigServiceLogger;

    /// <summary>
    /// Creates a fresh <see cref="TenantDbContext"/> pointed at this test's shared in-memory
    /// database. EF Core's DbContext is not thread-safe, so concurrency tests must give each
    /// logical operation its own context instance rather than sharing <see cref="_dbContext"/>.
    /// </summary>
    private TenantDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;
        return new TenantDbContext(options);
    }

    public IntegrationTests()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        _dbContext = new TenantDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _tenantRepository = new TenantRepository(new InMemoryTenantDbContextFactory(_dbContext));
        _configService = new ConfigurationService(_dbContext, _cache, new Mock<ILogger<ConfigurationService>>().Object);
        _mockTenantStore = new Mock<IDynamicTenantStore>(MockBehavior.Strict);
        _mockTenantServiceLogger = new Mock<ILogger<TenantService>>();
        _mockConfigServiceLogger = new Mock<ILogger<ConfigurationService>>();

        _tenantService = new TenantService(_tenantRepository, _mockTenantStore.Object, _mockTenantServiceLogger.Object);
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        _cache.Dispose();
        await _dbContext.DisposeAsync();
    }

    #region End-to-End Tenant Lifecycle Tests

    [Fact]
    public async Task TenantLifecycle_CreateUpdateDeleteRestore_WorksEndToEnd()
    {
        // Arrange & Act - Create tenant
        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(new List<Tenant>());

        var createdTenant = await _tenantService.CreateTenantAsync(
            "E2E Test Corp",
            "e2e-test-corp",
            "admin@e2etest.com",
            TenantIsolationStrategy.DatabasePerTenant);

        createdTenant.Status.Should().Be(TenantStatus.Provisioning);

        // Act - Activate tenant
        // The dynamic tenant store fronts the same underlying data as the repository, so the
        // mock must hand back the very entity instance already tracked by the DbContext rather
        // than a freshly constructed one with the same Id - otherwise EF sees two different
        // instances with the same key and throws when the repository later attaches/updates it.
        var activeTenant = (await _tenantRepository.GetByIdAsync(createdTenant.Id))!;
        activeTenant.Status = TenantStatus.Active;
        activeTenant.IsDeleted = false;
        activeTenant.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(365);

        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(createdTenant.Id))
            .ReturnsAsync(activeTenant);

        var activated = await _tenantService.ActivateTenantAsync(createdTenant.Id);
        activated.Should().BeTrue();

        // Act - Update tenant
        var updated = await _tenantService.UpdateTenantAsync(createdTenant.Id, t => t.Name = "Updated Corp");
        updated.Name.Should().Be("Updated Corp");

        // Act - Delete tenant (soft delete)
        var deleted = await _tenantService.DeleteTenantAsync(createdTenant.Id);
        deleted.Should().BeTrue();

        var deletedTenant = await _tenantRepository.GetByIdAsync(createdTenant.Id);
        deletedTenant.Should().NotBeNull();
        deletedTenant!.IsDeleted.Should().BeTrue();
        deletedTenant.Status.Should().Be(TenantStatus.Archived);

        // Act - Restore tenant
        deletedTenant.Restore();
        await _tenantRepository.UpdateAsync(deletedTenant);

        var restoredTenant = await _tenantRepository.GetByIdAsync(createdTenant.Id);
        restoredTenant!.IsDeleted.Should().BeFalse();
    }

    #endregion

    #region Multi-Tenant Configuration Management Tests

    [Fact]
    public async Task MultiTenantConfiguration_IsolatedPerTenant_WorksCorrectly()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        // Act - Set different configurations for each tenant
        await _configService.SetConfigurationAsync(tenant1Id, "database-url", "db://tenant1");
        await _configService.SetConfigurationAsync(tenant1Id, "api-key", "key-tenant1");

        await _configService.SetConfigurationAsync(tenant2Id, "database-url", "db://tenant2");
        await _configService.SetConfigurationAsync(tenant2Id, "api-key", "key-tenant2");

        // Assert - Configurations are isolated
        var t1Config = await _configService.GetConfigurationAsync(tenant1Id, "database-url");
        t1Config!.Value.Should().Be("db://tenant1");

        var t2Config = await _configService.GetConfigurationAsync(tenant2Id, "database-url");
        t2Config!.Value.Should().Be("db://tenant2");

        // Verify tenant2 configurations don't include tenant1's data
        var t1AllConfigs = await _configService.GetAllConfigurationsAsync(tenant1Id);
        t1AllConfigs.Should().NotContainKey("tenant2-key");

        var t2AllConfigs = await _configService.GetAllConfigurationsAsync(tenant2Id);
        t2AllConfigs.Should().NotContainKey("tenant1-key");
    }

    #endregion

    #region Subscription Expiration Management Tests

    [Fact]
    public async Task SubscriptionManagement_ExpiringSubscriptionDetection_WorksCorrectly()
    {
        // Arrange - Create tenants with different subscription states
        var expiringTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Expiring Tenant",
            Slug = "expiring",
            AdminEmail = "admin@expiring.com",
            Status = TenantStatus.Active,
            IsDeleted = false,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(10)
        };

        var validTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Valid Tenant",
            Slug = "valid",
            AdminEmail = "admin@valid.com",
            Status = TenantStatus.Active,
            IsDeleted = false,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(100)
        };

        var expiredTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Expired Tenant",
            Slug = "expired",
            AdminEmail = "admin@expired.com",
            Status = TenantStatus.Active,
            IsDeleted = false,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        _dbContext.Tenants.AddRange(expiringTenant, validTenant, expiredTenant);
        await _dbContext.SaveChangesAsync();

        // IsSubscriptionValidAsync resolves the tenant through the dynamic tenant store, not the
        // repository directly, so the store mock needs to know about these seeded tenants too.
        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(validTenant.Id)).ReturnsAsync(validTenant);
        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(expiredTenant.Id)).ReturnsAsync(expiredTenant);

        // Act
        var expiringInThirtyDays = await _tenantService.GetExpiringSubscriptionsAsync(30);
        var subscriptionValid = await _tenantService.IsSubscriptionValidAsync(validTenant.Id);
        var subscriptionInvalid = await _tenantService.IsSubscriptionValidAsync(expiredTenant.Id);

        // Assert
        expiringInThirtyDays.Should().HaveCount(1);
        expiringInThirtyDays[0].Id.Should().Be(expiringTenant.Id);

        subscriptionValid.Should().BeTrue();
        subscriptionInvalid.Should().BeFalse();
    }

    #endregion

    #region Trial to Paid Conversion Tests

    [Fact]
    public async Task TrialConversion_PromoteTrialToActive_WorksCorrectly()
    {
        // Arrange
        var trialTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Trial Tenant",
            Slug = "trial-tenant",
            AdminEmail = "admin@trial.com",
            Status = TenantStatus.Trial,
            IsDeleted = false,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tenants.Add(trialTenant);
        await _dbContext.SaveChangesAsync();

        // UpdateTenantAsync resolves the tenant through the dynamic tenant store, not the
        // repository directly, so the store mock needs to return the same tracked instance.
        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(trialTenant.Id)).ReturnsAsync(trialTenant);

        // Act - Simulate trial conversion to paid
        var updated = await _tenantService.UpdateTenantAsync(trialTenant.Id, t =>
        {
            t.Status = TenantStatus.Active;
            t.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(365);
        });

        // Assert
        updated.Status.Should().Be(TenantStatus.Active);
        updated.SubscriptionExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(360));
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ConcurrentConfigurationUpdates_MultipleTenants_AllSucceed()
    {
        // Arrange
        var tenantIds = Enumerable.Range(0, 10)
            .Select(_ => Guid.NewGuid())
            .ToList();

        // Act - Simulate concurrent writes
        var tasks = tenantIds.SelectMany((tenantId, index) =>
            Enumerable.Range(0, 5).Select(i =>
                _configService.SetConfigurationAsync(
                    tenantId,
                    $"key-{i}",
                    $"value-{index}-{i}")))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert - Verify all configurations were written
        foreach (var tenantId in tenantIds)
        {
            var configs = await _configService.GetAllConfigurationsAsync(tenantId);
            configs.Should().HaveCount(5);
        }
    }

    [Fact]
    public async Task ConcurrentTenantCreation_MultipleThreads_AllSucceed()
    {
        // Arrange
        var createdTenants = new ConcurrentBag<Tenant>();

        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(new List<Tenant>());

        // Act - Create tenants concurrently.
        // Each task gets its own DbContext/repository/service instance (all pointed at the
        // same in-memory database) because EF Core's DbContext is not safe for concurrent use
        // from multiple threads - sharing one instance here would throw spuriously.
        var tasks = Enumerable.Range(0, 10)
            .Select(i => Task.Run(async () =>
            {
                using var dbContext = CreateDbContext();
                var repository = new TenantRepository(new InMemoryTenantDbContextFactory(dbContext));
                var service = new TenantService(repository, _mockTenantStore.Object, _mockTenantServiceLogger.Object);

                var tenant = await service.CreateTenantAsync(
                    $"Concurrent Tenant {i}",
                    $"concurrent-tenant-{i}",
                    $"admin{i}@concurrent.com");

                createdTenants.Add(tenant);
            }))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert
        createdTenants.Should().HaveCount(10);
        createdTenants.Select(t => t.Slug).Distinct().Should().HaveCount(10);
    }

    [Fact]
    public async Task ConcurrentCacheAccess_SameConfiguration_DoesNotCauseRaceCondition()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        const string key = "shared-config";
        const string value = "shared-value";

        await _configService.SetConfigurationAsync(tenantId, key, value);

        // Act - Read same configuration concurrently
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => _configService.GetConfigurationAsync(tenantId, key))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - All reads should return consistent value
        results.Should().AllSatisfy(config =>
        {
            config.Should().NotBeNull();
            config!.Value.Should().Be(value);
        });
    }

    #endregion

    #region Data Filtering and Querying Tests

    [Fact]
    public async Task TenantQuerying_VariousFilters_ReturnCorrectResults()
    {
        // Arrange - Create diverse set of tenants
        var activeTenants = Enumerable.Range(0, 5)
            .Select(i => new Tenant
            {
                Id = Guid.NewGuid(),
                Name = $"Active Tenant {i}",
                Slug = $"active-{i}",
                AdminEmail = $"admin{i}@active.com",
                Status = TenantStatus.Active,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            })
            .ToList();

        var trialTenants = Enumerable.Range(0, 3)
            .Select(i => new Tenant
            {
                Id = Guid.NewGuid(),
                Name = $"Trial Tenant {i}",
                Slug = $"trial-{i}",
                AdminEmail = $"admin{i}@trial.com",
                Status = TenantStatus.Trial,
                IsDeleted = false,
                SubscriptionExpiresAt = DateTime.UtcNow.AddDays(7)
            })
            .ToList();

        _dbContext.Tenants.AddRange(activeTenants.Concat(trialTenants));
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var allActive = await _tenantRepository.GetActiveTenantAsync();
        allActive.Should().HaveCount(5);

        var trials = await _tenantRepository.GetTrialTenantsAsync();
        trials.Should().HaveCount(3);

        var byStatus = await _tenantRepository.GetByStatusAsync(TenantStatus.Active);
        byStatus.Should().HaveCount(5);

        var statusCounts = await _tenantRepository.GetStatusCountsAsync();
        statusCounts[TenantStatus.Active].Should().Be(5);
        statusCounts[TenantStatus.Trial].Should().Be(3);
    }

    [Fact]
    public async Task TenantSearch_ByNameSlugAndEmail_FindsCorrectTenants()
    {
        // Arrange
        var searchTenants = new[]
        {
            new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "ACME Corporation",
                Slug = "acme-corp",
                AdminEmail = "admin@acme.com",
                Status = TenantStatus.Active,
                IsDeleted = false
            },
            new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Tech Innovations",
                Slug = "tech-innovations",
                AdminEmail = "admin@tech.com",
                Status = TenantStatus.Active,
                IsDeleted = false
            }
        };

        _dbContext.Tenants.AddRange(searchTenants);
        await _dbContext.SaveChangesAsync();

        // Act & Assert - Search by name
        var byName = await _tenantRepository.SearchAsync("ACME");
        byName.Should().HaveCount(1);
        byName[0].Name.Should().Be("ACME Corporation");

        // Act & Assert - Search by slug
        var bySlug = await _tenantRepository.SearchAsync("tech");
        bySlug.Should().HaveCount(1);
        bySlug[0].Slug.Should().Contain("tech");

        // Act & Assert - Search by email
        var byEmail = await _tenantRepository.SearchAsync("@acme");
        byEmail.Should().HaveCount(1);
        byEmail[0].AdminEmail.Should().Be("admin@acme.com");
    }

    #endregion

    #region Configuration Import/Export Tests

    [Fact]
    public async Task ConfigurationImportExport_RoundTrip_PreservesData()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await _configService.SetConfigurationAsync(tenantId, "api-endpoint", "https://api.example.com");
        await _configService.SetConfigurationAsync(tenantId, "api-timeout", "30");
        await _configService.SetConfigurationAsync(tenantId, "feature-flag", "true");

        // Act - Export configurations
        var exported = await _configService.ExportConfigurationAsync(tenantId);

        // Delete all configurations
        await _configService.DeleteConfigurationAsync(tenantId, "api-endpoint");
        await _configService.DeleteConfigurationAsync(tenantId, "api-timeout");
        await _configService.DeleteConfigurationAsync(tenantId, "feature-flag");

        var beforeImport = await _configService.GetAllConfigurationsAsync(tenantId);
        beforeImport.Should().BeEmpty();

        // Act - Import configurations back
        var importedCount = await _configService.ImportConfigurationAsync(tenantId, exported);

        // Assert
        importedCount.Should().Be(3);

        var afterImport = await _configService.GetAllConfigurationsAsync(tenantId);
        afterImport.Should().HaveCount(3);
        afterImport["api-endpoint"].Should().Be("https://api.example.com");
        afterImport["api-timeout"].Should().Be("30");
    }

    #endregion

    #region Tenant Status Lifecycle Tests

    [Fact]
    public async Task TenantStatusTransitions_SuspendAndReactivate_WorksCorrectly()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Status Transition Tenant",
            Slug = "status-transition",
            AdminEmail = "admin@status.com",
            Status = TenantStatus.Active,
            IsDeleted = false,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(365)
        };

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act - Suspend tenant
        var suspended = await _tenantRepository.SuspendTenantAsync(tenant.Id, "Overdue payment");

        // Assert - Verify suspended
        suspended.Should().BeTrue();
        var suspendedTenant = await _tenantRepository.GetByIdAsync(tenant.Id);
        suspendedTenant!.Status.Should().Be(TenantStatus.Suspended);

        // Act - Reactivate tenant
        var reactivated = await _tenantRepository.ActivateTenantAsync(tenant.Id);

        // Assert
        reactivated.Should().BeTrue();
        var reactivatedTenant = await _tenantRepository.GetByIdAsync(tenant.Id);
        reactivatedTenant!.Status.Should().Be(TenantStatus.Active);
    }

    #endregion

    #region Inactive Tenant Detection Tests

    [Fact]
    public async Task InactiveTenantDetection_FindsTenantsNotUpdatedRecently()
    {
        // Arrange
        var recentlyActiveTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Recently Active",
            Slug = "recent",
            AdminEmail = "admin@recent.com",
            Status = TenantStatus.Active,
            IsDeleted = false,
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var inactiveTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Inactive",
            Slug = "inactive",
            AdminEmail = "admin@inactive.com",
            Status = TenantStatus.Active,
            IsDeleted = false,
            UpdatedAt = DateTime.UtcNow.AddDays(-120)
        };

        _dbContext.Tenants.AddRange(recentlyActiveTenant, inactiveTenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var inactive = await _tenantRepository.GetInactiveTenantsAsync(90);

        // Assert
        inactive.Should().HaveCount(1);
        inactive[0].Id.Should().Be(inactiveTenant.Id);
    }

    #endregion
}
