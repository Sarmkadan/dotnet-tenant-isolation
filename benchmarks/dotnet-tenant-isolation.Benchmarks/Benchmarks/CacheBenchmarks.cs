// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using TenantIsolation.Caching;

namespace TenantIsolation.Benchmarks;

/// <summary>
/// Benchmarks for the in-memory cache layer.
/// Measures the impact of removing Task.Run() wrappers and returning
/// pre-completed ValueTasks directly from ConcurrentDictionary operations.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class CacheBenchmarks
{
    private MemoryCacheProvider _provider = null!;

    private const string HitKey   = "tenant:f47ac10b:config:defaults";
    private const string MissKey  = "tenant:f47ac10b:config:missing";
    private const string UpsertKey = "tenant:f47ac10b:config:upsert";

    [GlobalSetup]
    public async Task Setup()
    {
        _provider = new MemoryCacheProvider();
        await _provider.SetAsync(HitKey, new CachedTenant
        {
            Id = Guid.Parse("f47ac10b-58cc-4372-a567-0e02b2c3d479"),
            Name = "Acme Corp",
            Slug = "acme-corp"
        });
    }

    /// <summary>
    /// Hot path: reading a value that is present and not expired.
    /// Expected to be a few dozen nanoseconds – a lock-free ConcurrentDictionary
    /// lookup returning a pre-completed ValueTask.
    /// </summary>
    [Benchmark(Baseline = true)]
    public ValueTask<CachedTenant?> GetAsync_Hit()
        => _provider.GetAsync<CachedTenant>(HitKey);

    /// <summary>
    /// Miss path: key not present in dictionary.
    /// </summary>
    [Benchmark]
    public ValueTask<CachedTenant?> GetAsync_Miss()
        => _provider.GetAsync<CachedTenant>(MissKey);

    /// <summary>
    /// Upsert path: AddOrUpdate on an existing key (realistic update scenario).
    /// </summary>
    [Benchmark]
    public ValueTask SetAsync_Upsert()
        => _provider.SetAsync(UpsertKey, new CachedTenant
        {
            Id = Guid.NewGuid(),
            Name = "Updated Tenant",
            Slug = "updated-tenant"
        });

    /// <summary>
    /// CacheKeyBuilder simple path: prefix + tenant segment + one resource segment.
    /// </summary>
    [Benchmark]
    public string CacheKeyBuilder_Simple()
        => new CacheKeyBuilder("tenant")
            .WithTenant("f47ac10b-58cc-4372-a567-0e02b2c3d479")
            .Add("configuration:defaults")
            .Build();

    /// <summary>
    /// CacheKeyBuilder with hash: includes JSON serialization of a parameter object.
    /// Shows the relative cost of the hashing step vs. the rest of key building.
    /// </summary>
    [Benchmark]
    public string CacheKeyBuilder_WithHash()
        => new CacheKeyBuilder("tenant")
            .WithTenant("f47ac10b-58cc-4372-a567-0e02b2c3d479")
            .Add("search")
            .WithHash(new { Page = 1, Size = 50, Status = "active" })
            .Build();

    [GlobalCleanup]
    public void Cleanup() => _provider.Dispose();

    public sealed record CachedTenant
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = "";
        public string Slug { get; init; } = "";
    }
}
