#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using TenantIsolation.Models;

namespace TenantIsolation.Services;

/// <summary>
/// Validates <see cref="DataIsolationPolicy"/> instances, both in isolation (field-level
/// shape checks) and against the rest of the system (connection strings, sibling policies),
/// returning structured <see cref="PolicyValidationResult"/> outcomes instead of booleans.
/// </summary>
public interface IDataIsolationPolicyValidator
{
    /// <summary>
    /// Validates the intrinsic shape of a policy (required fields, ranges, field-list
    /// consistency) without touching the database.
    /// </summary>
    /// <param name="policy">The policy to validate.</param>
    /// <returns>The structured validation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    PolicyValidationResult ValidateFields(DataIsolationPolicy policy);

    /// <summary>
    /// Validates a policy's field-level shape plus cross-cutting concerns that require
    /// database access: whether the owning tenant has an active connection string, and
    /// whether the policy's isolation mode conflicts with sibling policies for the same
    /// tenant/entity.
    /// </summary>
    /// <param name="policy">The policy to validate.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The structured validation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    Task<PolicyValidationResult> ValidateAsync(DataIsolationPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates every data isolation policy currently stored in the database and returns
    /// an aggregated report. Intended for use as a startup guardrail and for re-validation
    /// triggered by <see cref="TenantIsolation.Events.DataIsolationPolicyChangedEvent"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The aggregated validation report across all policies.</returns>
    Task<PolicyValidationReport> ValidateAllAsync(CancellationToken cancellationToken = default);
}
