#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TenantIsolation.Models;

namespace TenantIsolation.Services;

/// <summary>
/// Defines a contract for a dynamic tenant store that can provide tenant information
/// and notify about changes without requiring application restarts.
/// </summary>
public interface IDynamicTenantStore
{
    /// <summary>
    /// Event fired when a new tenant is registered or an existing tenant's status changes to active.
    /// </summary>
    event EventHandler<TenantEventArgs>? OnTenantRegistered;

    /// <summary>
    /// Event fired when a tenant is removed or its status changes to inactive/deleted.
    /// </summary>
    event EventHandler<TenantEventArgs>? OnTenantRemoved;

    /// <summary>
    /// Retrieves all active tenants from the store.
    /// </summary>
    /// <returns>A collection of active tenants.</returns>
    Task<IEnumerable<Tenant>> GetAllActiveTenantsAsync();

    /// <summary>
    /// Retrieves a specific tenant by its identifier.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <returns>The tenant if found, otherwise null.</returns>
    Task<Tenant?> GetTenantByIdAsync(Guid tenantId);
}

/// <summary>
/// Event arguments for tenant registration/removal events.
/// </summary>
public class TenantEventArgs : EventArgs
{
    public required Tenant Tenant { get; set; }
}
