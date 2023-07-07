// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace TenantIsolation.Caching;

/// <summary>
/// High-level caching service for application-specific caching operations.
/// Provides convenient methods for caching frequently accessed data.
/// Implements cache-aside pattern with automatic expiration.
/// </summary>
public interface ICachingService
{
    /// <summary>
    /// Get or fetch value, storing in cache for future requests.
    /// </summary>
    Task<T?> GetOrFetchAsync<T>(string key, Func<Task<T>> fetchFunc, TimeSpan? expiration = null);

    // ValueTask used for all operations that complete synchronously on the hot path
    // (ConcurrentDictionary lookup / write), eliminating Task heap allocation and
    // the async state-machine overhead on every cache hit.
    ValueTask<T?> GetAsync<T>(string key);
    ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    ValueTask RemoveAsync(string key);
    ValueTask RemoveAsync(params string[] keys);
    ValueTask ClearAsync();
    ValueTask<CacheStatistics> GetStatisticsAsync();
}

public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public double HitRate => (CacheHits + CacheMisses) > 0
        ? (double)CacheHits / (CacheHits + CacheMisses)
        : 0;
}

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
    /// Get value from cache, or fetch and cache if not present.
    /// Implements the cache-aside pattern for efficient data retrieval.
    /// </summary>
    public async Task<T?> GetOrFetchAsync<T>(string key, Func<Task<T>> fetchFunc, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return await fetchFunc();

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
            var freshValue = await fetchFunc();

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

    // Non-async methods: return the provider's pre-completed ValueTask directly —
    // no state-machine allocation, no Task wrapper on the hot path.

    public ValueTask<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ValueTask.FromResult<T?>(default);

        return _cacheProvider.GetAsync<T>(key);
    }

    public ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ValueTask.CompletedTask;

        var vt = _cacheProvider.SetAsync(key, value, expiration);
        _logger.LogDebug("Set cache value for key '{Key}' with expiration {Expiration}",
            key, expiration ?? TimeSpan.MaxValue);
        return vt;
    }

    public ValueTask RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ValueTask.CompletedTask;

        var vt = _cacheProvider.RemoveAsync(key);
        _logger.LogDebug("Removed cache entry for key '{Key}'", key);
        return vt;
    }

    public async ValueTask RemoveAsync(params string[] keys)
    {
        foreach (var k in keys.Where(k => !string.IsNullOrWhiteSpace(k)))
            await _cacheProvider.RemoveAsync(k);

        _logger.LogDebug("Removed {Count} cache entries", keys.Length);
    }

    public ValueTask ClearAsync()
    {
        var vt = _cacheProvider.ClearAsync();
        _cacheHits = 0;
        _cacheMisses = 0;
        _logger.LogInformation("Cleared all cache entries and statistics");
        return vt;
    }

    public ValueTask<CacheStatistics> GetStatisticsAsync()
        => ValueTask.FromResult(new CacheStatistics
        {
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses
        });
}

/// <summary>
/// Tenant-aware caching service.
/// Automatically scopes cache keys to the current tenant, preventing cross-tenant cache hits.
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

        // string.Concat avoids the intermediate format string allocation of $"{tenantId}:{key}".
        return string.Concat(tenantId, ":", key);
    }

    public Task<T?> GetOrFetchAsync<T>(string key, Func<Task<T>> fetchFunc, TimeSpan? expiration = null)
        => _innerService.GetOrFetchAsync(GetTenantAwareKey(key), fetchFunc, expiration);

    public ValueTask<T?> GetAsync<T>(string key)
        => _innerService.GetAsync<T>(GetTenantAwareKey(key));

    public ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        => _innerService.SetAsync(GetTenantAwareKey(key), value, expiration);

    public ValueTask RemoveAsync(string key)
        => _innerService.RemoveAsync(GetTenantAwareKey(key));

    public ValueTask RemoveAsync(params string[] keys)
        => _innerService.RemoveAsync(keys.Select(GetTenantAwareKey).ToArray());

    public ValueTask ClearAsync()
        => _innerService.ClearAsync();

    public ValueTask<CacheStatistics> GetStatisticsAsync()
        => _innerService.GetStatisticsAsync();
}

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
