// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using TenantIsolation.Utilities;

namespace TenantIsolation.Benchmarks;

/// <summary>
/// Benchmarks for string utility operations.
/// Measures the impact of source-generated [GeneratedRegex] patterns and
/// ObjectPool&lt;StringBuilder&gt; reuse inside ToSlug.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class StringBenchmarks
{
    private const string AsciiInput   = "My New Tenant Name With Spaces 2024";
    private const string UnicodeInput = "Ünïcödé Tenant Ñäme — Ångström Corp";
    private const string EmailInput   = "admin.user@example-tenant.com";
    private const string PascalInput  = "TenantConfigurationService";

    /// <summary>
    /// ToSlug on a clean ASCII string — the common fast path.
    /// With GeneratedRegex + pooled StringBuilder the regex matching is compiled
    /// once at startup and the StringBuilder is reused across calls.
    /// </summary>
    [Benchmark(Baseline = true)]
    public string ToSlug_Ascii() => AsciiInput.ToSlug();

    /// <summary>
    /// ToSlug on a Unicode string — exercises the FormD normalisation loop.
    /// </summary>
    [Benchmark]
    public string ToSlug_Unicode() => UnicodeInput.ToSlug();

    /// <summary>
    /// Deterministic hash used by CacheKeyBuilder.WithHash() for string parameters.
    /// Pure arithmetic – no allocation expected.
    /// </summary>
    [Benchmark]
    public int GetDeterministicHashCode() => AsciiInput.GetDeterministicHashCode();

    /// <summary>
    /// PII masking used in audit log paths.
    /// </summary>
    [Benchmark]
    public string MaskSensitiveData() => EmailInput.MaskSensitiveData(visibleCharacters: 5);

    /// <summary>
    /// Human-readable label generation for display names.
    /// Exercises the UpperCaseRunsRegex generated pattern.
    /// </summary>
    [Benchmark]
    public string ToHumanReadable() => PascalInput.ToHumanReadable();

    /// <summary>
    /// Removing special characters — e.g., sanitising user-supplied input.
    /// </summary>
    [Benchmark]
    public string RemoveSpecialCharacters() => UnicodeInput.RemoveSpecialCharacters();
}
