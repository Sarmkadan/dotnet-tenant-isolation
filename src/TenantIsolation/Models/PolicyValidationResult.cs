#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace TenantIsolation.Models;

/// <summary>
/// A single, structured validation failure for a <see cref="DataIsolationPolicy"/>.
/// </summary>
/// <param name="Code">The machine-readable failure reason.</param>
/// <param name="Message">A human-readable description of the failure.</param>
/// <param name="Field">The policy field the failure relates to, if applicable.</param>
public sealed record PolicyValidationError(PolicyErrorCode Code, string Message, string? Field = null)
{
    /// <summary>
    /// Returns a human-readable representation combining the error code and message.
    /// </summary>
    /// <returns>A string in the form "Code: Message".</returns>
    public override string ToString() => $"{Code}: {Message}";
}

/// <summary>
/// The outcome of validating a single <see cref="DataIsolationPolicy"/>, carrying
/// structured error codes instead of a bare boolean or free-text list.
/// </summary>
public sealed class PolicyValidationResult
{
    private static readonly PolicyValidationResult SuccessInstance = new(Array.Empty<PolicyValidationError>());

    /// <summary>
    /// Structured validation errors. Empty when the policy is valid.
    /// </summary>
    public IReadOnlyList<PolicyValidationError> Errors { get; }

    /// <summary>
    /// True when no validation errors were found.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyValidationResult"/> class.
    /// </summary>
    /// <param name="errors">The validation errors found, if any.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="errors"/> is null.</exception>
    public PolicyValidationResult(IReadOnlyList<PolicyValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors;
    }

    /// <summary>
    /// Gets a shared, cached result representing a valid policy with no errors.
    /// </summary>
    /// <returns>A <see cref="PolicyValidationResult"/> with an empty error list.</returns>
    public static PolicyValidationResult Success() => SuccessInstance;

    /// <summary>
    /// Builds a failed result from the given errors.
    /// </summary>
    /// <param name="errors">The validation errors to include.</param>
    /// <returns>A <see cref="PolicyValidationResult"/> wrapping the provided errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="errors"/> is null.</exception>
    public static PolicyValidationResult Failure(IEnumerable<PolicyValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var list = errors.ToList();
        return list.Count == 0 ? Success() : new PolicyValidationResult(list);
    }

    /// <summary>
    /// Renders all errors as a single, newline-delimited, human-readable message.
    /// </summary>
    /// <returns>A formatted summary of the validation errors, or an empty string when valid.</returns>
    public string ToDisplayString() =>
        IsValid ? string.Empty : string.Join(Environment.NewLine + "- ", Errors.Select(e => e.ToString()));
}

/// <summary>
/// One entry of an aggregated startup validation report, identifying which policy
/// produced which result.
/// </summary>
/// <param name="PolicyId">The identifier of the policy that was validated.</param>
/// <param name="TenantId">The tenant that owns the policy.</param>
/// <param name="EntityType">The entity type the policy applies to.</param>
/// <param name="Result">The structured validation outcome for this policy.</param>
public sealed record PolicyValidationReportEntry(
    Guid PolicyId,
    Guid TenantId,
    string EntityType,
    PolicyValidationResult Result);

/// <summary>
/// Aggregated result of validating every configured <see cref="DataIsolationPolicy"/>,
/// produced by <see cref="IDataIsolationPolicyValidator.ValidateAllAsync"/> and used by the
/// startup guardrail to fail fast on misconfiguration.
/// </summary>
public sealed class PolicyValidationReport
{
    /// <summary>
    /// UTC timestamp at which validation ran.
    /// </summary>
    public DateTime ValidatedAt { get; }

    /// <summary>
    /// Every policy that was evaluated, along with its individual result.
    /// </summary>
    public IReadOnlyList<PolicyValidationReportEntry> Entries { get; }

    /// <summary>
    /// True when every policy in <see cref="Entries"/> is valid.
    /// </summary>
    public bool IsValid => Entries.All(e => e.Result.IsValid);

    /// <summary>
    /// The subset of <see cref="Entries"/> that failed validation.
    /// </summary>
    public IReadOnlyList<PolicyValidationReportEntry> FailedEntries =>
        Entries.Where(e => !e.Result.IsValid).ToList();

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyValidationReport"/> class.
    /// </summary>
    /// <param name="entries">The per-policy validation results.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entries"/> is null.</exception>
    public PolicyValidationReport(IReadOnlyList<PolicyValidationReportEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = entries;
        ValidatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Renders a full human-readable report suitable for logging or startup failure messages.
    /// </summary>
    /// <returns>A multi-line summary of every failed policy and its errors.</returns>
    public string ToDisplayString()
    {
        if (IsValid)
        {
            return $"All {Entries.Count} data isolation policies are valid (checked at {ValidatedAt:O}).";
        }

        var failed = FailedEntries;
        var lines = failed.Select(e =>
            $"Policy {e.PolicyId} (tenant {e.TenantId}, entity '{e.EntityType}'):" +
            Environment.NewLine + "  - " + e.Result.ToDisplayString());

        return $"{failed.Count} of {Entries.Count} data isolation policies failed validation (checked at {ValidatedAt:O}):"
            + Environment.NewLine + string.Join(Environment.NewLine, lines);
    }
}
