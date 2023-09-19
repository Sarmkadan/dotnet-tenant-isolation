#nullable enable

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Constants;
using TenantIsolation.Data;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;
using TenantIsolation.Services;
using Xunit;

namespace TenantIsolation.Tests;

public class TenantServiceTests
{
    private readonly Mock<TenantRepository> _mockTenantRepository;
    private readonly Mock<IDynamicTenantStore> _mockDynamicTenantStore;
    private readonly Mock<ILogger<TenantService>> _mockLogger;
    private readonly TenantService _sut;

    public TenantServiceTests()
    {
        _mockTenantRepository = new Mock<TenantRepository>(MockBehavior.Strict,
            new Mock<ITenantDbContextFactory<TenantDbContext>>().Object);
        _mockDynamicTenantStore = new Mock<IDynamicTenantStore>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<TenantService>>();

        _sut = new TenantService(_mockTenantRepository.Object, _mockDynamicTenantStore.Object, _mockLogger.Object);
    }

    #region CreateTenantAsync Tests

    [Fact]
    public async Task CreateTenantAsync_WithValidInput_CreatesAndReturnsTenant()
    {
        // Arrange
        const string name = "Test Tenant";
        const string slug = "test-tenant";
        const string adminEmail = "admin@example.com";

        _mockTenantRepository.Setup(r => r.IsSlugUniqueAsync(slug))
            .ReturnsAsync(true);

        _mockTenantRepository.Setup(r => r.AddAsync(It.IsAny<Tenant>()))
            .ReturnsAsync((Tenant t) => t);

        // Act
        var result = await _sut.CreateTenantAsync(name, slug, adminEmail);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Slug.Should().Be(slug.ToLower());
        result.AdminEmail.Should().Be(adminEmail);
        result.Status.Should().Be(TenantStatus.Provisioning);
        result.IsolationStrategy.Should().Be(TenantIsolationStrategy.DatabasePerTenant);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _mockTenantRepository.Verify(r => r.IsSlugUniqueAsync(slug), Times.Once);
        _mockTenantRepository.Verify(r => r.AddAsync(It.IsAny<Tenant>()), Times.Once);
    }

    [Fact]
    public async Task CreateTenantAsync_WithCustomStrategy_UsesProvidedStrategy()
    {
        // Arrange
        const string name = "Test Tenant";
        const string slug = "test-tenant";
        const string adminEmail = "admin@example.com";
        const TenantIsolationStrategy strategy = TenantIsolationStrategy.SchemaPerTenant;

        _mockTenantRepository.Setup(r => r.IsSlugUniqueAsync(slug))
            .ReturnsAsync(true);

        _mockTenantRepository.Setup(r => r.AddAsync(It.IsAny<Tenant>()))
            .ReturnsAsync((Tenant t) => t);

        // Act
        var result = await _sut.CreateTenantAsync(name, slug, adminEmail, strategy);

        // Assert
        result.IsolationStrategy.Should().Be(strategy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTenantAsync_WithNullOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateTenantAsync(invalidName, "slug", "admin@example.com"));

        ex.ParamName.Should().Be("name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTenantAsync_WithNullOrWhitespaceSlug_ThrowsArgumentException(string invalidSlug)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateTenantAsync("Test Tenant", invalidSlug, "admin@example.com"));

        ex.ParamName.Should().Be("slug");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CreateTenantAsync_WithNullOrWhitespaceEmail_ThrowsArgumentNullException(string invalidEmail)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.CreateTenantAsync("Test Tenant", "slug", invalidEmail));

        ex.ParamName.Should().Be("adminEmail");
    }

    [Fact]
    public async Task CreateTenantAsync_WithDuplicateSlug_ThrowsTenantIsolationException()
    {
        // Arrange
        const string slug = "existing-slug";

        _mockTenantRepository.Setup(r => r.IsSlugUniqueAsync(slug))
            .ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantIsolationException>(
            () => _sut.CreateTenantAsync("Test Tenant", slug, "admin@example.com"));

        ex.Message.Should().Contain("already in use");
    }

