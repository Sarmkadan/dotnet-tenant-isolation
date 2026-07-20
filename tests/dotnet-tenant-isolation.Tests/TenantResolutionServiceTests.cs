#nullable enable

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using TenantIsolation.Configuration;
using TenantIsolation.Constants;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;
using TenantIsolation.Services;
using Xunit;

namespace TenantIsolation.Tests;

/// <summary>
/// Contains unit tests for the <see cref="TenantResolutionService"/> class.
/// Tests various tenant resolution strategies including header-based, claims-based, route-based,
/// and subdomain-based resolution, as well as caching and error scenarios.
/// </summary>
public class TenantResolutionServiceTests
{
    /// <summary>
    /// Mock of <see cref="IHttpContextAccessor"/> used to provide HTTP context for testing.
    /// </summary>
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

    /// <summary>
    /// Mock of <see cref="IDynamicTenantStore"/> used to simulate tenant data access.
    /// </summary>
    private readonly Mock<IDynamicTenantStore> _mockTenantStore;

    /// <summary>
    /// Mock of <see cref="ILogger{TenantResolutionService}"/> used to verify logging behavior.
    /// </summary>
    private readonly Mock<ILogger<TenantResolutionService>> _mockLogger;

    /// <summary>
    /// Mock of <see cref="IOptions{TenantResolutionOptions}"/> used to provide options for testing.
    /// </summary>
    private readonly Mock<IOptions<TenantResolutionOptions>> _mockOptions;

    /// <summary>
    /// System under test - instance of <see cref="TenantResolutionService"/> being tested.
    /// </summary>
    private readonly TenantResolutionService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolutionServiceTests"/> class.
    /// Sets up the mock dependencies and creates the service under test.
    /// </summary>
    public TenantResolutionServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockTenantStore = new Mock<IDynamicTenantStore>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<TenantResolutionService>>();
        _mockOptions = new Mock<IOptions<TenantResolutionOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(new TenantResolutionOptions
        {
            ResolutionStrategies = new List<TenantResolutionStrategy> { TenantResolutionStrategy.Header },
            ThrowOnResolutionFailure = false
        });

