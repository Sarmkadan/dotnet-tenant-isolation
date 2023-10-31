#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace TenantIsolation.Utilities;

/// <summary>
/// Validation helpers for string operations from <see cref="StringExtensions"/>.
/// Provides comprehensive validation for string operations including email, URL,
/// and general string format validation.
/// </summary>
public static class StringExtensionsValidation
{
    /// <summary>
    /// Validates string extension method behaviors and returns a list of human-readable problems.
    /// </summary>
    /// <param name="input">The input string to validate against StringExtensions methods</param>
    /// <returns>List of validation problems; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if input is null</exception>
    public static IReadOnlyList<string> Validate(this string? input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var problems = new List<string>();

        // Validate ToSlug behavior
        var slug = input.ToSlug();
        if (string.IsNullOrWhiteSpace(slug))
        {
            problems.Add("ToSlug() returned empty or whitespace string");
        }
        else if (slug.Any(c => char.IsWhiteSpace(c)))
        {
            problems.Add("ToSlug() returned string containing whitespace characters");
        }

        // Validate Truncate behavior with various edge cases
        var truncatedEmpty = input.Truncate(0);
        if (truncatedEmpty.Length != 0)
        {
            problems.Add("Truncate(0) should return empty string");
        }

        var truncatedShort = input.Truncate(5);
        if (truncatedShort.Length > 5)
        {
            problems.Add("Truncate() returned string longer than maxLength parameter");
        }

        // Validate IsValidEmail behavior
        var validEmailResult = "test@example.com".IsValidEmail();
        if (!validEmailResult)
        {
            problems.Add("IsValidEmail() failed basic valid email test");
        }

        if (!"".IsValidEmail())
        {
            problems.Add("IsValidEmail() should return false for empty string");
        }

        if ("invalid-email".IsValidEmail())
        {
            problems.Add("IsValidEmail() should return false for invalid email format");
        }

        // Validate IsValidUrl behavior
        var validUrlResult = "https://example.com".IsValidUrl();
        if (!validUrlResult)
        {
            problems.Add("IsValidUrl() failed basic valid URL test");
        }

        if (!"".IsValidUrl())
        {
            problems.Add("IsValidUrl() should return false for empty string");
        }

        if ("not-a-url".IsValidUrl())
        {
            problems.Add("IsValidUrl() should return false for invalid URL format");
        }

        // Validate SafeSubstring behavior
        var outOfBounds = input.SafeSubstring(100, 10);
        if (!string.IsNullOrEmpty(outOfBounds))
        {
            problems.Add("SafeSubstring() with out-of-bounds indices should return empty string");
        }

        var negativeStart = input.SafeSubstring(-1, 5);
        if (negativeStart.Length > 5)
        {
            problems.Add("SafeSubstring() with negative startIndex should handle gracefully");
        }

        // Validate RemoveSpecialCharacters behavior
        var cleaned = input.RemoveSpecialCharacters();
        if (cleaned.Any(c => !char.IsLetterOrDigit(c)))
        {
            problems.Add("RemoveSpecialCharacters() returned string containing non-alphanumeric characters");
        }

        // Validate MaskSensitiveData behavior
        var masked = input.MaskSensitiveData();
        if (string.IsNullOrEmpty(masked))
        {
            problems.Add("MaskSensitiveData() returned empty string");
        }
        else if (masked.Any(c => c != '*'))
        {
            problems.Add("MaskSensitiveData() should mask with asterisks");
        }
        else if (input.Length > 3 && masked.Count(c => c == '*') != input.Length - 3)
        {
            problems.Add("MaskSensitiveData() should preserve only specified visible characters");
        }

        // Validate ToPascalCase behavior
        var pascal = input.ToPascalCase();
        if (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(pascal))
        {
            if (char.IsLower(pascal[0]))
            {
                problems.Add("ToPascalCase() should return string starting with uppercase character");
            }
        }

        // Validate ToHumanReadable behavior
        var human = input.ToHumanReadable();
        if (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(human))
        {
            if (char.IsLower(human[0]))
            {
                problems.Add("ToHumanReadable() should return string starting with uppercase character");
            }
        }

        // Validate GetDeterministicHashCode behavior
        try
        {
            var hash1 = "test".GetDeterministicHashCode();
            var hash2 = "test".GetDeterministicHashCode();
            if (hash1 != hash2)
            {
                problems.Add("GetDeterministicHashCode() should return consistent value for same input");
            }
        }
        catch
        {
            problems.Add("GetDeterministicHashCode() threw exception for valid input");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if a string is valid according to StringExtensions method behaviors.
    /// </summary>
    /// <param name="input">The input string to check</param>
    /// <returns>True if valid, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown if input is null</exception>
    public static bool IsValid(this string? input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a string is valid according to StringExtensions method behaviors,
    /// throwing an exception if not.
    /// </summary>
    /// <param name="input">The input string to validate</param>
    /// <exception cref="ArgumentException">Thrown if input is invalid, with detailed error message</exception>
    /// <exception cref="ArgumentNullException">Thrown if input is null</exception>
    public static void EnsureValid(this string? input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var problems = input.Validate();
        if (problems.Count == 0)
            return;

        throw new ArgumentException(
            $"StringExtensions validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
    }
}