#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using TenantIsolation.Models;

namespace TenantIsolation.Services;

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
    Task<Tenant> ResolveTenantAsync();

    /// <summary>
    /// Get the tenant already resolved for the current request, or null if none.
    /// </summary>
    Tenant? GetCurrentTenant();

    /// <summary>
    /// Get the id of the tenant resolved for the current request, or null.
    /// </summary>
    Guid? GetCurrentTenantId();

    /// <summary>
    /// Whether a tenant has been resolved for the current request.
    /// </summary>
    bool HasTenant();
}
