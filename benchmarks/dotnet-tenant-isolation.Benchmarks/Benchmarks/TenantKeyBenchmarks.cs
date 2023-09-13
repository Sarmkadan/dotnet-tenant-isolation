// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Frozen;
using BenchmarkDotNet.Attributes;
using TenantIsolation.Caching;

namespace TenantIsolation.Benchmarks;

/// <summary>
/// Benchmarks for tenant-scoped key building and subdomain resolution checks.
/// Measures string.Concat vs interpolation for key prefix assembly, and
/// FrozenSet&lt;string&gt; lookup latency for the reserved-subdomain guard.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class TenantKeyBenchmarks
{
    private const string TenantIdStr  = "f47ac10b-58cc-4372-a567-0e02b2c3d479";
    private const string ResourceKey  = "configuration:defaults";
    private const string KnownSubdomain    = "acme";
    private const string ReservedSubdomain = "api";

    // FrozenSet used by TenantResolutionService – reproduced here for isolated measurement.
    private static readonly FrozenSet<string> ReservedSubdomains =
        FrozenSet.Create(StringComparer.OrdinalIgnoreCase,
            "www", "api", "mail", "smtp", "ftp", "admin", "app",
            "static", "cdn", "assets", "dev", "staging", "prod", "auth", "login");

    /// <summary>
    /// Baseline: string.Concat to build a tenant-scoped key (used inside
    /// TenantAwareCachingService.GetTenantAwareKey).
    /// </summary>
    [Benchmark(Baseline = true)]
    public string TenantAwareKey_Concat()
        => string.Concat(TenantIdStr, ":", ResourceKey);

    /// <summary>
    /// Interpolation equivalent — shows the allocation difference
    /// vs string.Concat for the same two-part join.
    /// </summary>
    [Benchmark]
    public string TenantAwareKey_Interpolation()
        => $"{TenantIdStr}:{ResourceKey}";

    /// <summary>
    /// Full CacheKeyBuilder pipeline: prefix + WithTenant + Add.
    /// Covers the ArrayPool path in Build().
    /// </summary>
    [Benchmark]
    public string CacheKeyBuilder_TenantResource()
        => new CacheKeyBuilder("app")
            .WithTenant(TenantIdStr)
            .Add(ResourceKey)
            .Build();

    /// <summary>
    /// FrozenSet.Contains for a reserved subdomain — expected sub-10 ns.
    /// </summary>
    [Benchmark]
    public bool FrozenSet_ReservedHit()
        => ReservedSubdomains.Contains(ReservedSubdomain);

    /// <summary>
    /// FrozenSet.Contains for a normal tenant subdomain — miss path.
    /// </summary>
    [Benchmark]
    public bool FrozenSet_ReservedMiss()
        => ReservedSubdomains.Contains(KnownSubdomain);

    /// <summary>
    /// IndexOf-based subdomain extraction (used in ResolveTenantFromSubdomainAsync)
    /// vs the previous Split-based approach. Avoids string[] allocation entirely.
    /// </summary>
    [Benchmark]
    public string SubdomainExtract_IndexOf()
    {
        const string host = "acme.example.com";
        var dot = host.IndexOf('.');
        return dot > 0 ? host[..dot] : host;
    }

    [Benchmark]
    public string SubdomainExtract_Split()
    {
        const string host = "acme.example.com";
        var parts = host.Split('.');
        return parts.Length >= 2 ? parts[0] : host;
    }
}