    [Fact]
    public async Task CreateTenantAsync_WhenRepositoryThrows_WrapsInTenantIsolationException()
    {
        // Arrange
        _mockTenantRepository.Setup(r => r.IsSlugUniqueAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        _mockTenantRepository.Setup(r => r.AddAsync(It.IsAny<Tenant>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantIsolationException>(
            () => _sut.CreateTenantAsync("Test Tenant", "slug", "admin@example.com"));

        ex.Message.Should().Contain("Failed to create tenant");
        ex.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateTenantAsync_WithMixedCaseSlug_ConvertsToLowercase()
    {
        // Arrange
        const string mixedCaseSlug = "Test-TENANT-Slug";

        _mockTenantRepository.Setup(r => r.IsSlugUniqueAsync(mixedCaseSlug))
            .ReturnsAsync(true);

        _mockTenantRepository.Setup(r => r.AddAsync(It.IsAny<Tenant>()))
            .ReturnsAsync((Tenant t) => t);

        // Act
        var result = await _sut.CreateTenantAsync("Test Tenant", mixedCaseSlug, "admin@example.com");

        // Assert
        result.Slug.Should().Be(mixedCaseSlug.ToLower());
    }

    #endregion

    #region GetTenantAsync Tests

    [Fact]
    public async Task GetTenantAsync_WithValidId_ReturnsTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant" };

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _sut.GetTenantAsync(tenantId);

        // Assert
        result.Should().Be(tenant);
        _mockDynamicTenantStore.Verify(s => s.GetTenantByIdAsync(tenantId), Times.Once);
    }

    [Fact]
    public async Task GetTenantAsync_WithNonExistentId_ThrowsTenantNotResolvedException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantNotResolvedException>(
            () => _sut.GetTenantAsync(tenantId));

        ex.Message.Should().Contain(tenantId.ToString());
    }

    #endregion

    #region GetTenantBySlugAsync Tests

    [Fact]
    public async Task GetTenantBySlugAsync_WithValidSlug_ReturnsTenant()
    {
        // Arrange
        const string slug = "test-tenant";
        var tenant = new Tenant { Id = Guid.NewGuid(), Slug = slug };
        var activeTenants = new List<Tenant> { tenant };

        _mockDynamicTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(activeTenants);

        // Act
        var result = await _sut.GetTenantBySlugAsync(slug);

        // Assert
        result.Should().Be(tenant);
    }

    [Fact]
    public async Task GetTenantBySlugAsync_WithCaseMismatch_ReturnsTenant()
    {
        // Arrange
        const string slug = "Test-Tenant";
        var tenant = new Tenant { Id = Guid.NewGuid(), Slug = "test-tenant" };
        var activeTenants = new List<Tenant> { tenant };

        _mockDynamicTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(activeTenants);

        // Act
        var result = await _sut.GetTenantBySlugAsync(slug);

        // Assert
        result.Should().Be(tenant);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetTenantBySlugAsync_WithNullOrWhitespaceSlug_ThrowsArgumentException(string invalidSlug)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.GetTenantBySlugAsync(invalidSlug));

        ex.ParamName.Should().Be("slug");
    }

    [Fact]
    public async Task GetTenantBySlugAsync_WithNonExistentSlug_ThrowsTenantNotResolvedException()
    {
        // Arrange
        const string slug = "nonexistent";
        var activeTenants = new List<Tenant>();

        _mockDynamicTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(activeTenants);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantNotResolvedException>(
            () => _sut.GetTenantBySlugAsync(slug));

        ex.Message.Should().Contain(slug);
    }

    #endregion

    #region ActivateTenantAsync Tests

    [Fact]
    public async Task ActivateTenantAsync_WithActiveTenant_ReturnsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Status = TenantStatus.Active,
            IsDeleted = false,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        _mockTenantRepository.Setup(r => r.ActivateTenantAsync(tenantId))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ActivateTenantAsync(tenantId);

        // Assert
        result.Should().BeTrue();
        _mockTenantRepository.Verify(r => r.ActivateTenantAsync(tenantId), Times.Once);
    }

