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
    public static string ToSlug(this string input)
    {
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
    public static string Truncate(this string input, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        return input[..(maxLength - suffix.Length)] + suffix;
    }

    /// <summary>
    /// Check if string is a valid email format (basic validation).
    /// </summary>
    public static bool IsValidEmail(this string input)
    {
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
    public static bool IsValidUrl(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return Uri.TryCreate(input, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Safe substring extraction that handles out-of-bounds gracefully.
    /// </summary>
    public static string SafeSubstring(this string input, int startIndex, int length)
    {
        if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
            return string.Empty;

        if (startIndex < 0)
            startIndex = 0;

        if (startIndex + length > input.Length)
            length = input.Length - startIndex;

        return input.Substring(startIndex, length);
    }

    /// <summary>
    /// Remove special characters from string, keeping only alphanumeric.
    /// </summary>
    public static string RemoveSpecialCharacters(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return NonAlphanumericRegex().Replace(input, string.Empty);
    }

    /// <summary>
    /// Mask sensitive parts of string (e.g., email, phone number).
    /// Used for audit logging and display without exposing full PII.
    /// </summary>
    public static string MaskSensitiveData(this string input, int visibleCharacters = 3)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= visibleCharacters)
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
    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpperInvariant(input[0]) + input[1..];
    }

    /// <summary>
    /// Convert PascalCase or camelCase to space-separated words.
    /// </summary>
    public static string ToHumanReadable(this string input)
    {
        if (string.IsNullOrEmpty(input))
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
    public static int GetDeterministicHashCode(this string input)
    {
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
