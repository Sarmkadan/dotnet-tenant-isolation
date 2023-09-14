#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;
using TenantIsolation.Exceptions;

namespace TenantIsolation.Utilities;

/// <summary>
/// Utility class for common validation operations across the framework
/// Centralizes validation logic to ensure consistent enforcement of business rules
/// </summary>
public static class ValidationUtility
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(250)
    );

    private static readonly Regex SlugRegex = new(
        @"^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(250)
    );

    private static readonly Regex GuidRegex = new(
        @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(250)
    );

    /// <summary>
    /// Validate email format
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            return EmailRegex.IsMatch(email) && email.Length <= 254;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate tenant slug format
    /// Slug must be lowercase alphanumeric with hyphens, 3-63 characters
    /// </summary>
    public static bool IsValidSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        try
        {
            return SlugRegex.IsMatch(slug) && slug.Length >= 3 && slug.Length <= 63;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate GUID format
    /// </summary>
    public static bool IsValidGuid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            return GuidRegex.IsMatch(value) && Guid.TryParse(value, out _);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate string is not empty or whitespace
    /// </summary>
    public static void RequireNotEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new TenantIsolationException($"{fieldName} is required and cannot be empty");
    }

    /// <summary>
    /// Validate string meets minimum length
    /// </summary>
    public static void RequireMinLength(string? value, int minLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < minLength)
            throw new TenantIsolationException($"{fieldName} must be at least {minLength} characters");
    }

    /// <summary>
    /// Validate string does not exceed maximum length
    /// </summary>
    public static void RequireMaxLength(string? value, int maxLength, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Length > maxLength)
            throw new TenantIsolationException($"{fieldName} cannot exceed {maxLength} characters");
    }

    /// <summary>
    /// Validate string is within length range
    /// </summary>
    public static void RequireLengthBetween(string? value, int minLength, int maxLength, string fieldName)
    {
        RequireMinLength(value, minLength, fieldName);
        RequireMaxLength(value, maxLength, fieldName);
    }

    /// <summary>
    /// Validate email format, throw if invalid
    /// </summary>
    public static void RequireValidEmail(string? email)
    {
        RequireNotEmpty(email, nameof(email));
        if (!IsValidEmail(email))
            throw new TenantIsolationException($"'{email}' is not a valid email address");
    }

    /// <summary>
    /// Validate slug format, throw if invalid
    /// </summary>
    public static void RequireValidSlug(string? slug)
    {
        RequireNotEmpty(slug, nameof(slug));
        if (!IsValidSlug(slug))
            throw new TenantIsolationException($"'{slug}' is not a valid slug (use lowercase alphanumeric with hyphens, 3-63 chars)");
    }

    /// <summary>
    /// Validate GUID, throw if invalid
    /// </summary>
    public static void RequireValidGuid(string? value, string fieldName)
    {
        RequireNotEmpty(value, fieldName);
        if (!IsValidGuid(value))
            throw new TenantIsolationException($"'{value}' is not a valid GUID");
    }

    /// <summary>
    /// Validate object is not null
    /// </summary>
    public static void RequireNotNull(object? value, string fieldName)
    {
        if (value == null)
            throw new TenantIsolationException($"{fieldName} cannot be null");
    }

    /// <summary>
    /// Validate integer is positive
    /// </summary>
    public static void RequirePositive(int value, string fieldName)
    {
        if (value <= 0)
            throw new TenantIsolationException($"{fieldName} must be greater than zero");
    }

    /// <summary>
    /// Validate integer is within range
    /// </summary>
    public static void RequireRange(int value, int minValue, int maxValue, string fieldName)
    {
        if (value < minValue || value > maxValue)
            throw new TenantIsolationException($"{fieldName} must be between {minValue} and {maxValue}");
    }

    /// <summary>
    /// Validate date is not in past
    /// Useful for subscription and license validation
    /// </summary>
    public static void RequireFutureDate(DateTime dateTime, string fieldName)
    {
        if (dateTime <= DateTime.UtcNow)
            throw new TenantIsolationException($"{fieldName} must be a future date");
    }

    /// <summary>
    /// Validate date is not in future
    /// </summary>
    public static void RequirePastDate(DateTime dateTime, string fieldName)
    {
        if (dateTime > DateTime.UtcNow)
            throw new TenantIsolationException($"{fieldName} cannot be a future date");
    }

    /// <summary>
    /// Validate date range is valid (start before end)
    /// </summary>
    public static void RequireValidDateRange(DateTime startDate, DateTime endDate, string startFieldName, string endFieldName)
    {
        if (startDate >= endDate)
            throw new TenantIsolationException($"{startFieldName} must be before {endFieldName}");
    }

    /// <summary>
    /// Validate URL format
    /// </summary>
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validate URL, throw if invalid
    /// </summary>
    public static void RequireValidUrl(string? url, string fieldName)
    {
        RequireNotEmpty(url, fieldName);
        if (!IsValidUrl(url))
            throw new TenantIsolationException($"'{url}' is not a valid URL");
    }

    /// <summary>
    /// Validate enum value is defined
    /// </summary>
    public static void RequireValidEnum<T>(T value) where T : struct, Enum
    {
        if (!Enum.IsDefined(typeof(T), value))
            throw new TenantIsolationException($"'{value}' is not a valid {typeof(T).Name}");
    }
}
