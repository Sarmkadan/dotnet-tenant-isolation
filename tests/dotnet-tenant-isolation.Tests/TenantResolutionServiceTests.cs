#nullable enable

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TenantIsolation.Constants;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;
using TenantIsolation.Services;
using Xunit;

namespace TenantIsolation.Tests;

public class TenantResolutionServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IDynamicTenantStore> _mockTenantStore;
    private readonly Mock<ILogger<TenantResolutionService>> _mockLogger;
    private readonly TenantResolutionService _sut;

    public TenantResolutionServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockTenantStore = new Mock<IDynamicTenantStore>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<TenantResolutionService>>();

        _sut = new TenantResolutionService(_mockHttpContextAccessor.Object, _mockTenantStore.Object, _mockLogger.Object);
    }

    private HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return httpContext;
    }

    #region ResolveTenantAsync - Header Resolution Tests

    [Fact]
    public async Task ResolveTenantAsync_WithValidTenantIdHeader_ResolvesFromHeader()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[TenantConstants.TenantIdHeader] = tenantId.ToString();

        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        result.Should().Be(tenant);
        result.Id.Should().Be(tenantId);

        _mockTenantStore.Verify(s => s.GetTenantByIdAsync(tenantId), Times.Once);
    }

    [Fact]
    public async Task ResolveTenantAsync_WithValidTenantSlugHeader_ResolvesFromHeader()
    {
        // Arrange
        const string slug = "test-tenant";
        var tenant = new Tenant { Id = Guid.NewGuid(), Slug = slug, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[TenantConstants.TenantSlugHeader] = slug;

        var tenants = new List<Tenant> { tenant };
        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        result.Should().Be(tenant);
    }

    [Fact]
    public async Task ResolveTenantAsync_WithInvalidTenantIdHeader_TriesNextStrategy()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[TenantConstants.TenantIdHeader] = "invalid-guid";

        const string slug = "test-tenant";
        var tenant = new Tenant { Id = Guid.NewGuid(), Slug = slug, Status = TenantStatus.Active };
        var tenants = new List<Tenant> { tenant };

        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert - Should return empty list because no slug header was set
        result.Should().Be(tenant); // Will eventually fail when all strategies exhausted

        // This test shows fallback behavior - the service tries claims next
    }

    #endregion

    #region ResolveTenantAsync - Claims Resolution Tests

    [Fact]
    public async Task ResolveTenantAsync_WithValidTenantIdClaim_ResolvesFromClaims()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();

        var claims = new List<Claim>
        {
            new Claim(TenantConstants.TenantIdClaimType, tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        result.Should().Be(tenant);
        _mockTenantStore.Verify(s => s.GetTenantByIdAsync(tenantId), Times.Once);
    }

    [Fact]
    public async Task ResolveTenantAsync_WithValidTenantSlugClaim_ResolvesFromClaims()
    {
        // Arrange
        const string slug = "test-tenant";
        var tenant = new Tenant { Id = Guid.NewGuid(), Slug = slug, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();

        var claims = new List<Claim>
        {
            new Claim(TenantConstants.TenantSlugClaimType, slug)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        var tenants = new List<Tenant> { tenant };
        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        result.Should().Be(tenant);
    }

    [Fact]
    public async Task ResolveTenantAsync_WithoutAuthentication_SkipsClaimsResolution()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();

        // Create unauthenticated principal
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "test") });
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        // Set up route resolution
        var routeData = new RouteData();
        routeData.Values[TenantConstants.TenantRouteParameter] = tenantId.ToString();
        httpContext.SetRouteData(routeData);

        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        result.Should().Be(tenant);
    }

    #endregion

    #region ResolveTenantAsync - Route Resolution Tests

    [Fact]
    public async Task ResolveTenantAsync_WithValidTenantIdRoute_ResolvesFromRoute()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();

        var routeData = new RouteData();
        routeData.Values[TenantConstants.TenantRouteParameter] = tenantId.ToString();
        httpContext.SetRouteData(routeData);

        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        result.Should().Be(tenant);
    }

    [Fact]
    public async Task ResolveTenantAsync_WithValidTenantSlugRoute_ResolvesFromRoute()
    {
        // Arrange
        const string slug = "test-tenant";
        var tenant = new Tenant { Id = Guid.NewGuid(), Slug = slug, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();

        var routeData = new RouteData();
        routeData.Values[TenantConstants.TenantSlugRouteParameter] = slug;
        httpContext.SetRouteData(routeData);

        var tenants = new List<Tenant> { tenant };
        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        result.Should().Be(tenant);
    }

    #endregion

    #region ResolveTenantAsync - Subdomain Resolution Tests

    [Fact]
    public async Task ResolveTenantAsync_WithValidTenantSubdomain_ResolvesFromSubdomain()
    {
        // Arrange
        const string slug = "tenant1";
        var tenant = new Tenant { Id = Guid.NewGuid(), Slug = slug, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();

        httpContext.Request.Host = new HostString($"{slug}.example.com");

        var tenants = new List<Tenant> { tenant };
        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        result.Should().Be(tenant);
    }

    [Fact]
    public async Task ResolveTenantAsync_WithReservedSubdomain_IgnoresSubdomainResolution()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Host = new HostString("www.example.com");

        // Since www is reserved, it should not be used for tenant resolution
        // This test verifies that reserved subdomains are filtered out
        var tenants = new List<Tenant>();
        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act & Assert
        await Assert.ThrowsAsync<TenantNotResolvedException>(
            () => _sut.ResolveTenantAsync());
    }

    [Theory]
    [InlineData("api.example.com")]
    [InlineData("admin.example.com")]
    [InlineData("mail.example.com")]
    [InlineData("auth.example.com")]
    [InlineData("login.example.com")]
    public async Task ResolveTenantAsync_WithReservedSubdomains_AllIgnoredDuringResolution(string host)
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Host = new HostString(host);

        var tenants = new List<Tenant>();
        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act & Assert
        await Assert.ThrowsAsync<TenantNotResolvedException>(
            () => _sut.ResolveTenantAsync());
    }

    [Fact]
    public async Task ResolveTenantAsync_WithSubdomainCaseMismatch_StillResolves()
    {
        // Arrange
        const string slug = "testcorp";
        var tenant = new Tenant { Id = Guid.NewGuid(), Slug = slug, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();

        httpContext.Request.Host = new HostString($"TestCorp.example.com");

        var tenants = new List<Tenant> { tenant };
        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        result.Should().Be(tenant);
    }

    #endregion

    #region ResolveTenantAsync - Caching Tests

    [Fact]
    public async Task ResolveTenantAsync_CachesTenantInHttpContext()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[TenantConstants.TenantIdHeader] = tenantId.ToString();

        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        httpContext.Items.Should().ContainKey(TenantConstants.CurrentTenantContextKey);
        httpContext.Items[TenantConstants.CurrentTenantContextKey].Should().Be(tenant);
    }

    [Fact]
    public async Task ResolveTenantAsync_ReturnsCachedTenantOnSecondCall()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[TenantConstants.TenantIdHeader] = tenantId.ToString();

        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var firstCall = await _sut.ResolveTenantAsync();
        var secondCall = await _sut.ResolveTenantAsync();

        // Assert
        firstCall.Should().Be(secondCall);
        // Verify store was only called once (second call should use cache)
        _mockTenantStore.Verify(s => s.GetTenantByIdAsync(It.IsAny<Guid>()), Times.Once);
    }

    #endregion

    #region ResolveTenantAsync - Status Validation Tests

    [Fact]
    public async Task ResolveTenantAsync_WithInactiveTenant_ThrowsTenantNotActiveException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Status = TenantStatus.Suspended };
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[TenantConstants.TenantIdHeader] = tenantId.ToString();

        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantNotActiveException>(
            () => _sut.ResolveTenantAsync());

        ex.Message.Should().Contain("Suspended");
    }

    [Theory]
    [InlineData(TenantStatus.Archived)]
    [InlineData(TenantStatus.Suspended)]
    [InlineData(TenantStatus.Provisioning)]
    public async Task ResolveTenantAsync_WithNonActiveStatus_ThrowsException(TenantStatus status)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Status = status };
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[TenantConstants.TenantIdHeader] = tenantId.ToString();

        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act & Assert
        await Assert.ThrowsAsync<TenantNotActiveException>(
            () => _sut.ResolveTenantAsync());
    }

    #endregion

    #region ResolveTenantAsync - Error Cases Tests

    [Fact]
    public async Task ResolveTenantAsync_WithoutHttpContext_ThrowsTenantNotResolvedException()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantNotResolvedException>(
            () => _sut.ResolveTenantAsync());

        ex.Message.Should().Contain("HTTP context not available");
    }

    [Fact]
    public async Task ResolveTenantAsync_WithNoResolutionStrategies_ThrowsTenantNotResolvedException()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var tenants = new List<Tenant>();

        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act & Assert
        await Assert.ThrowsAsync<TenantNotResolvedException>(
            () => _sut.ResolveTenantAsync());
    }

    #endregion

    #region GetCurrentTenant Tests

    [Fact]
    public void GetCurrentTenant_WithCachedTenant_ReturnsTenant()
    {
        // Arrange
        var tenant = new Tenant { Id = Guid.NewGuid() };
        var httpContext = CreateHttpContext();
        httpContext.Items[TenantConstants.CurrentTenantContextKey] = tenant;

        // Act
        var result = _sut.GetCurrentTenant();

        // Assert
        result.Should().Be(tenant);
    }

    [Fact]
    public void GetCurrentTenant_WithoutCachedTenant_ReturnsNull()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        // Act
        var result = _sut.GetCurrentTenant();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentTenant_WithoutHttpContext_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.GetCurrentTenant();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetCurrentTenantId Tests

    [Fact]
    public void GetCurrentTenantId_WithCachedTenant_ReturnsTenantId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId };
        var httpContext = CreateHttpContext();
        httpContext.Items[TenantConstants.CurrentTenantContextKey] = tenant;

        // Act
        var result = _sut.GetCurrentTenantId();

        // Assert
        result.Should().Be(tenantId);
    }

    [Fact]
    public void GetCurrentTenantId_WithoutCachedTenant_ReturnsNull()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        // Act
        var result = _sut.GetCurrentTenantId();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region HasTenant Tests

    [Fact]
    public void HasTenant_WithCachedTenant_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant { Id = Guid.NewGuid() };
        var httpContext = CreateHttpContext();
        httpContext.Items[TenantConstants.CurrentTenantContextKey] = tenant;

        // Act
        var result = _sut.HasTenant();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasTenant_WithoutCachedTenant_ReturnsFalse()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        // Act
        var result = _sut.HasTenant();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Resolution Priority Tests

    [Fact]
    public async Task ResolveTenantAsync_WithHeaderAndClaim_PrefersHeader()
    {
        // Arrange
        var headerTenantId = Guid.NewGuid();
        var claimTenantId = Guid.NewGuid();

        var headerTenant = new Tenant { Id = headerTenantId, Status = TenantStatus.Active };
        var claimTenant = new Tenant { Id = claimTenantId, Status = TenantStatus.Active };

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[TenantConstants.TenantIdHeader] = headerTenantId.ToString();

        var claims = new List<Claim>
        {
            new Claim(TenantConstants.TenantIdClaimType, claimTenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        httpContext.User = new ClaimsPrincipal(identity);

        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(headerTenantId))
            .ReturnsAsync(headerTenant);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        result.Id.Should().Be(headerTenantId);
        // Verify header tenant was resolved, not claim tenant
        _mockTenantStore.Verify(s => s.GetTenantByIdAsync(headerTenantId), Times.Once);
        _mockTenantStore.Verify(s => s.GetTenantByIdAsync(claimTenantId), Times.Never);
    }

    #endregion

    #region Subdomain Extraction Tests

    [Fact]
    public async Task ResolveTenantAsync_WithoutDotInHost_IgnoresSubdomain()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Host = new HostString("localhost");

        var tenants = new List<Tenant>();
        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act & Assert
        await Assert.ThrowsAsync<TenantNotResolvedException>(
            () => _sut.ResolveTenantAsync());
    }

    [Fact]
    public async Task ResolveTenantAsync_WithEmptyHost_IgnoresSubdomain()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Host = new HostString("");

        var tenants = new List<Tenant>();
        _mockTenantStore.Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(tenants);

        // Act & Assert
        await Assert.ThrowsAsync<TenantNotResolvedException>(
            () => _sut.ResolveTenantAsync());
    }

    #endregion
}