        _sut = new TenantResolutionService(
            _mockHttpContextAccessor.Object,
            _mockTenantStore.Object,
            _mockLogger.Object,
            _mockOptions.Object);
    }

    /// <summary>
    /// Creates a new <see cref="DefaultHttpContext"/> instance and sets it up with the mock HttpContextAccessor.
    /// </summary>
    /// <returns>A configured <see cref="HttpContext"/> instance for testing.</returns>
    private HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return httpContext;
    }

    /// <summary>
    /// Sets a route value on the given <see cref="HttpContext"/> so that
    /// <c>HttpContext.GetRouteData()</c> (used by the production route resolution
    /// strategy) can see it. Setting <c>HttpContext.Items</c> directly does not
    /// populate route data and would make the route resolution strategy a no-op.
    /// </summary>
    private static void SetRouteValue(HttpContext httpContext, string key, object value)
    {
        var routingFeature = httpContext.Features.Get<Microsoft.AspNetCore.Routing.IRoutingFeature>();
        if (routingFeature == null)
        {
            routingFeature = new Microsoft.AspNetCore.Routing.RoutingFeature();
            httpContext.Features.Set(routingFeature);
        }

        routingFeature.RouteData ??= new RouteData();
        routingFeature.RouteData.Values[key] = value;
    }

    #region ResolveTenantAsync - Header Resolution Tests

    /// <summary>
    /// Tests that tenant is correctly resolved from the Tenant-Id header when a valid GUID is provided.
    /// Verifies that the tenant is retrieved from the store and returned correctly.
    /// </summary>
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

    /// <summary>
    /// Tests that tenant is correctly resolved from the Tenant-Slug header when a valid slug is provided.
    /// Verifies that the tenant is retrieved from the store using the slug and returned correctly.
    /// </summary>
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

    /// <summary>
    /// Tests that when an invalid tenant ID is provided in the header, the service falls back to the next resolution strategy.
    /// Verifies that the service attempts to resolve using the slug strategy when header resolution fails.
    /// </summary>
    [Fact]
    public async Task ResolveTenantAsync_WithInvalidTenantIdHeader_TriesNextStrategy()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[TenantConstants.TenantIdHeader] = "invalid-guid";

        // No slug header, no claims, no route data, and no subdomain-bearing host is
        // configured, so every subsequent resolution strategy is exhausted too.

        // Act
        Func<Task> act = async () => await _sut.ResolveTenantAsync();

        // Assert - falls through every strategy and ultimately fails to resolve a tenant.
        await act.Should().ThrowAsync<TenantNotResolvedException>();
    }

    #endregion

    #region ResolveTenantAsync - Claims Resolution Tests

    /// <summary>
    /// Tests that tenant is correctly resolved from the Tenant-Id claim when present in the user's identity.
    /// Verifies that the tenant is retrieved from the store using the claim value and returned correctly.
    /// </summary>
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

    /// <summary>
    /// Tests that tenant is correctly resolved from the Tenant-Slug claim when present in the user's identity.
    /// Verifies that the tenant is retrieved from the store using the slug claim and returned correctly.
    /// </summary>
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

    /// <summary>
    /// Tests that tenant resolution skips claims-based resolution when the user is not authenticated.
    /// Verifies that the service falls back to route-based resolution when no claims are present.
    /// </summary>
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
        SetRouteValue(httpContext, TenantConstants.TenantRouteParameter, tenantId.ToString());

        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        result.Should().Be(tenant);
    }

    #endregion

    #region ResolveTenantAsync - Route Resolution Tests

    /// <summary>
    /// Tests that tenant is correctly resolved from route parameters when provided in the HTTP context.
    /// Verifies that the tenant is retrieved from the store using the route parameter value and returned correctly.
    /// </summary>
    [Fact]
    public async Task ResolveTenantAsync_WithValidTenantIdRoute_ResolvesFromRoute()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();

        // Set tenant in context items for route resolution
        SetRouteValue(httpContext, TenantConstants.TenantRouteParameter, tenantId.ToString());

        _mockTenantStore.Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _sut.ResolveTenantAsync();

        // Assert
        result.Should().Be(tenant);
    }

    /// <summary>
    /// Tests that tenant is correctly resolved from route slug parameters when provided in the HTTP context.
    /// Verifies that the tenant is retrieved from the store using the slug route parameter and returned correctly.
    /// </summary>
    [Fact]
    public async Task ResolveTenantAsync_WithValidTenantSlugRoute_ResolvesFromRoute()
    {
        // Arrange
        const string slug = "test-tenant";
        var tenant = new Tenant { Id = Guid.NewGuid(), Slug = slug, Status = TenantStatus.Active };
        var httpContext = CreateHttpContext();

        // Set tenant slug in context items for route resolution
        SetRouteValue(httpContext, TenantConstants.TenantSlugRouteParameter, slug);

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

    /// <summary>
    /// Tests that tenant is correctly resolved from the subdomain portion of the host when a valid tenant subdomain is provided.
    /// Verifies that the tenant is retrieved from the store using the subdomain and returned correctly.
    /// </summary>
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

    /// <summary>
    /// Tests that reserved subdomains (like www) are ignored during tenant resolution.
    /// Verifies that the service throws <see cref="TenantNotResolvedException"/> when attempting to resolve from a reserved subdomain.
    /// </summary>
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

    /// <summary>
    /// Tests that various reserved subdomains (api, admin, mail, auth, login) are all ignored during tenant resolution.
    /// Verifies that the service throws <see cref="TenantNotResolvedException"/> for each reserved subdomain type.
    /// </summary>
    /// <param name="host">The reserved subdomain host to test.</param>
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

    /// <summary>
    /// Tests that tenant resolution works correctly even when there's a case mismatch between the subdomain and stored tenant slug.
    /// Verifies that the service performs case-insensitive comparison when matching tenants by subdomain.
    /// </summary>
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

    /// <summary>
    /// Tests that resolved tenant is cached in the HTTP context for subsequent calls.
    /// Verifies that the tenant is stored in the HttpContext.Items collection after resolution.
    /// </summary>
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

    /// <summary>
    /// Tests that subsequent calls to <see cref="TenantResolutionService.ResolveTenantAsync"/> return the cached tenant instead of querying the store again.
    /// Verifies that the service uses the cached tenant from HttpContext.Items on subsequent calls.
    /// </summary>
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

    /// <summary>
    /// Tests that an inactive tenant (Suspended status) throws <see cref="TenantNotActiveException"/>.
    /// Verifies that the service validates tenant status and rejects tenants that are not Active.
    /// </summary>
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

    /// <summary>
    /// Tests that tenants with non-Active statuses (Archived, Suspended, Provisioning) throw <see cref="TenantNotActiveException"/>.
    /// Verifies that the service validates tenant status and rejects tenants that are not in Active state.
    /// </summary>
    /// <param name="status">The non-Active tenant status to test.</param>
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

    /// <summary>
    /// Tests that <see cref="TenantResolutionService.ResolveTenantAsync"/> throws <see cref="TenantNotResolvedException"/> when HTTP context is not available.
    /// Verifies that the service properly handles missing HTTP context scenarios.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="TenantResolutionService.ResolveTenantAsync"/> throws <see cref="TenantNotResolvedException"/> when no tenant resolution strategies are available.
    /// Verifies that the service throws appropriate exception when all resolution attempts fail.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="TenantResolutionService.GetCurrentTenant"/> returns the cached tenant when it exists in the HTTP context.
    /// Verifies that the service can retrieve the tenant from the cached value in HttpContext.Items.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="TenantResolutionService.GetCurrentTenant"/> returns null when no tenant is cached in the HTTP context.
    /// Verifies that the service returns null when HttpContext.Items does not contain a cached tenant.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="TenantResolutionService.GetCurrentTenant"/> returns null when HTTP context is not available.
    /// Verifies that the service handles missing HTTP context scenarios gracefully.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="TenantResolutionService.GetCurrentTenantId"/> returns the tenant ID when a tenant is cached in the HTTP context.
    /// Verifies that the service can extract and return the tenant ID from the cached tenant object.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="TenantResolutionService.GetCurrentTenantId"/> returns null when no tenant is cached in the HTTP context.
    /// Verifies that the service returns null when HttpContext.Items does not contain a cached tenant.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="TenantResolutionService.HasTenant"/> returns true when a tenant is cached in the HTTP context.
    /// Verifies that the service can determine whether a tenant is currently resolved.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="TenantResolutionService.HasTenant"/> returns false when no tenant is cached in the HTTP context.
    /// Verifies that the service can determine when no tenant is currently resolved.
    /// </summary>
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

    /// <summary>
    /// Tests that when both header and claim tenant resolution strategies are available, the header strategy takes precedence.
    /// Verifies that the service follows the correct priority order: headers > claims > routes > subdomains.
    /// </summary>
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

    /// <summary>
    /// Tests that subdomain resolution is ignored when the host does not contain a dot (e.g., localhost).
    /// Verifies that the service properly handles localhost and other simple hostnames.
    /// </summary>
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

    /// <summary>
    /// Tests that subdomain resolution is ignored when the host is empty.
    /// Verifies that the service properly handles empty host scenarios.
    /// </summary>
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
