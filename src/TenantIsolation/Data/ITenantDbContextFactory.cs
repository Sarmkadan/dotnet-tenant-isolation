#nullable enable

using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace TenantIsolation.Data;

/// <summary>
/// Factory to create tenant-aware DbContext instances.
/// </summary>
public interface ITenantDbContextFactory<TContext> where TContext : DbContext
{
    /// <summary>
    /// Creates a new tenant-aware DbContext instance for the specified tenant.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant for which to create the DbContext.</param>
    /// <returns>A new instance of TContext configured for the specified tenant.</returns>
    TContext Create(Guid tenantId);

    /// <summary>
    /// Creates a new tenant-aware DbContext instance based on the current HTTP context.
    /// If no tenant is resolved, it will create a DbContext without tenant-specific filters.
    /// </summary>
    /// <returns>A new instance of TContext configured for the current tenant.</returns>
    TContext Create();
}
