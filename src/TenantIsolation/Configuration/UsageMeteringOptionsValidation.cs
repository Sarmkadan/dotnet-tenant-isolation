#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace TenantIsolation.Configuration;

/// <summary>
/// Provides validation helpers for <see cref="UsageMeteringOptions"/> configuration.
/// </summary>
public static class UsageMeteringOptionsValidation
{
    /// <summary>
    /// Validates the provided <see cref="UsageMeteringOptions"/> instance.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <returns>An immutable list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this UsageMeteringOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate WarningThresholdPercent
        if (value.WarningThresholdPercent is < 1 or > 100)
        {
            problems.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "WarningThresholdPercent must be between 1 and 100 (actual: {0}).",
                    value.WarningThresholdPercent));
        }

        // Validate MaxMetricsPerTenant
        if (value.MaxMetricsPerTenant < 0)
        {
            problems.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "MaxMetricsPerTenant cannot be negative (actual: {0}).",
                    value.MaxMetricsPerTenant));
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the provided <see cref="UsageMeteringOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The options to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this UsageMeteringOptions? value) => value?.Validate().Count == 0;

    /// <summary>
    /// Ensures that the provided <see cref="UsageMeteringOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails, containing a human-readable list of problems.</exception>
    public static void EnsureValid(this UsageMeteringOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
            return;

        throw new ArgumentException(
            $"UsageMeteringOptions validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}",
            nameof(value));
    }
}
