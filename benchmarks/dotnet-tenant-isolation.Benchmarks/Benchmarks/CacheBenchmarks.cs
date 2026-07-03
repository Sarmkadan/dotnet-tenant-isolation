#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================
using BenchmarkDotNet.Attributes;
using TenantIsolation.Caching;
using TenantIsolation.Constants;

namespace TenantIsolation.Benchmarks;

/// <summary>
/// Benchmarks for cache layer operations.
/// Measures the performance of in-memory cache with different scenarios.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class CacheBenchmarks : IDisposable
{
    private MemoryCacheProvider? _cache;
    private string? _cacheKey;
    private string? _tenantId;
    private string? _userId;

    [GlobalSetup]
    public void Setup()
    {
        _cache = new MemoryCacheProvider();
        _cacheKey = "test:key:12345";
        _tenantId = Guid.NewGuid().ToString();
        _userId = Guid.NewGuid().ToString();

        // Pre-populate cache for "cache hit" scenarios
        _cache.SetAsync(_cacheKey, "test-value", TimeSpan.FromMinutes(30)).GetAwaiter().GetResult();

        // Pre-populate with tenant context
        var keyBuilder = new CacheKeyBuilder("tenant-cache")
            .WithTenant(_tenantId)
            .Add("user")
            .Add("preferences");
        var tenantKey = keyBuilder.Build();
        _cache.SetAsync(tenantKey, new { Theme = "dark", Locale = "en-US" }, TimeSpan.FromMinutes(30)).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Baseline: Get value from cache when key exists (cache hit).
    /// This is the most common scenario in production.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async ValueTask<object?> GetAsync_CacheHit()
    {
        return await _cache!.GetAsync<object>(_cacheKey!);
    }

    /// <summary>
    /// Get value from cache when key doesn't exist (cache miss).
    /// Tests the fallback path.
    /// </summary>
    [Benchmark]
    public async ValueTask<object?> GetAsync_CacheMiss()
    {
        return await _cache!.GetAsync<object>("nonexistent:key");
    }

    /// <summary>
    /// Set value in cache (upsert operation).
    /// Tests write path performance.
    /// </summary>
    [Benchmark]
    public async ValueTask SetAsync_Upsert()
    {
        await _cache!.SetAsync(_cacheKey!, "new-value", TimeSpan.FromMinutes(30));
    }

    /// <summary>
    /// Build cache key with simple segments (no tenant context).
    /// Tests the simplest key building scenario.
    /// </summary>
    [Benchmark]
    public string CacheKeyBuilder_Simple()
    {
        var builder = new CacheKeyBuilder("simple-cache")
            .Add("user")
            .Add("preferences")
            .Add("settings");
        return builder.Build();
    }

    /// <summary>
    /// Build cache key with tenant context.
    /// Tests tenant-isolated key building.
    /// </summary>
    [Benchmark]
    public string CacheKeyBuilder_WithTenant()
    {
        var builder = new CacheKeyBuilder("tenant-cache")
            .WithTenant(_tenantId)
            .Add("user")
            .Add("preferences");
        return builder.Build();
    }

    /// <summary>
    /// Build cache key with tenant, user, and hash segments.
    /// Tests complex key building with multiple contexts.
    /// </summary>
    [Benchmark]
    public string CacheKeyBuilder_WithTenantUserAndHash()
    {
        var builder = new CacheKeyBuilder("complex-cache")
            .WithTenant(_tenantId)
            .WithUser(_userId)
            .WithHash(new { Page = 1, Size = 10, Sort = "name" })
            .Add("products");
        return builder.Build();
    }

    /// <summary>
    /// Check if key exists in cache (cache hit).
    /// Very lightweight operation.
    /// </summary>
    [Benchmark]
    public async ValueTask<bool> ExistsAsync_CacheHit()
    {
        return await _cache!.ExistsAsync(_cacheKey!);
    }

    /// <summary>
    /// Check if key exists in cache (cache miss).
    /// Tests the negative path.
    /// </summary>
    [Benchmark]
    public async ValueTask<bool> ExistsAsync_CacheMiss()
    {
        return await _cache!.ExistsAsync("nonexistent:key");
    }

    /// <summary>
    /// Remove key from cache.
    /// Tests cleanup operations.
    /// </summary>
    [Benchmark]
    public async ValueTask RemoveAsync()
    {
        await _cache!.RemoveAsync(_cacheKey!);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cache?.Dispose();
    }

    public void Dispose()
    {
        Cleanup();
    }
}