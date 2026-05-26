#nullable enable

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TenantIsolation.Constants;
using TenantIsolation.Data;
using TenantIsolation.Models;
using Xunit;

namespace TenantIsolation.Tests;

public class TenantRepositoryTests : IAsyncLifetime
{
    private readonly TenantDbContext _dbContext;
    private readonly TenantRepository _repository;

    public TenantRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase($"TenantRepositoryTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TenantDbContext(options);
        _repository = new TenantRepository(new InMemoryTenantDbContextFactory(_dbContext));
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    #region GetBySlugAsync Tests

    [Fact]
    public async Task GetBySlugAsync_WithValidSlug_ReturnsTenant()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Slug = "test-tenant",
            AdminEmail = "admin@test.com",
            Status = TenantStatus.Active,
            IsDeleted = false
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySlugAsync("test-tenant");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(tenant.Id);
        result.Slug.Should().Be("test-tenant");
    }

    [Fact]
    public async Task GetBySlugAsync_WithDeletedTenant_ReturnsNull()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Tenant",
            Slug = "deleted-tenant",
            AdminEmail = "admin@test.com",
            Status = TenantStatus.Active,
            IsDeleted = true
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySlugAsync("deleted-tenant");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySlugAsync_WithInactiveStatus_ReturnsNull()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Suspended Tenant",
            Slug = "suspended-tenant",
            AdminEmail = "admin@test.com",
            Status = TenantStatus.Suspended,
            IsDeleted = false
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySlugAsync("suspended-tenant");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetBySlugAsync_WithNullOrWhitespaceSlug_ThrowsArgumentException(string? slug)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.GetBySlugAsync(slug!));

        ex.ParamName.Should().Be("slug");
    }

    [Fact]
    public async Task GetBySlugAsync_WithNonExistentSlug_ReturnsNull()
    {
        // Act
        var result = await _repository.GetBySlugAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetActiveTenantAsync Tests

    [Fact]
    public async Task GetActiveTenantAsync_ReturnsOnlyActiveTenants()
    {
        // Arrange
        var activeTenant1 = CreateTenant("Active 1", "active-1", TenantStatus.Active);
        var activeTenant2 = CreateTenant("Active 2", "active-2", TenantStatus.Active);
        var suspendedTenant = CreateTenant("Suspended", "suspended", TenantStatus.Suspended);

        _dbContext.Tenants.AddRange(activeTenant1, activeTenant2, suspendedTenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveTenantAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.Status.Should().Be(TenantStatus.Active));
        result.Should().Contain(t => t.Id == activeTenant1.Id);
        result.Should().Contain(t => t.Id == activeTenant2.Id);
    }

    [Fact]
    public async Task GetActiveTenantAsync_ExcludesDeletedTenants()
    {
        // Arrange
        var activeTenant = CreateTenant("Active", "active", TenantStatus.Active, false);
        var deletedTenant = CreateTenant("Deleted", "deleted", TenantStatus.Active, true);

        _dbContext.Tenants.AddRange(activeTenant, deletedTenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveTenantAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveTenantAsync_OrdersByName()
    {
        // Arrange
        var tenant1 = CreateTenant("Zebra Corp", "zebra", TenantStatus.Active);
        var tenant2 = CreateTenant("Apple Inc", "apple", TenantStatus.Active);
        var tenant3 = CreateTenant("Middle Co", "middle", TenantStatus.Active);

        _dbContext.Tenants.AddRange(tenant1, tenant2, tenant3);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveTenantAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Apple Inc");
        result[1].Name.Should().Be("Middle Co");
        result[2].Name.Should().Be("Zebra Corp");
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_WithValidStatus_ReturnsTenants()
    {
        // Arrange
        var trial1 = CreateTenant("Trial 1", "trial-1", TenantStatus.Trial);
        var trial2 = CreateTenant("Trial 2", "trial-2", TenantStatus.Trial);
        var active = CreateTenant("Active", "active", TenantStatus.Active);

        _dbContext.Tenants.AddRange(trial1, trial2, active);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(TenantStatus.Trial);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.Status.Should().Be(TenantStatus.Trial));
    }

    [Fact]
    public async Task GetByStatusAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var older = CreateTenant("Older", "older", TenantStatus.Trial);
        older.CreatedAt = DateTime.UtcNow.AddDays(-10);

        var newer = CreateTenant("Newer", "newer", TenantStatus.Trial);
        newer.CreatedAt = DateTime.UtcNow.AddDays(-1);

        _dbContext.Tenants.AddRange(older, newer);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(TenantStatus.Trial);

        // Assert
        result.Should().HaveCount(2);
        result[0].CreatedAt.Should().BeGreaterThan(result[1].CreatedAt);
    }

    #endregion

    #region GetTrialTenantsAsync Tests

    [Fact]
    public async Task GetTrialTenantsAsync_ReturnsOnlyTrialTenants()
    {
        // Arrange
        var trial = CreateTenant("Trial", "trial", TenantStatus.Trial);
        var active = CreateTenant("Active", "active", TenantStatus.Active);

        _dbContext.Tenants.AddRange(trial, active);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetTrialTenantsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(TenantStatus.Trial);
    }

    #endregion

    #region GetExpiringSubscriptionsAsync Tests

    [Fact]
    public async Task GetExpiringSubscriptionsAsync_ReturnsTenantsExpiringWithin30Days()
    {
        // Arrange
        var expiringToday = CreateTenant("Expiring Today", "today", TenantStatus.Active);
        expiringToday.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(1);

        var expiringIn30Days = CreateTenant("Expiring In 30", "in-30", TenantStatus.Active);
        expiringIn30Days.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(29);

        var expiredYesterday = CreateTenant("Expired", "expired", TenantStatus.Active);
        expiredYesterday.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(-1);

        var expiringInFuture = CreateTenant("Expiring Later", "later", TenantStatus.Active);
        expiringInFuture.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(35);

        _dbContext.Tenants.AddRange(expiringToday, expiringIn30Days, expiredYesterday, expiringInFuture);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetExpiringSubscriptionsAsync(30);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Id == expiringToday.Id);
        result.Should().Contain(t => t.Id == expiringIn30Days.Id);
    }

    [Fact]
    public async Task GetExpiringSubscriptionsAsync_WithCustomDays_UsesCustomThreshold()
    {
        // Arrange
        var expiringIn7Days = CreateTenant("Expiring In 7", "in-7", TenantStatus.Active);
        expiringIn7Days.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(5);

        var expiringIn15Days = CreateTenant("Expiring In 15", "in-15", TenantStatus.Active);
        expiringIn15Days.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(12);

        _dbContext.Tenants.AddRange(expiringIn7Days, expiringIn15Days);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetExpiringSubscriptionsAsync(7);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(expiringIn7Days.Id);
    }

    [Fact]
    public async Task GetExpiringSubscriptionsAsync_ExcludesAlreadyExpired()
    {
        // Arrange
        var expiredYesterday = CreateTenant("Expired", "expired", TenantStatus.Active);
        expiredYesterday.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(-1);

        _dbContext.Tenants.Add(expiredYesterday);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetExpiringSubscriptionsAsync(30);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpiringSubscriptionsAsync_ExcludesDeletedTenants()
    {
        // Arrange
        var expiringDeleted = CreateTenant("Expiring Deleted", "expiring-deleted", TenantStatus.Active, true);
        expiringDeleted.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(15);

        _dbContext.Tenants.Add(expiringDeleted);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetExpiringSubscriptionsAsync(30);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpiringSubscriptionsAsync_WithNegativeDays_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _repository.GetExpiringSubscriptionsAsync(-1));

        ex.ParamName.Should().Be("daysUntilExpiry");
    }

    [Fact]
    public async Task GetExpiringSubscriptionsAsync_OrdersBySubscriptionExpiryAsc()
    {
        // Arrange
        var expiring10 = CreateTenant("Expiring In 10", "10", TenantStatus.Active);
        expiring10.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(10);

        var expiring5 = CreateTenant("Expiring In 5", "5", TenantStatus.Active);
        expiring5.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(5);

        _dbContext.Tenants.AddRange(expiring10, expiring5);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetExpiringSubscriptionsAsync(30);

        // Assert
        result.Should().HaveCount(2);
        result[0].SubscriptionExpiresAt.Should().BeLessThan(result[1].SubscriptionExpiresAt);
    }

    #endregion

    #region GetRecentlyCreatedAsync Tests

    [Fact]
    public async Task GetRecentlyCreatedAsync_ReturnsTenantsCreatedWithinDays()
    {
        // Arrange
        var recentTenant = CreateTenant("Recent", "recent", TenantStatus.Active);
        recentTenant.CreatedAt = DateTime.UtcNow.AddDays(-1);

        var oldTenant = CreateTenant("Old", "old", TenantStatus.Active);
        oldTenant.CreatedAt = DateTime.UtcNow.AddDays(-30);

        _dbContext.Tenants.AddRange(recentTenant, oldTenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetRecentlyCreatedAsync(7);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(recentTenant.Id);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithNameMatch_ReturnsTenant()
    {
        // Arrange
        var tenant = CreateTenant("ACME Corporation", "acme", TenantStatus.Active);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync("acme");

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(tenant.Id);
    }

    [Fact]
    public async Task SearchAsync_WithSlugMatch_ReturnsTenant()
    {
        // Arrange
        var tenant = CreateTenant("Test", "test-corp", TenantStatus.Active);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync("corp");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_WithEmailMatch_ReturnsTenant()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Slug = "test",
            AdminEmail = "admin@acme.com",
            Status = TenantStatus.Active,
            IsDeleted = false
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync("admin@acme");

        // Assert
        result.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_WithNullOrWhitespaceQuery_ReturnsEmptyList(string? query)
    {
        // Act
        var result = await _repository.SearchAsync(query!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive()
    {
        // Arrange
        var tenant = CreateTenant("TestCorp", "testcorp", TenantStatus.Active);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync("TESTCORP");

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetWithDetailsAsync Tests

    [Fact]
    public async Task GetWithDetailsAsync_WithValidId_ReturnsTenant()
    {
        // Arrange
        var tenant = CreateTenant("Test", "test", TenantStatus.Active);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithDetailsAsync(tenant.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(tenant.Id);
    }

    [Fact]
    public async Task GetWithDetailsAsync_WithDeletedTenant_ReturnsNull()
    {
        // Arrange
        var tenant = CreateTenant("Test", "test", TenantStatus.Active, true);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithDetailsAsync(tenant.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetStatusCountsAsync Tests

    [Fact]
    public async Task GetStatusCountsAsync_ReturnsCountByStatus()
    {
        // Arrange
        _dbContext.Tenants.AddRange(
            CreateTenant("Active 1", "active-1", TenantStatus.Active),
            CreateTenant("Active 2", "active-2", TenantStatus.Active),
            CreateTenant("Trial 1", "trial-1", TenantStatus.Trial),
            CreateTenant("Suspended 1", "suspended-1", TenantStatus.Suspended)
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetStatusCountsAsync();

        // Assert
        result.Should().HaveCount(3);
        result[TenantStatus.Active].Should().Be(2);
        result[TenantStatus.Trial].Should().Be(1);
        result[TenantStatus.Suspended].Should().Be(1);
    }

    [Fact]
    public async Task GetStatusCountsAsync_ExcludesDeletedTenants()
    {
        // Arrange
        _dbContext.Tenants.AddRange(
            CreateTenant("Active", "active", TenantStatus.Active),
            CreateTenant("Deleted Active", "deleted", TenantStatus.Active, true)
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetStatusCountsAsync();

        // Assert
        result[TenantStatus.Active].Should().Be(1);
    }

    #endregion

    #region IsSlugUniqueAsync Tests

    [Fact]
    public async Task IsSlugUniqueAsync_WithUniqueSlug_ReturnsTrue()
    {
        // Act
        var result = await _repository.IsSlugUniqueAsync("unique-slug");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSlugUniqueAsync_WithExistingSlug_ReturnsFalse()
    {
        // Arrange
        var tenant = CreateTenant("Test", "existing-slug", TenantStatus.Active);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.IsSlugUniqueAsync("existing-slug");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSlugUniqueAsync_WithExcludeId_AllowsSameSlugForExcludedId()
    {
        // Arrange
        var tenant = CreateTenant("Test", "slug", TenantStatus.Active);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.IsSlugUniqueAsync("slug", tenant.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSlugUniqueAsync_IgnoresDeletedTenants()
    {
        // Arrange
        var tenant = CreateTenant("Test", "slug", TenantStatus.Active, true);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.IsSlugUniqueAsync("slug");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsSlugUniqueAsync_WithNullOrWhitespaceSlug_ThrowsArgumentException(string? slug)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _repository.IsSlugUniqueAsync(slug!));

        ex.ParamName.Should().Be("slug");
    }

    #endregion

    #region GetInactiveTenantsAsync Tests

    [Fact]
    public async Task GetInactiveTenantsAsync_ReturnsTenantsNotUpdatedInXDays()
    {
        // Arrange
        var inactiveTenant = CreateTenant("Inactive", "inactive", TenantStatus.Active);
        inactiveTenant.UpdatedAt = DateTime.UtcNow.AddDays(-100);

        var activeTenant = CreateTenant("Active", "active", TenantStatus.Active);
        activeTenant.UpdatedAt = DateTime.UtcNow.AddDays(-1);

        _dbContext.Tenants.AddRange(inactiveTenant, activeTenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetInactiveTenantsAsync(90);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(inactiveTenant.Id);
    }

    #endregion

    #region ActivateTenantAsync Tests

    [Fact]
    public async Task ActivateTenantAsync_WithValidTenant_ActivatesSuccessfully()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Provisioning Tenant",
            Slug = "provisioning",
            AdminEmail = "admin@test.com",
            Status = TenantStatus.Provisioning,
            IsDeleted = false,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.ActivateTenantAsync(tenant.Id);

        // Assert
        result.Should().BeTrue();

        var updatedTenant = await _repository.GetByIdAsync(tenant.Id);
        updatedTenant.Should().NotBeNull();
        updatedTenant!.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public async Task ActivateTenantAsync_WithNonExistentTenant_ReturnsFalse()
    {
        // Act
        var result = await _repository.ActivateTenantAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SuspendTenantAsync Tests

    [Fact]
    public async Task SuspendTenantAsync_WithValidTenant_SuspendsSuccessfully()
    {
        // Arrange
        var tenant = CreateTenant("Test", "test", TenantStatus.Active);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.SuspendTenantAsync(tenant.Id, "Payment failed");

        // Assert
        result.Should().BeTrue();

        var suspendedTenant = await _repository.GetByIdAsync(tenant.Id);
        suspendedTenant.Should().NotBeNull();
        suspendedTenant!.Status.Should().Be(TenantStatus.Suspended);
    }

    [Fact]
    public async Task SuspendTenantAsync_WithNonExistentTenant_ReturnsFalse()
    {
        // Act
        var result = await _repository.SuspendTenantAsync(Guid.NewGuid(), "Reason");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetBillingSummaryAsync Tests

    [Fact]
    public async Task GetBillingSummaryAsync_ReturnsBillingData()
    {
        // Arrange
        var tenant1 = CreateTenant("Tenant 1", "tenant-1", TenantStatus.Active);
        tenant1.PlanId = "plan-pro";

        var tenant2 = CreateTenant("Tenant 2", "tenant-2", TenantStatus.Active);
        tenant2.PlanId = "plan-pro";

        var tenant3 = CreateTenant("Tenant 3", "tenant-3", TenantStatus.Trial);
        tenant3.PlanId = "plan-free";

        _dbContext.Tenants.AddRange(tenant1, tenant2, tenant3);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetBillingSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        var list = ((IEnumerable<dynamic>)result).ToList();
        list.Should().HaveCount(2);
    }

    #endregion

    #region Helper Methods

    private Tenant CreateTenant(string name, string slug, TenantStatus status, bool isDeleted = false)
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            AdminEmail = $"admin@{slug}.com",
            Status = status,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}

/// <summary>
/// In-memory implementation for testing purposes
/// </summary>
public class InMemoryTenantDbContextFactory : ITenantDbContextFactory<TenantDbContext>
{
    private readonly TenantDbContext _context;

    public InMemoryTenantDbContextFactory(TenantDbContext context)
    {
        _context = context;
    }

    public TenantDbContext CreateDbContext()
    {
        return _context;
    }
}