    [Fact]
    public async Task ActivateTenantAsync_WithDeletedTenant_ThrowsTenantNotActiveException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Status = TenantStatus.Active,
            IsDeleted = true
        };

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantNotActiveException>(
            () => _sut.ActivateTenantAsync(tenantId));

        ex.Message.Should().Contain("cannot be activated");
    }

    [Fact]
    public async Task ActivateTenantAsync_WithArchivedStatus_ThrowsTenantNotActiveException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Status = TenantStatus.Archived,
            IsDeleted = false
        };

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantNotActiveException>(
            () => _sut.ActivateTenantAsync(tenantId));
    }

    #endregion

    #region SuspendTenantAsync Tests

    [Fact]
    public async Task SuspendTenantAsync_WithValidTenant_ReturnsTrueAndCallsRepository()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Status = TenantStatus.Active };

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        _mockTenantRepository.Setup(r => r.SuspendTenantAsync(tenantId, null))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SuspendTenantAsync(tenantId);

        // Assert
        result.Should().BeTrue();
        _mockTenantRepository.Verify(r => r.SuspendTenantAsync(tenantId, null), Times.Once);
    }

    [Fact]
    public async Task SuspendTenantAsync_WithReason_PassesReasonToRepository()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        const string reason = "Payment failed";
        var tenant = new Tenant { Id = tenantId, Status = TenantStatus.Active };

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        _mockTenantRepository.Setup(r => r.SuspendTenantAsync(tenantId, reason))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SuspendTenantAsync(tenantId, reason);

        // Assert
        result.Should().BeTrue();
        _mockTenantRepository.Verify(r => r.SuspendTenantAsync(tenantId, reason), Times.Once);
    }

    #endregion

    #region DeleteTenantAsync Tests

    [Fact]
    public async Task DeleteTenantAsync_WithValidTenant_MarksAsDeleted()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Status = TenantStatus.Active,
            IsDeleted = false
        };

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        _mockTenantRepository.Setup(r => r.UpdateAsync(It.IsAny<Tenant>()))
            .ReturnsAsync((Tenant t) => t);

        // Act
        var result = await _sut.DeleteTenantAsync(tenantId);

        // Assert
        result.Should().BeTrue();
        tenant.IsDeleted.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Archived);

        _mockTenantRepository.Verify(r => r.UpdateAsync(It.Is<Tenant>(t => t.Id == tenantId && t.IsDeleted)), Times.Once);
    }

    #endregion

    #region UpdateTenantAsync Tests

    [Fact]
    public async Task UpdateTenantAsync_WithValidAction_UpdatesAndReturns()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var originalTenant = new Tenant
        {
            Id = tenantId,
            Name = "Old Name",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(originalTenant);

        _mockTenantRepository.Setup(r => r.UpdateAsync(It.IsAny<Tenant>()))
            .ReturnsAsync((Tenant t) => t);

        // Act
        var result = await _sut.UpdateTenantAsync(tenantId, t => t.Name = "New Name");

        // Assert
        result.Name.Should().Be("New Name");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _mockTenantRepository.Verify(r => r.UpdateAsync(It.IsAny<Tenant>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTenantAsync_WithNullAction_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.UpdateTenantAsync(Guid.NewGuid(), null!));

        ex.ParamName.Should().Be("updateAction");
    }

    #endregion

    #region IsSubscriptionValidAsync Tests

    [Fact]
    public async Task IsSubscriptionValidAsync_WithValidSubscription_ReturnsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _sut.IsSubscriptionValidAsync(tenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSubscriptionValidAsync_WithExpiredSubscription_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _sut.IsSubscriptionValidAsync(tenantId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetActiveTenantsAsync Tests

    [Fact]
    public async Task GetActiveTenantsAsync_ReturnsListOfActiveTenants()
    {
        // Arrange
        var activeTenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant 1" },
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant 2" }
        };

        _mockDynamicTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(activeTenants);

        // Act
        var result = await _sut.GetActiveTenantsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(activeTenants);
    }

    [Fact]
    public async Task GetActiveTenantsAsync_WithNoActiveTenants_ReturnsEmptyList()
    {
        // Arrange
        _mockDynamicTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(new List<Tenant>());

        // Act
        var result = await _sut.GetActiveTenantsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetExpiringSubscriptionsAsync Tests

    [Fact]
    public async Task GetExpiringSubscriptionsAsync_WithDefaultDays_CallsRepositoryWith30Days()
    {
        // Arrange
        var expiringTenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), SubscriptionExpiresAt = DateTime.UtcNow.AddDays(15) }
        };

        _mockTenantRepository.Setup(r => r.GetExpiringSubscriptionsAsync(30))
            .ReturnsAsync(expiringTenants);

        // Act
        var result = await _sut.GetExpiringSubscriptionsAsync();

        // Assert
        result.Should().HaveCount(1);
        _mockTenantRepository.Verify(r => r.GetExpiringSubscriptionsAsync(30), Times.Once);
    }

    [Fact]
    public async Task GetExpiringSubscriptionsAsync_WithCustomDays_CallsRepositoryWithCustomDays()
    {
        // Arrange
        const int customDays = 7;
        var expiringTenants = new List<Tenant>();

        _mockTenantRepository.Setup(r => r.GetExpiringSubscriptionsAsync(customDays))
            .ReturnsAsync(expiringTenants);

        // Act
        var result = await _sut.GetExpiringSubscriptionsAsync(customDays);

        // Assert
        result.Should().BeEmpty();
        _mockTenantRepository.Verify(r => r.GetExpiringSubscriptionsAsync(customDays), Times.Once);
    }

    #endregion

    #region SearchTenantsAsync Tests

    [Fact]
    public async Task SearchTenantsAsync_WithQueryMatchingName_ReturnsTenant()
    {
        // Arrange
        const string query = "acme";
        var tenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Name = "ACME Corporation", Slug = "acme-corp" }
        };

        _mockDynamicTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act
        var result = await _sut.SearchTenantsAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("ACME Corporation");
    }

    [Fact]
    public async Task SearchTenantsAsync_WithQueryMatchingSlug_ReturnsTenant()
    {
        // Arrange
        const string query = "corp";
        var tenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Name = "Test", Slug = "test-corp" }
        };

        _mockDynamicTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act
        var result = await _sut.SearchTenantsAsync(query);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchTenantsAsync_WithQueryMatchingEmail_ReturnsTenant()
    {
        // Arrange
        const string query = "admin@acme.com";
        var tenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Name = "ACME", AdminEmail = "admin@acme.com" }
        };

        _mockDynamicTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act
        var result = await _sut.SearchTenantsAsync(query);

        // Assert
        result.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchTenantsAsync_WithNullOrWhitespaceQuery_ReturnsEmptyList(string query)
    {
        // Act
        var result = await _sut.SearchTenantsAsync(query);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchTenantsAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        const string query = "nonexistent";
        var tenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Name = "Other Tenant", Slug = "other" }
        };

        _mockDynamicTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act
        var result = await _sut.SearchTenantsAsync(query);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region IsInTrialAsync Tests

    [Fact]
    public async Task IsInTrialAsync_WithTrialStatus_ReturnsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Status = TenantStatus.Trial };

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _sut.IsInTrialAsync(tenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsInTrialAsync_WithActiveStatus_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Status = TenantStatus.Active };

        _mockDynamicTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _sut.IsInTrialAsync(tenantId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetTenantStatisticsAsync Tests

    [Fact]
    public async Task GetTenantStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
        var statusCounts = new Dictionary<TenantStatus, int>
        {
            { TenantStatus.Active, 50 },
            { TenantStatus.Trial, 10 },
            { TenantStatus.Suspended, 5 }
        };

        var expiringTenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), SubscriptionExpiresAt = DateTime.UtcNow.AddDays(15) }
        };

        _mockTenantRepository.Setup(r => r.GetStatusCountsAsync())
            .ReturnsAsync(statusCounts);

        _mockTenantRepository.Setup(r => r.GetExpiringSubscriptionsAsync(30))
            .ReturnsAsync(expiringTenants);

        // Act
        var result = await _sut.GetTenantStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        var dynamicResult = (dynamic)result;
        Assert.Equal(65, (int)dynamicResult.TotalTenants);
        Assert.Equal(50, (int)dynamicResult.ActiveTenants);

        _mockTenantRepository.Verify(r => r.GetStatusCountsAsync(), Times.Once);
        _mockTenantRepository.Verify(r => r.GetExpiringSubscriptionsAsync(30), Times.Once);
    }

    [Fact]
    public async Task GetTenantStatisticsAsync_WithNoTenants_ReturnsZeroStats()
    {
        // Arrange
        var statusCounts = new Dictionary<TenantStatus, int>();
        var expiringTenants = new List<Tenant>();

        _mockTenantRepository.Setup(r => r.GetStatusCountsAsync())
            .ReturnsAsync(statusCounts);

        _mockTenantRepository.Setup(r => r.GetExpiringSubscriptionsAsync(30))
            .ReturnsAsync(expiringTenants);

        // Act
        var result = await _sut.GetTenantStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        var dynamicResult = (dynamic)result;
        Assert.Equal(0, (int)dynamicResult.TotalTenants);
        Assert.Equal(0, (int)dynamicResult.ActiveTenants);
    }

    #endregion

    #region GetTenantsByStatusAsync Tests

    [Fact]
    public async Task GetTenantsByStatusAsync_WithValidStatus_ReturnsTenants()
    {
        // Arrange
        var activeTenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Status = TenantStatus.Active },
            new Tenant { Id = Guid.NewGuid(), Status = TenantStatus.Active },
            new Tenant { Id = Guid.NewGuid(), Status = TenantStatus.Trial }
        };

        _mockDynamicTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(activeTenants);

        // Act
        var result = await _sut.GetTenantsByStatusAsync(TenantStatus.Active);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.Status.Should().Be(TenantStatus.Active));
    }

    [Fact]
    public async Task GetTenantsByStatusAsync_WithNoMatchingStatus_ReturnsEmptyList()
    {
        // Arrange
        var activeTenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Status = TenantStatus.Active }
        };

        _mockDynamicTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(activeTenants);

        // Act
        var result = await _sut.GetTenantsByStatusAsync(TenantStatus.Trial);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
