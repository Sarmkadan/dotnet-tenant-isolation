#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.ObjectPool;

namespace TenantIsolation.Utilities;

/// <summary>
/// String utility extension methods for common operations.
/// </summary>
public static partial class StringExtensions
{
    // Source-generated regexes: compiled once at startup, zero per-call overhead.
    [GeneratedRegex(@"[^\w\s-]")]
    private static partial Regex NonWordCharsRegex();

    [GeneratedRegex(@"[\s_]+")]
    private static partial Regex WhitespaceRunsRegex();

    [GeneratedRegex(@"^-+|-+$")]
    private static partial Regex LeadingTrailingDashesRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"([A-Z]+)")]
    private static partial Regex UpperCaseRunsRegex();

    // Shared pool reduces StringBuilder allocations on the slug-generation hot path.
    private static readonly ObjectPool<StringBuilder> StringBuilderPool =
        new DefaultObjectPoolProvider().CreateStringBuilderPool(initialCapacity: 128, maximumRetainedCapacity: 4096);

    /// <summary>
    /// Convert string to slug format (lowercase, hyphens instead of spaces).
    /// Used for tenant identifier generation to ensure URL-safe values.
    /// </summary>
    /// <param name="input">The input string to convert to a slug.</param>
    /// <returns>A URL-safe slug string, or empty string if input is null or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static string ToSlug(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalizedString = input.Normalize(NormalizationForm.FormD);
        var sb = StringBuilderPool.Get();
        try
        {
            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            var result = sb.ToString().Normalize(NormalizationForm.FormC);
            result = NonWordCharsRegex().Replace(result, string.Empty);
            result = WhitespaceRunsRegex().Replace(result, "-");
            result = LeadingTrailingDashesRegex().Replace(result, string.Empty);
            return result.ToLowerInvariant();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    /// <summary>
    /// Truncate string to specified length with ellipsis.
    /// </summary>
    /// <param name="input">The input string to truncate.</param>
    /// <param name="maxLength">Maximum length of the resulting string (including suffix).</param>
    /// <param name="suffix">The suffix to append when truncating (default: "...").</param>
    /// <returns>The truncated string with suffix, or the original string if it's shorter than maxLength.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    public static string Truncate(this string input, int maxLength, string suffix = "...")
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);
        ArgumentNullException.ThrowIfNull(suffix);

        if (input.Length <= maxLength)
            return input;

        return input[..(maxLength - suffix.Length)] + suffix;
    }

    /// <summary>
    /// Check if string is a valid email format (basic validation).
    /// </summary>
    /// <param name="input">The email address to validate.</param>
    /// <returns>True if the string is a valid email format; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static bool IsValidEmail(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(input);
            return addr.Address == input;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if string is a valid URL.
    /// </summary>
    /// <param name="input">The URL string to validate.</param>
    /// <returns>True if the string is a valid HTTP/HTTPS URL; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static bool IsValidUrl(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input))
            return false;

        return Uri.TryCreate(input, UriKind.Absolute, out var uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Safe substring extraction that handles out-of-bounds gracefully.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="startIndex">The zero-based starting character position.</param>
    /// <param name="length">The number of characters to return.</param>
    /// <returns>A substring starting at startIndex with the specified length, or empty string if out of bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startIndex"/> or <paramref name="length"/> is negative.</exception>
    public static string SafeSubstring(this string input, int startIndex, int length)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (startIndex >= input.Length)
            return string.Empty;

        if (startIndex + length > input.Length)
            length = input.Length - startIndex;

        return input.Substring(startIndex, length);
    }

    /// <summary>
    /// Remove special characters from string, keeping only alphanumeric.
    /// </summary>
    /// <param name="input">The input string to process.</param>
    /// <returns>A string containing only alphanumeric characters, or empty string if input is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static string RemoveSpecialCharacters(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0)
            return string.Empty;

        return NonAlphanumericRegex().Replace(input, string.Empty);
    }

    /// <summary>
    /// Mask sensitive parts of string (e.g., email, phone number).
    /// Used for audit logging and display without exposing full PII.
    /// </summary>
    /// <param name="input">The sensitive data to mask.</param>
    /// <param name="visibleCharacters">Number of characters to leave unmasked at the start (default: 3).</param>
    /// <returns>A masked version of the input string with asterisks replacing sensitive characters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="visibleCharacters"/> is negative.</exception>
    public static string MaskSensitiveData(this string input, int visibleCharacters = 3)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegative(visibleCharacters);

        if (input.Length <= visibleCharacters)
            return new string('*', Math.Max(input.Length, 1));

        // string.Create + Span<char>: one allocation instead of two
        // (substring + star-string) for the common case where input is longer
        // than visibleCharacters.
        return string.Create(input.Length, (input, visibleCharacters), static (span, state) =>
        {
            var (src, visible) = state;
            src.AsSpan(0, visible).CopyTo(span);
            span[visible..].Fill('*');
        });
    }

    /// <summary>
    /// Convert camelCase to PascalCase.
    /// </summary>
    /// <param name="input">The input string in camelCase format.</param>
    /// <returns>The input string converted to PascalCase, or empty string if input is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static string ToPascalCase(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0)
            return string.Empty;

        return char.ToUpperInvariant(input[0]) + input[1..];
    }

    /// <summary>
    /// Convert PascalCase or camelCase to space-separated words.
    /// </summary>
    /// <param name="input">The input string in PascalCase or camelCase format.</param>
    /// <returns>A human-readable string with spaces between words, or the original string if null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static string ToHumanReadable(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0)
            return input;

        var result = UpperCaseRunsRegex().Replace(input, " $1").Trim();
        if (result.Length == 0)
            return result;

        // string.Create avoids the two-string allocation chain of
        // result[0].ToString().ToUpperInvariant() + result[1..].
        return string.Create(result.Length, result, static (span, src) =>
        {
            span[0] = char.ToUpperInvariant(src[0]);
            src.AsSpan(1).CopyTo(span[1..]);
        });
    }

    /// <summary>
    /// Get string hash code deterministically (for consistency across sessions).
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <returns>A deterministic hash code for the input string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static int GetDeterministicHashCode(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        unchecked
        {
            int hash1 = 5381;
            int hash2 = hash1;

            for (int i = 0; i < input.Length && input[i] != '\0'; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ input[i];
                if (i + 1 < input.Length && input[i + 1] != '\0')
                    hash2 = ((hash2 << 5) + hash2) ^ input[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }
}