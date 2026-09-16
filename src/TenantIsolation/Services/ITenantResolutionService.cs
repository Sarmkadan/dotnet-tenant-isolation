#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using TenantIsolation.Constants;
using TenantIsolation.Models;

namespace TenantIsolation.Services;

/// <summary>
/// Represents the result of tenant resolution including the resolved tenant
/// and the strategy that was used to resolve it.
/// </summary>
public class TenantResolutionResult
{
    /// <summary>
    /// The resolved tenant. Will be null if resolution failed.
    /// </summary>
    public Tenant? Tenant { get; }

    /// <summary>
    /// The strategy that successfully resolved the tenant.
    /// Will be null if resolution failed.
    /// </summary>
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
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tenant"/> is <see langword="null"/>.
    /// </exception>
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
    /// <param name="result">The tenant resolution result to evaluate.</param>
    /// <returns><see langword="true"/> if tenant resolution succeeded; otherwise, <see langword="false"/>.</returns>
    public static implicit operator bool(TenantResolutionResult result) => result.Success;

    /// <summary>
    /// Implicit conversion from Tenant for backward compatibility.
    /// </summary>
    /// <param name="result">The tenant resolution result from which to retrieve the tenant.</param>
    /// <returns>The resolved tenant, or <see langword="null"/> if resolution failed.</returns>
    public static implicit operator Tenant?(TenantResolutionResult result) => result.Tenant;
}

/// <summary>
/// Resolves the current tenant from the ambient request context.
/// Abstracted as an interface so hosts can plug in a custom resolution
/// scheme and consumers (middleware, DbContext factory, controllers)
/// can be unit tested without spinning up the full strategy chain.
/// </summary>
public interface ITenantResolutionService
{
    /// <summary>
    /// Resolve the tenant for the current request, trying all configured strategies.
    /// </summary>
    /// <returns>The resolved tenant</returns>
    /// <exception cref="TenantNotResolvedException">Thrown if tenant cannot be resolved and ThrowOnResolutionFailure is true</exception>
    Task<Tenant> ResolveTenantAsync();

    /// <summary>
    /// Resolve the tenant for the current request with strategy information.
    /// </summary>
    /// <returns>Resolution result containing tenant and strategy used</returns>
    Task<TenantResolutionResult> ResolveTenantWithStrategyAsync();

    /// <summary>
    /// Get the tenant already resolved for the current request, or null if none.
    /// </summary>
    /// <returns>The resolved tenant, or <see langword="null"/> if no tenant has been resolved.</returns>
    Tenant? GetCurrentTenant();

    /// <summary>
    /// Get the id of the tenant resolved for the current request, or null.
    /// </summary>
    /// <returns>The resolved tenant identifier, or <see langword="null"/> if no tenant has been resolved.</returns>
    Guid? GetCurrentTenantId();

    /// <summary>
    /// Whether a tenant has been resolved for the current request.
    /// </summary>
    /// <returns><see langword="true"/> if a tenant has been resolved; otherwise, <see langword="false"/>.</returns>
    bool HasTenant();

    /// <summary>
    /// Get the strategy that was used to resolve the current tenant.
    /// Returns null if no tenant has been resolved yet.
    /// </summary>
    /// <returns>The resolution strategy, or <see langword="null"/> if no tenant has been resolved.</returns>
    TenantResolutionStrategy? GetResolvedStrategy();
}
