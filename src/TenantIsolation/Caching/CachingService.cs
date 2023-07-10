// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace TenantIsolation.Caching;

/// <summary>
/// High-level caching service for application-specific caching operations
/// Provides convenient methods for caching frequently accessed data
/// Implements cache-aside pattern with automatic expiration
/// </summary>
public interface ICachingService
{
    /// <summary>
    /// Get or fetch value, storing in cache for future requests
    /// </summary>
    Task<T?> GetOrFetchAsync<T>(string key, Func<Task<T>> fetchFunc, TimeSpan? expiration = null);

    /// <summary>
    /// Get cached value
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Cache value
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Remove cached value
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Remove multiple cached values
    /// </summary>
    Task RemoveAsync(params string[] keys);

    /// <summary>
    /// Clear all cache
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync();
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public double HitRate => (CacheHits + CacheMisses) > 0
        ? (double)CacheHits / (CacheHits + CacheMisses)
        : 0;
}

/// <summary>
/// Caching service implementation
/// </summary>
public class CachingService : ICachingService
{
    private readonly ICacheProvider _cacheProvider;
    private readonly ILogger<CachingService> _logger;
    private int _cacheHits;
    private int _cacheMisses;

    public CachingService(ICacheProvider cacheProvider, ILogger<CachingService> logger)
    {
        _cacheProvider = cacheProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get value from cache, or fetch and cache if not present
    /// Implements the cache-aside pattern for efficient data retrieval
    /// </summary>
    public async Task<T?> GetOrFetchAsync<T>(string key, Func<Task<T>> fetchFunc, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return await fetchFunc();

        // Try to get from cache
        var cachedValue = await _cacheProvider.GetAsync<T>(key);
        if (cachedValue != null)
        {
            _cacheHits++;
            _logger.LogDebug("Cache hit for key '{Key}'", key);
            return cachedValue;
        }

        _cacheMisses++;
        _logger.LogDebug("Cache miss for key '{Key}'. Fetching fresh data...", key);

        try
        {
            // Fetch fresh value
            var freshValue = await fetchFunc();

            // Cache the value
            if (freshValue != null)
            {
                await _cacheProvider.SetAsync(key, freshValue, expiration);
                _logger.LogDebug("Cached value for key '{Key}'", key);
            }

            return freshValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching value for cache key '{Key}'", key);
            throw;
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return default;

        return await _cacheProvider.GetAsync<T>(key);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        await _cacheProvider.SetAsync(key, value, expiration);
        _logger.LogDebug("Set cache value for key '{Key}' with expiration {Expiration}",
            key, expiration ?? TimeSpan.MaxValue);
    }

    public async Task RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        await _cacheProvider.RemoveAsync(key);
        _logger.LogDebug("Removed cache entry for key '{Key}'", key);
    }

    public async Task RemoveAsync(params string[] keys)
    {
        var tasks = keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => _cacheProvider.RemoveAsync(k));

        await Task.WhenAll(tasks);
        _logger.LogDebug("Removed {Count} cache entries", keys.Length);
    }

    public async Task ClearAsync()
    {
        await _cacheProvider.ClearAsync();
        _cacheHits = 0;
        _cacheMisses = 0;
        _logger.LogInformation("Cleared all cache entries and statistics");
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        return await Task.FromResult(new CacheStatistics
        {
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses
        });
    }
}

/// <summary>
/// Tenant-aware caching service
/// Automatically applies tenant context to cache keys
/// </summary>
public class TenantAwareCachingService : ICachingService
{
    private readonly ICachingService _innerService;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly ILogger<TenantAwareCachingService> _logger;

    public TenantAwareCachingService(
        ICachingService innerService,
        IHttpContextAccessor contextAccessor,
        ILogger<TenantAwareCachingService> logger)
    {
        _innerService = innerService;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    private string GetTenantAwareKey(string key)
    {
        var tenantId = _contextAccessor.HttpContext?.Items["TenantId"]?.ToString();
        if (string.IsNullOrEmpty(tenantId))
            return key;

        return $"{tenantId}:{key}";
    }

    public async Task<T?> GetOrFetchAsync<T>(string key, Func<Task<T>> fetchFunc, TimeSpan? expiration = null)
    {
        var tenantAwareKey = GetTenantAwareKey(key);
        return await _innerService.GetOrFetchAsync(tenantAwareKey, fetchFunc, expiration);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var tenantAwareKey = GetTenantAwareKey(key);
        return await _innerService.GetAsync<T>(tenantAwareKey);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var tenantAwareKey = GetTenantAwareKey(key);
        await _innerService.SetAsync(key, value, expiration);
    }

    public async Task RemoveAsync(string key)
    {
        var tenantAwareKey = GetTenantAwareKey(key);
        await _innerService.RemoveAsync(tenantAwareKey);
    }

    public async Task RemoveAsync(params string[] keys)
    {
        var tenantAwareKeys = keys.Select(GetTenantAwareKey).ToArray();
        await _innerService.RemoveAsync(tenantAwareKeys);
    }

    public async Task ClearAsync()
    {
        await _innerService.ClearAsync();
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        return await _innerService.GetStatisticsAsync();
    }
}

/// <summary>
/// Extension methods for caching service registration
/// </summary>
public static class CachingServiceExtensions
{
    public static IServiceCollection AddCachingService(this IServiceCollection services)
    {
        services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
        services.AddScoped<ICachingService, CachingService>();
        return services;
    }

    public static IServiceCollection AddTenantAwareCachingService(this IServiceCollection services)
    {
        services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
        services.AddScoped<ICachingService, TenantAwareCachingService>();
        return services;
    }
}
