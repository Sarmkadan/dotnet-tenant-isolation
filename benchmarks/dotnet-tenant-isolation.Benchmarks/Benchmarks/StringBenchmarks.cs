#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================
using BenchmarkDotNet.Attributes;
using TenantIsolation.Utilities;

namespace TenantIsolation.Benchmarks;

/// <summary>
/// Benchmarks for string utility operations.
/// Measures the performance of text processing utilities used throughout the framework.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class StringBenchmarks
{
    private const string TestStringAscii = "Hello World Test 123";
    private const string TestStringUnicode = "Héllo Wörld Tëst 123";
    private const string TestEmail = "user.name+tag@example.com";
    private const string TestUrl = "https://example.com/path?query=value#fragment";
    private const string TestPascalCase = "HelloWorldTestCase";
    private const string TestCamelCase = "helloWorldTestCase";
    private const string TestSpecialChars = "Hello_World-Test@123!";

    /// <summary>
    /// Baseline: Convert ASCII string to slug format.
    /// This is the most common scenario for tenant identifier generation.
    /// </summary>
    [Benchmark(Baseline = true)]
    public string ToSlug_Ascii()
    {
        return TestStringAscii.ToSlug();
    }

    /// <summary>
    /// Convert Unicode string to slug format.
    /// Tests international character handling.
    /// </summary>
    [Benchmark]
    public string ToSlug_Unicode()
    {
        return TestStringUnicode.ToSlug();
    }

    /// <summary>
    /// Get deterministic hash code for a string.
    /// Used for consistent key generation across sessions.
    /// </summary>
    [Benchmark]
    public int GetDeterministicHashCode()
    {
        return TestStringAscii.GetDeterministicHashCode();
    }

    /// <summary>
    /// Mask sensitive data in a string (email).
    /// Used for audit logging and display.
    /// </summary>
    [Benchmark]
    public string MaskSensitiveData()
    {
        return TestEmail.MaskSensitiveData();
    }

    /// <summary>
    /// Convert PascalCase to human-readable format.
    /// Used for display purposes.
    /// </summary>
    [Benchmark]
    public string ToHumanReadable()
    {
        return TestPascalCase.ToHumanReadable();
    }

    /// <summary>
    /// Remove special characters from string.
    /// Used for sanitizing input.
    /// </summary>
    [Benchmark]
    public string RemoveSpecialCharacters()
    {
        return TestSpecialChars.RemoveSpecialCharacters();
    }

    /// <summary>
    /// Validate email format.
    /// Used for tenant admin email validation.
    /// </summary>
    [Benchmark]
    public bool IsValidEmail()
    {
        return TestEmail.IsValidEmail();
    }

    /// <summary>
    /// Validate URL format.
    /// Used for webhook and external API validation.
    /// </summary>
    [Benchmark]
    public bool IsValidUrl()
    {
        return TestUrl.IsValidUrl();
    }

    /// <summary>
    /// Convert camelCase to PascalCase.
    /// Used for consistent naming conventions.
    /// </summary>
    [Benchmark]
    public string ToPascalCase()
    {
        return TestCamelCase.ToPascalCase();
    }
}