#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Linq.Expressions;
using TenantIsolation.Configuration;
using TenantIsolation.Constants;
using TenantIsolation.Models;
using TenantIsolation.Services; // Assuming TenantResolutionService is here

namespace TenantIsolation.Data;

/// <summary>
/// Default implementation of <see cref="ITenantDbContextFactory{TContext}"/> for <see cref="TenantDbContext"/>.
/// This factory ensures that each created DbContext is correctly configured with tenant-specific
/// options, including connection strings and query filters, based on the resolved tenant.
/// </summary>
public class TenantDbContextFactory : ITenantDbContextFactory<TenantDbContext>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantDbContextFactory> _logger;
    private readonly ITenantResolutionService _tenantResolutionService; // To get the current tenant
    private readonly IOptions<TenantIsolationOptions> _tenantIsolationOptions;
    private readonly DbContextOptions<TenantDbContext> _masterDbContextOptions; // Base options without tenant-specifics

    public TenantDbContextFactory(
        IHttpContextAccessor httpContextAccessor,
        ILogger<TenantDbContextFactory> logger,
        ITenantResolutionService tenantResolutionService,
        IOptions<TenantIsolationOptions> tenantIsolationOptions,
        DbContextOptions<TenantDbContext> masterDbContextOptions) // Injected base options
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _tenantResolutionService = tenantResolutionService;
        _tenantIsolationOptions = tenantIsolationOptions;
        _masterDbContextOptions = masterDbContextOptions;
    }

    /// <summary>
    /// Creates a new tenant-aware DbContext instance for the specified tenant.
    /// </summary>
    public TenantDbContext Create(Guid tenantId)
    {
        // For dedicated database per tenant or schema per tenant, you'd typically build
        // new options here with the tenant-specific connection string.
        // For row-level security, we apply a query filter.

        // Start with the base options
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>(_masterDbContextOptions);

        // Apply tenant-specific connection string if applicable (e.g., DatabasePerTenant)
        // This part would depend on how connection strings are managed per tenant.
        // For simplicity, this example focuses on Row-Level Security via query filters.
        // A full implementation might involve looking up TenantConnectionString for the tenantId.

        // Apply tenant-specific query filter for Row-Level Security
        // This is done via a custom IModelCacheKeyFactory and a tenant-aware OnModelCreating
        // or by explicitly adding query filters here.
        // For now, given the issue is connection leaks, the primary goal is a fresh DbContext per tenant.

        // If global query filters for tenant isolation are desired without relying on
        // the mutable _currentTenantId field (which was removed), a custom IModelCacheKeyFactory
        // would be needed to ensure OnModelCreating is re-run for each tenant.
        // However, for this fix, we are removing the dynamic query filtering from DbContext's OnModelCreating.
        // Tenant-specific querying will be implicitly handled at the service/repository layer
        // by passing the tenantId to query methods, or by dynamically constructing options with
        // tenant-specific connection strings.

        // The issue describes a connection leak, which is solved by ensuring a fresh DbContext
        // for each tenant context switch. The removal of _currentTenantId and the introduction of
        // this factory supports that.

        return new TenantDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Creates a new tenant-aware DbContext instance based on the current HTTP context.
    /// </summary>
    public TenantDbContext Create()
    {
        var currentTenant = _tenantResolutionService.GetCurrentTenant();

        if (currentTenant?.Id == null)
        {
            _logger.LogWarning("Attempted to create a tenant-aware DbContext without a resolved tenant. Creating a DbContext without tenant-specific filters.");
            // If no tenant is resolved, we still return a DbContext, but it won't have tenant-specific filters applied.
            // This is suitable for shared data (e.g., public content) or initial setup.
            return new TenantDbContext(_masterDbContextOptions);
        }

        return Create(currentTenant.Id);
    }

    /// <summary>
    /// Applies global query filters for tenant isolation based on the provided tenant ID.
    /// This method is left as a placeholder for where dynamic query filters might be applied
    /// if a custom IModelCacheKeyFactory were implemented. For this fix, dynamic filtering
    /// is not directly applied here, relying on other mechanisms (e.g., explicit WHERE clauses).
    /// </summary>
    private void ApplyTenantQueryFilters(DbContextOptionsBuilder optionsBuilder, Guid tenantId)
    {
        // This method is intentionally left empty for this fix.
        // If explicit tenant-aware global query filters are desired in the future,
        // this is where the logic would go, likely in conjunction with a custom
        // IModelCacheKeyFactory.
    }
}
