#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace TenantIsolation.Utilities;

/// <summary>
/// Provides validation helpers for <see cref="TracingContext"/> instances
/// </summary>
public static class TracingContextValidation
{
    /// <summary>
    /// Validates a tracing context and returns a list of human-readable validation problems
    /// </summary>
    /// <param name="value">The tracing context to validate</param>
    /// <returns>List of validation problems; empty list if context is valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> Validate(this TracingContext value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate CorrelationId - must be a valid GUID string
        if (string.IsNullOrWhiteSpace(value.CorrelationId))
        {
            problems.Add("CorrelationId is null or empty");
        }
        else if (!IsValidGuidString(value.CorrelationId))
        {
            problems.Add("CorrelationId is not a valid GUID string");
        }

        // Validate TraceId - must be a valid ActivityTraceId string (32 hex chars)
        if (string.IsNullOrWhiteSpace(value.TraceId))
        {
            problems.Add("TraceId is null or empty");
        }
        else if (value.TraceId.Length != 32 || !value.TraceId.All(c => c.IsHexDigit()))
        {
            problems.Add("TraceId must be a 32-character hexadecimal string");
        }

        // Validate SpanId - must be a valid ActivitySpanId string (16 hex chars)
        if (string.IsNullOrWhiteSpace(value.SpanId))
        {
            problems.Add("SpanId is null or empty");
        }
        else if (value.SpanId.Length != 16 || !value.SpanId.All(c => c.IsHexDigit()))
        {
            problems.Add("SpanId must be a 16-character hexadecimal string");
        }

        // Validate ParentSpanId - if present, must be valid
        if (!string.IsNullOrEmpty(value.ParentSpanId))
        {
            if (value.ParentSpanId.Length != 16 || !value.ParentSpanId.All(c => c.IsHexDigit()))
            {
                problems.Add("ParentSpanId must be a 16-character hexadecimal string when present");
            }
        }

        // Validate RequestPath - if present, should be a valid HTTP path
        if (!string.IsNullOrEmpty(value.RequestPath))
        {
            if (value.RequestPath.Length > 2048)
            {
                problems.Add("RequestPath exceeds maximum length of 2048 characters");
            }
            else if (value.RequestPath.Any(c => char.IsControl(c)))
            {
                problems.Add("RequestPath contains control characters");
            }
        }

        // Validate TenantId - if present, should be valid
        if (value.TenantId.HasValue && value.TenantId.Value == Guid.Empty)
        {
            problems.Add("TenantId is set to Guid.Empty");
        }

        // Validate UserId - if present, should not contain invalid characters
        if (!string.IsNullOrEmpty(value.UserId))
        {
            if (value.UserId.Length > 256)
            {
                problems.Add("UserId exceeds maximum length of 256 characters");
            }
            else if (value.UserId.Any(c => char.IsControl(c)))
            {
                problems.Add("UserId contains control characters");
            }
        }

        // Validate StartTime - should not be default(DateTime)
        if (value.StartTime == default)
        {
            problems.Add("StartTime is set to default DateTime");
        }
        else if (value.StartTime > DateTime.UtcNow.AddHours(1))
        {
            problems.Add("StartTime is in the future");
        }
        else if (value.StartTime < DateTime.UtcNow.AddYears(-1))
        {
            problems.Add("StartTime is more than one year in the past");
        }

        // Validate Metadata - should not be null
        if (value.Metadata is null)
        {
            problems.Add("Metadata dictionary is null");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if a tracing context is valid
    /// </summary>
    /// <param name="value">The tracing context to check</param>
    /// <returns>True if context is valid; false otherwise</returns>
    public static bool IsValid(this TracingContext value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures a tracing context is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The tracing context to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    /// <exception cref="ArgumentException">Thrown if context is invalid, listing all problems</exception>
    public static void EnsureValid(this TracingContext value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "TracingContext is invalid:{0}- {1}",
                    Environment.NewLine,
                    string.Join($"{Environment.NewLine}- ", problems)));
        }
    }

    /// <summary>
    /// Checks if a string contains only hexadecimal digits
    /// </summary>
    private static bool IsHexDigit(this char c)
    {
        return (c >= '0' && c <= '9') ||
               (c >= 'a' && c <= 'f') ||
               (c >= 'A' && c <= 'F');
    }

    /// <summary>
    /// Checks if a string is a valid GUID string format
    /// </summary>
    private static bool IsValidGuidString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        try
        {
            _ = Guid.Parse(input);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
