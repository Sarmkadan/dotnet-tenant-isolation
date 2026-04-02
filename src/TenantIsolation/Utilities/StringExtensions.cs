// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.RegularExpressions;

namespace TenantIsolation.Utilities;

/// <summary>
/// String utility extension methods for common operations
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Convert string to slug format (lowercase, hyphens instead of spaces)
    /// Used for tenant identifier generation to ensure URL-safe values
    /// </summary>
    public static string ToSlug(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalizedString = input.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                stringBuilder.Append(c);
        }

        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        result = Regex.Replace(result, @"[^\w\s-]", string.Empty);
        result = Regex.Replace(result, @"[\s_]+", "-");
        result = Regex.Replace(result, @"^-+|-+$", string.Empty);

        return result.ToLowerInvariant();
    }

    /// <summary>
    /// Truncate string to specified length with ellipsis
    /// </summary>
    public static string Truncate(this string input, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        return input[..(maxLength - suffix.Length)] + suffix;
    }

    /// <summary>
    /// Check if string is a valid email format (basic validation)
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
    /// Check if string is a valid URL
    /// </summary>
    public static bool IsValidUrl(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return Uri.TryCreate(input, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Safe substring extraction that handles out-of-bounds gracefully
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
    /// Remove special characters from string, keeping only alphanumeric
    /// </summary>
    public static string RemoveSpecialCharacters(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return Regex.Replace(input, @"[^a-zA-Z0-9]", string.Empty);
    }

    /// <summary>
    /// Mask sensitive parts of string (e.g., email, phone number)
    /// Used for audit logging and display without exposing full PII
    /// </summary>
    public static string MaskSensitiveData(this string input, int visibleCharacters = 3)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= visibleCharacters)
            return new string('*', Math.Max(input.Length, 1));

        return input[..visibleCharacters] + new string('*', input.Length - visibleCharacters);
    }

    /// <summary>
    /// Convert camelCase to PascalCase
    /// </summary>
    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpperInvariant(input[0]) + input[1..];
    }

    /// <summary>
    /// Convert PascalCase or camelCase to space-separated words
    /// </summary>
    public static string ToHumanReadable(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = Regex.Replace(input, "([A-Z]+)", " $1").Trim();
        return result[0].ToString().ToUpperInvariant() + result[1..];
    }

    /// <summary>
    /// Get string hash code deterministically (for consistency across sessions)
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
