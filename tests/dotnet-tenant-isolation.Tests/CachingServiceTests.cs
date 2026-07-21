#nullable enable

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Caching;
using Xunit;

namespace TenantIsolation.Tests;

/// <summary>
/// Contains unit tests for the <see cref="CachingService"/> and <see cref="TenantAwareCachingService"/> classes.
/// Tests basic caching operations, tenant-scoped key isolation, expiry, and removal.
/// </summary>
public class CachingServiceTests
{
    /// <summary>
    /// Mock of <see cref="ICacheProvider"/> used to simulate cache operations.
    /// </summary>
    private Mock<ICacheProvider> _mockCacheProvider;

    /// <summary>
    /// Mock of <see cref="ILogger{CachingService}"/> used to verify logging behavior.
    /// </summary>
    private Mock<ILogger<CachingService>> _mockLogger;

    /// <summary>
    /// System under test - instance of <see cref="CachingService"/> being tested.
    /// </summary>
    private CachingService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingServiceTests"/> class.
    /// Sets up mocks and test subject.
    /// </summary>
    public CachingServiceTests()
    {
        _mockCacheProvider = new Mock<ICacheProvider>();
        _mockLogger = new Mock<ILogger<CachingService>>();
        _sut = new CachingService(_mockCacheProvider.Object, _mockLogger.Object);
    }

    #region Basic Caching Operations

