#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace TenantIsolation.Services;

/// <summary>
/// Provides validation helpers for <see cref="ComponentHealthInfo"/> instances
/// </summary>
public static class ComponentHealthInfoValidation
{
    /// <summary>
    /// Validates a <see cref="ComponentHealthInfo"/> instance and returns a list of human-readable problems
    /// </summary>
    /// <param name="value">The component health info to validate</param>
    /// <returns>An empty list if valid; otherwise, a list of validation error messages</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this ComponentHealthInfo value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Name (must not be null, empty, or whitespace-only)
        if (string.IsNullOrWhiteSpace(value.Name))
        {
            errors.Add("Name cannot be null, empty, or whitespace.");
        }

        // Validate Message (null is invalid, empty is acceptable)
        if (value.Message is null)
        {
            errors.Add("Message cannot be null.");
        }

        // Validate ResponseTimeMs (must be non-negative)
        if (value.ResponseTimeMs < 0)
        {
            errors.Add("ResponseTimeMs cannot be negative.");
        }

        // Validate CheckedAt (must not be default/MinValue)
        if (value.CheckedAt == default)
        {
            errors.Add("CheckedAt cannot be default/MinValue.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="ComponentHealthInfo"/> instance is valid
    /// </summary>
    /// <param name="value">The component health info to validate</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static bool IsValid(this ComponentHealthInfo value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="ComponentHealthInfo"/> instance is valid, throwing an <see cref="ArgumentException"/> if not
    /// </summary>
    /// <param name="value">The component health info to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid, with a list of problems</exception>
    public static void EnsureValid(this ComponentHealthInfo value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();

        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"ComponentHealthInfo is not valid. Problems: {string.Join(" ", errors)}",
                nameof(value));
        }
    }
}