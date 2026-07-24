#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace TenantIsolation.Models;

/// <summary>
/// Enumerates the specific, machine-readable reasons a <see cref="DataIsolationPolicy"/>
/// can fail validation. Consumers (dashboards, alerting, startup guardrails) should
/// branch on this value rather than parsing free-text error messages.
/// </summary>
public enum PolicyErrorCode
{
    /// <summary>Policy identifier is an empty GUID.</summary>
    MissingId,

    /// <summary>Tenant identifier is an empty GUID.</summary>
    MissingTenantId,

    /// <summary>Entity type is missing or blank.</summary>
    MissingEntityType,

    /// <summary>Entity type exceeds the maximum allowed length.</summary>
    EntityTypeTooLong,

    /// <summary>Policy type is not a defined <see cref="DataIsolationPolicyType"/> value.</summary>
    InvalidPolicyType,

    /// <summary>Priority is outside the allowed 0-1000 range.</summary>
    PriorityOutOfRange,

    /// <summary>CreatedAt is unset or in the future.</summary>
    InvalidCreatedAt,

    /// <summary>UpdatedAt is unset or in the future.</summary>
    InvalidUpdatedAt,

    /// <summary>Custom policy type is missing its required filter rule.</summary>
    MissingFilterRule,

    /// <summary>Allowed or denied field list contains an empty/whitespace entry.</summary>
    InvalidFieldList,

    /// <summary>A field appears in both the allowed and denied field lists.</summary>
    ConflictingFieldRules,

    /// <summary>Cross-tenant access list contains a value that is not a valid GUID.</summary>
    InvalidCrossTenantAccessFormat,

    /// <summary>Description exceeds the maximum allowed length.</summary>
    DescriptionTooLong,

    /// <summary>Filter rule exceeds the maximum allowed length.</summary>
    FilterRuleTooLong,

    /// <summary>
    /// No active connection string is configured for the tenant that owns this policy,
    /// so the isolation boundary cannot actually be enforced at the data-access layer.
    /// </summary>
    MissingConnectionString,

    /// <summary>
    /// The policy's isolation mode contradicts itself or another active policy for the
    /// same tenant/entity - e.g. a Strict policy that also whitelists cross-tenant access,
    /// or two active policies for the same tenant/entity declaring different policy types.
    /// </summary>
    ConflictingIsolationMode
}