    /// <summary>
    /// Tests that GetAsync returns default value when key is null or empty.
    /// </summary>
    [Fact]
    public async Task GetAsync_WithNullOrEmptyKey_ReturnsDefault()
    {
        // Act
        var result1 = await _sut.GetAsync<string>((string)null!);
        var result2 = await _sut.GetAsync<string>(string.Empty);
        var result3 = await _sut.GetAsync<string>("   ");

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetAsync delegates to cache provider for valid keys.
    /// </summary>
    [Fact]
    public async Task GetAsync_DelegatesToCacheProvider()
    {
        // Arrange
        const string testKey = "test:key";
        const string expectedValue = "test value";

        _mockCacheProvider
            .Setup(x => x.GetAsync<string>(testKey))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _sut.GetAsync<string>(testKey);

        // Assert
        result.Should().Be(expectedValue);
        _mockCacheProvider.Verify(x => x.GetAsync<string>(testKey), Times.Once);
    }

    /// <summary>
    /// Tests that SetAsync delegates to cache provider for valid keys.
    /// </summary>
    [Fact]
    public async Task SetAsync_DelegatesToCacheProvider()
    {
        // Arrange
        const string testKey = "test:key";
        const string testValue = "test value";
        var expiration = TimeSpan.FromMinutes(10);

        // Act
        await _sut.SetAsync(testKey, testValue, expiration);

        // Assert
        _mockCacheProvider.Verify(x => x.SetAsync(testKey, testValue, expiration), Times.Once);
    }

    /// <summary>
    /// Tests that SetAsync does nothing when key is null or empty.
    /// </summary>
    [Fact]
    public async Task SetAsync_WithNullOrEmptyKey_DoesNothing()
    {
        // Act
        await _sut.SetAsync((string)null!, "value");
        await _sut.SetAsync(string.Empty, "value");
        await _sut.SetAsync("   ", "value");

        // Assert
        _mockCacheProvider.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Tests that RemoveAsync with single key delegates to cache provider for valid keys.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_SingleKey_WithValidKey_DelegatesToCacheProvider()
    {
        // Arrange
        const string testKey = "test:key";

        // Act
        await _sut.RemoveAsync(testKey);

        // Assert
        _mockCacheProvider.Verify(x => x.RemoveAsync(testKey), Times.Once);
    }

    /// <summary>
    /// Tests that RemoveAsync with single key does nothing when key is null or empty.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_SingleKey_WithNullOrEmptyKey_DoesNothing()
    {
        // Act
        await _sut.RemoveAsync((string)null!);
        await _sut.RemoveAsync(string.Empty);
        await _sut.RemoveAsync("   ");

        // Assert
        _mockCacheProvider.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Tests that RemoveAsync with multiple keys delegates to cache provider.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_WithMultipleKeys_DelegatesToCacheProvider()
    {
        // Arrange
        var testKeys = new[] { "key1", "key2", "key3" };

        // Act
        await _sut.RemoveAsync(testKeys);

        // Assert
        _mockCacheProvider.Verify(x => x.RemoveAsync("key1"), Times.Once);
        _mockCacheProvider.Verify(x => x.RemoveAsync("key2"), Times.Once);
        _mockCacheProvider.Verify(x => x.RemoveAsync("key3"), Times.Once);
    }

    /// <summary>
    /// Tests that ClearAsync delegates to cache provider.
    /// </summary>
    [Fact]
    public async Task ClearAsync_DelegatesToCacheProvider()
    {
        // Act
        await _sut.ClearAsync();

        // Assert
        _mockCacheProvider.Verify(x => x.ClearAsync(), Times.Once);
    }

    /// <summary>
    /// Tests that GetStatisticsAsync returns statistics with zero values initially.
    /// </summary>
    [Fact]
    public async Task GetStatisticsAsync_ReturnsStatistics()
    {
        // Act
        var stats = await _sut.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalEntries.Should().Be(0);
        stats.CacheHits.Should().Be(0);
        stats.CacheMisses.Should().Be(0);
        stats.HitRate.Should().Be(0);
    }

    #endregion

    #region GetOrFetchAsync

    /// <summary>
    /// Tests that GetOrFetchAsync fetches and caches value when cache is empty.
    /// </summary>
    [Fact]
    public async Task GetOrFetchAsync_WithCacheMiss_FetchesAndCachesValue()
    {
        // Arrange
        const string testKey = "test:key";
        const string expectedValue = "fetched value";
        var fetchCalled = false;

        _mockCacheProvider
            .Setup(x => x.GetAsync<string>(testKey))
            .ReturnsAsync((string?)null);

        Func<Task<string>> fetchFunc = async () =>
        {
            fetchCalled = true;
            return expectedValue;
        };

        // Act
        var result = await _sut.GetOrFetchAsync(testKey, fetchFunc, TimeSpan.FromMinutes(5));

        // Assert
        result.Should().Be(expectedValue);
        fetchCalled.Should().BeTrue();
        _mockCacheProvider.Verify(x => x.SetAsync(testKey, expectedValue, TimeSpan.FromMinutes(5)), Times.Once);
    }

    /// <summary>
    /// Tests that GetOrFetchAsync returns cached value when available.
    /// </summary>
    [Fact]
    public async Task GetOrFetchAsync_WithCacheHit_ReturnsCachedValue()
    {
        // Arrange
        const string testKey = "test:key";
        const string cachedValue = "cached value";
        var fetchCalled = false;

        _mockCacheProvider
            .Setup(x => x.GetAsync<string>(testKey))
            .ReturnsAsync(cachedValue);

        Func<Task<string>> fetchFunc = async () =>
        {
            fetchCalled = true;
            return "new value";
        };

        // Act
        var result = await _sut.GetOrFetchAsync(testKey, fetchFunc, TimeSpan.FromMinutes(5));

        // Assert
        result.Should().Be(cachedValue);
        fetchCalled.Should().BeFalse();
        _mockCacheProvider.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    /// <summary>
    /// Tests that GetOrFetchAsync calls fetch function when key is null or empty.
    /// This is the actual behavior - it bypasses cache for null/empty keys.
    /// </summary>
    [Fact]
    public async Task GetOrFetchAsync_WithNullOrEmptyKey_CallsFetchFunction()
    {
        // Arrange
        var fetchCalled = false;

        Func<Task<string>> fetchFunc = async () =>
        {
            fetchCalled = true;
            return "fetched value";
        };

        // Act
        var result1 = await _sut.GetOrFetchAsync((string)null!, fetchFunc);
        var result2 = await _sut.GetOrFetchAsync(string.Empty, fetchFunc);
        var result3 = await _sut.GetOrFetchAsync("   ", fetchFunc);

        // Assert
        result1.Should().Be("fetched value");
        result2.Should().Be("fetched value");
        result3.Should().Be("fetched value");
        fetchCalled.Should().BeTrue();
    }

    /// <summary>
    /// Tests that GetOrFetchAsync handles null fetch function result.
    /// </summary>
    [Fact]
    public async Task GetOrFetchAsync_WithNullFetchResult_DoesNotCache()
    {
        // Arrange
        const string testKey = "test:key";

        _mockCacheProvider
            .Setup(x => x.GetAsync<string>(testKey))
            .ReturnsAsync((string?)null);

        Func<Task<string>> fetchFunc = async () => null!;

        // Act
        var result = await _sut.GetOrFetchAsync(testKey, fetchFunc);

        // Assert
        result.Should().BeNull();
        _mockCacheProvider.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    #endregion

    #region TenantAwareCachingService


    /// <summary>
    /// Tests that TenantAwareCachingService scopes keys to tenant.
    /// </summary>
    [Fact]
    public async Task TenantAwareCachingService_GetTenantAwareKey_ScopesKeysToTenant()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockLogger = new Mock<ILogger<TenantAwareCachingService>>();
        var mockInnerService = new Mock<ICachingService>();

        var tenantAwareService = new TenantAwareCachingService(
            mockInnerService.Object,
            mockHttpContextAccessor.Object,
            mockLogger.Object);

        // Act - simulate tenant in HttpContext
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantId"] = "tenant-123";
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Test that keys are scoped
        const string logicalKey = "user:profile";
        var tenantAwareKey = tenantAwareService.GetType()
            .GetMethod("GetTenantAwareKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(tenantAwareService, new object[] { logicalKey }) as string;

        tenantAwareKey.Should().Be("tenant-123:user:profile");
    }

    /// <summary>
    /// Tests that TenantAwareCachingService handles missing tenant context.
    /// </summary>
    [Fact]
    public async Task TenantAwareCachingService_WithMissingTenantContext_UsesLogicalKey()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockLogger = new Mock<ILogger<TenantAwareCachingService>>();
        var mockInnerService = new Mock<ICachingService>();

        var tenantAwareService = new TenantAwareCachingService(
            mockInnerService.Object,
            mockHttpContextAccessor.Object,
            mockLogger.Object);

        // Act - no tenant in HttpContext
        var httpContext = new DefaultHttpContext();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Test that keys are not modified when no tenant
        const string logicalKey = "user:profile";
        var tenantAwareKey = tenantAwareService.GetType()
            .GetMethod("GetTenantAwareKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(tenantAwareService, new object[] { logicalKey }) as string;

        tenantAwareKey.Should().Be(logicalKey);
    }

    /// <summary>
    /// Tests that TenantAwareCachingService isolates cache keys between tenants.
    /// Two tenants with the same logical key should have different cache entries.
    /// </summary>
    [Fact]
    public async Task TenantAwareCachingService_IsolatesCacheKeysBetweenTenants()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockLogger = new Mock<ILogger<TenantAwareCachingService>>();
        var mockInnerService = new Mock<ICacheProvider>();

        var cachingService = new CachingService(mockInnerService.Object, Mock.Of<ILogger<CachingService>>());
        var tenantAwareService = new TenantAwareCachingService(
            cachingService,
            mockHttpContextAccessor.Object,
            mockLogger.Object);

        // Setup tenant 1
        var httpContext1 = new DefaultHttpContext();
        httpContext1.Items["TenantId"] = "tenant-1";
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext1);

        const string logicalKey = "user:profile";
        const string tenant1Value = "tenant 1 profile";
        const string tenant2Value = "tenant 2 profile";

        // Setup cache for tenant 1
        mockInnerService
            .Setup(x => x.GetAsync<string>("tenant-1:user:profile"))
            .ReturnsAsync(tenant1Value);

        // Act - get from tenant 1
        var result1 = await tenantAwareService.GetAsync<string>(logicalKey);

        // Assert - tenant 1 gets its value
        result1.Should().Be(tenant1Value);

        // Setup tenant 2
        var httpContext2 = new DefaultHttpContext();
        httpContext2.Items["TenantId"] = "tenant-2";
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext2);

        // Setup cache for tenant 2 (different key due to tenant isolation)
        mockInnerService
            .Setup(x => x.GetAsync<string>("tenant-2:user:profile"))
            .ReturnsAsync(tenant2Value);

        // Act - get from tenant 2
        var result2 = await tenantAwareService.GetAsync<string>(logicalKey);

        // Assert - tenant 2 gets its value, not tenant 1's
        result2.Should().Be(tenant2Value);
        result2.Should().NotBe(result1);
    }

    /// <summary>
    /// Tests that TenantAwareCachingService removes keys scoped to specific tenant.
    /// </summary>
    [Fact]
    public async Task TenantAwareCachingService_RemoveAsync_RemovesTenantScopedKey()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockLogger = new Mock<ILogger<TenantAwareCachingService>>();
        var mockInnerService = new Mock<ICacheProvider>();

        var cachingService = new CachingService(mockInnerService.Object, Mock.Of<ILogger<CachingService>>());
        var tenantAwareService = new TenantAwareCachingService(
            cachingService,
            mockHttpContextAccessor.Object,
            mockLogger.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantId"] = "tenant-123";
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        const string logicalKey = "user:profile";

        // Act
        await tenantAwareService.RemoveAsync(logicalKey);

        // Assert
        mockInnerService.Verify(x => x.RemoveAsync("tenant-123:user:profile"), Times.Once);
    }

    /// <summary>
    /// Tests that TenantAwareCachingService removes multiple keys scoped to specific tenant.
    /// </summary>
    [Fact]
    public async Task TenantAwareCachingService_RemoveAsync_MultipleKeys_RemovesTenantScopedKeys()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockLogger = new Mock<ILogger<TenantAwareCachingService>>();
        var mockInnerService = new Mock<ICacheProvider>();

        var cachingService = new CachingService(mockInnerService.Object, Mock.Of<ILogger<CachingService>>());
        var tenantAwareService = new TenantAwareCachingService(
            cachingService,
            mockHttpContextAccessor.Object,
            mockLogger.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantId"] = "tenant-123";
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var keys = new[] { "key1", "key2", "key3" };

        // Act
        await tenantAwareService.RemoveAsync(keys);

        // Assert
        mockInnerService.Verify(x => x.RemoveAsync("tenant-123:key1"), Times.Once);
        mockInnerService.Verify(x => x.RemoveAsync("tenant-123:key2"), Times.Once);
        mockInnerService.Verify(x => x.RemoveAsync("tenant-123:key3"), Times.Once);
    }

    /// <summary>
    /// Tests that TenantAwareCachingService sets keys scoped to specific tenant.
    /// </summary>
    [Fact]
    public async Task TenantAwareCachingService_SetAsync_SetsTenantScopedKey()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockLogger = new Mock<ILogger<TenantAwareCachingService>>();
        var mockInnerService = new Mock<ICacheProvider>();

        var cachingService = new CachingService(mockInnerService.Object, Mock.Of<ILogger<CachingService>>());
        var tenantAwareService = new TenantAwareCachingService(
            cachingService,
            mockHttpContextAccessor.Object,
            mockLogger.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantId"] = "tenant-123";
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        const string logicalKey = "user:profile";
        const string value = "profile data";
        var expiration = TimeSpan.FromMinutes(10);

        // Act
        await tenantAwareService.SetAsync(logicalKey, value, expiration);

        // Assert
        mockInnerService.Verify(x => x.SetAsync("tenant-123:user:profile", value, expiration), Times.Once);
    }

    #endregion
}
