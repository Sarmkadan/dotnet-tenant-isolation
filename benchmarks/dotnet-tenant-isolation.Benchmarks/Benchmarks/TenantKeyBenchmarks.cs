#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================
using BenchmarkDotNet.Attributes;
using TenantIsolation.Caching;

namespace TenantIsolation.Benchmarks;

/// <summary>
/// Benchmarks for tenant key generation and subdomain resolution.
/// Measures the performance of tenant-scoped operations and key building.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class TenantKeyBenchmarks
{
    private const string TestTenantId = "f47ac10b-58cc-4372-a567-0e02b2c3d479";
    private const string TestResource = "user-preferences";
    private const string TestSubdomain = "acme-corp.example.com";
    private const string TestTenantSlug = "acme-corp";
    private readonly string[] _reservedTenants = ["admin", "system", "root", "default", "public"];

    /// <summary>
    /// Baseline: Build tenant-aware key using string.Concat.
    /// This is the traditional approach.
    /// </summary>
    [Benchmark(Baseline = true)]
    public string TenantAwareKey_StringConcat()
    {
        return string.Concat("tenant:", TestTenantId, ":", TestResource);
    }

    /// <summary>
    /// Build tenant-aware key using string interpolation.
    /// Modern C# approach.
    /// </summary>
    [Benchmark]
    public string TenantAwareKey_Interpolation()
    {
        return $"tenant:{TestTenantId}:{TestResource}";
    }

    /// <summary>
    /// Build cache key with tenant and resource using CacheKeyBuilder.
    /// Recommended approach for tenant isolation.
    /// </summary>
    [Benchmark]
    public string CacheKeyBuilder_TenantAndResource()
    {
        var builder = new CacheKeyBuilder("tenant-cache")
            .WithTenant(TestTenantId)
            .Add(TestResource);
        return builder.Build();
    }

    /// <summary>
    /// Check if tenant ID exists in reserved set using FrozenSet (tenant hit).
    /// FrozenSet provides O(1) lookup with minimal overhead.
    /// </summary>
    [Benchmark]
    public bool FrozenSet_Contains_ReservedHit()
    {
        var set = System.Collections.Frozen.FrozenSet.ToFrozenSet(_reservedTenants);
        return set.Contains(TestTenantSlug);
    }

    /// <summary>
    /// Check if tenant ID exists in reserved set using FrozenSet (tenant miss).
    /// Tests the negative path.
    /// </summary>
    [Benchmark]
    public bool FrozenSet_Contains_ReservedMiss()
    {
        var set = System.Collections.Frozen.FrozenSet.ToFrozenSet(_reservedTenants);
        return set.Contains("nonexistent-tenant");
    }

    /// <summary>
    /// Extract tenant subdomain using IndexOf (fastest approach).
    /// Used in subdomain-based tenant resolution.
    /// </summary>
    [Benchmark]
    public string SubdomainExtract_IndexOf()
    {
        var index = TestSubdomain.IndexOf('.');
        return index > 0 ? TestSubdomain[..index] : TestSubdomain;
    }

    /// <summary>
    /// Extract tenant subdomain using Split (more allocations).
    /// Alternative approach for comparison.
    /// </summary>
    [Benchmark]
    public string SubdomainExtract_Split()
    {
        return TestSubdomain.Split('.')[0];
    }

    /// <summary>
    /// Generate tenant-scoped key for configuration cache.
    /// Common pattern in multi-tenant applications.
    /// </summary>
    [Benchmark]
    public string GenerateTenantScopedKey()
    {
        return new CacheKeyBuilder("config")
            .WithTenant(TestTenantId)
            .Add("feature-flags")
            .Add("v2")
            .Build();
    }

    /// <summary>
    /// Generate tenant-scoped cache key with hash for parameterized queries.
    /// Used for caching queries with different parameters.
    /// </summary>
    [Benchmark]
    public string GenerateTenantScopedKey_WithHash()
    {
        var parameters = new { Page = 1, Size = 20, Sort = "name:asc" };
        return new CacheKeyBuilder("query-cache")
            .WithTenant(TestTenantId)
            .Add("products")
            .WithHash(parameters)
            .Build();
    }
}
