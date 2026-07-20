#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics.CodeAnalysis;
using TenantIsolation.Constants;

namespace TenantIsolation.Models;

/// <summary>
/// Represents the result of tenant resolution including the resolved tenant
/// and the strategy that was used to resolve it.
/// </summary>
public class TenantResolutionResult
{
    /// <summary>
    /// The resolved tenant. Will be null if resolution failed.
    /// </summary>
    [NotNullIfNotNull(nameof(Tenant))]
    public Tenant? Tenant { get; }

    /// <summary>
    /// The strategy that successfully resolved the tenant.
    /// Will be null if resolution failed.
    /// </summary>
    [NotNullIfNotNull(nameof(Tenant))]
    public TenantResolutionStrategy? ResolvedStrategy { get; }

    /// <summary>
    /// Whether tenant resolution was successful.
    /// </summary>
    public bool Success => Tenant != null;

    /// <summary>
    /// Creates a successful resolution result.
    /// </summary>
    /// <param name="tenant">The resolved tenant</param>
    /// <param name="strategy">The strategy used for resolution</param>
    public TenantResolutionResult(Tenant tenant, TenantResolutionStrategy strategy)
    {
        Tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        ResolvedStrategy = strategy;
    }

    /// <summary>
    /// Creates a failed resolution result.
    /// </summary>
    public TenantResolutionResult()
    {
        Tenant = null;
        ResolvedStrategy = null;
    }

    /// <summary>
    /// Implicit conversion to bool for easy success checking.
    /// </summary>
    public static implicit operator bool(TenantResolutionResult result) => result.Success;

    /// <summary>
    /// Implicit conversion from Tenant for backward compatibility.
    /// </summary>
    public static implicit operator Tenant?(TenantResolutionResult result) => result.Tenant;
}
