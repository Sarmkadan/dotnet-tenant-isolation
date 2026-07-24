#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using TenantIsolation.Services;

namespace TenantIsolation.Models;

/// <summary>
/// Thin, database-independent compatibility shim over <see cref="DataIsolationPolicyValidator"/>'s
/// field-level checks. Kept for callers that validate a policy's intrinsic shape without a
/// dependency-injected <see cref="IDataIsolationPolicyValidator"/> in scope (e.g. constructors,
/// unit tests, model binding). New code that has access to DI should prefer injecting
/// <see cref="IDataIsolationPolicyValidator"/> and calling <c>ValidateAsync</c>, which additionally
/// checks connection-string presence and cross-policy isolation-mode conflicts.
/// </summary>
public static class DataIsolationPolicyValidation
{
    /// <summary>
    /// Validates a DataIsolationPolicy instance and returns a list of human-readable problems
    /// </summary>
    /// <param name="value">The policy to validate</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this DataIsolationPolicy value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.ValidateStructured().Errors.Select(e => e.Message).ToList().AsReadOnly();
    }

    /// <summary>
    /// Validates a DataIsolationPolicy instance's intrinsic field-level shape and returns a
    /// structured result carrying <see cref="PolicyErrorCode"/> values instead of bare strings
    /// </summary>
    /// <param name="value">The policy to validate</param>
    /// <returns>The structured field-level validation result</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static PolicyValidationResult ValidateStructured(this DataIsolationPolicy value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return DataIsolationPolicyValidator.ValidateFieldsStatic(value);
    }

    /// <summary>
    /// Determines whether a DataIsolationPolicy instance is valid
    /// </summary>
    /// <param name="value">The policy to check</param>
    /// <returns>True if the policy is valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static bool IsValid(this DataIsolationPolicy value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.ValidateStructured().IsValid;
    }

    /// <summary>
    /// Ensures that a DataIsolationPolicy instance is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The policy to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if the policy is invalid, containing a list of problems</exception>
    public static void EnsureValid(this DataIsolationPolicy value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var result = value.ValidateStructured();
        if (result.IsValid)
        {
            return;
        }

        throw new ArgumentException("DataIsolationPolicy validation failed:" + Environment.NewLine + "- " + result.ToDisplayString());
    }
}
