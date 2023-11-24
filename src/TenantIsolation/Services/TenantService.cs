#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using TenantIsolation.Constants;
using TenantIsolation.Data;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;
using System.Linq;
using TenantIsolation.Controllers;

namespace TenantIsolation.Services;

/// <summary>
/// Provides services for managing tenant lifecycles, including creation, activation, suspension, and deletion.
/// </summary>
public class TenantService
{
    private readonly TenantRepository _tenantRepository; // For write operations and direct DB access if needed
    private readonly IDynamicTenantStore _dynamicTenantStore; // For read operations with caching
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        TenantRepository tenantRepository,
        IDynamicTenantStore dynamicTenantStore,
        ILogger<TenantService> logger)
    {
        _tenantRepository = tenantRepository;
        _dynamicTenantStore = dynamicTenantStore;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new tenant and initializes it in the system.
    /// </summary>
    /// <param name="name">The display name of the tenant.</param>
    /// <param name="slug">The unique URL-friendly slug for the tenant.</param>
    /// <param name="adminEmail">The primary email address for the tenant administrator.</param>
    /// <param name="strategy">The data isolation strategy to use for this tenant (defaults to <see cref="TenantIsolationStrategy.DatabasePerTenant"/>).</param>
    /// <returns>A <see cref="Task{Tenant}"/> representing the asynchronous operation, containing the created <see cref="Tenant"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when name, slug, or adminEmail is invalid.</exception>
    /// <exception cref="TenantIsolationException">Thrown when the slug is already in use or database operations fail.</exception>
    public async Task<Tenant> CreateTenantAsync(string name, string slug, string adminEmail,
        TenantIsolationStrategy strategy = TenantIsolationStrategy.DatabasePerTenant)

    {
        // Fix: Changed ArgumentNullException to ArgumentException for string validation.
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));

        // Fix: Changed ArgumentNullException to ArgumentException for string validation.
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Tenant slug is required.", nameof(slug));

        if (string.IsNullOrWhiteSpace(adminEmail))
            throw new ArgumentNullException(nameof(adminEmail), "Admin email is required.");

        if (!await _tenantRepository.IsSlugUniqueAsync(slug)) // Use repository for uniqueness check as it's a write concern
            throw new TenantIsolationException($"Tenant slug '{slug}' is already in use");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLower(),
            AdminEmail = adminEmail,
            Status = TenantStatus.Provisioning,
            IsolationStrategy = strategy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _tenantRepository.AddAsync(tenant);
            _logger.LogInformation("Created tenant {TenantId} with slug {Slug}", tenant.Id, slug);
            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tenant {Slug}", slug);
            throw new TenantIsolationException($"Failed to create tenant: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get tenant by identifier
    /// </summary>
    public async Task<Tenant> GetTenantAsync(Guid tenantId)
    {
        var tenant = await _dynamicTenantStore.GetTenantByIdAsync(tenantId);
        if (tenant == null)
            throw new TenantNotResolvedException(tenantId.ToString());

        return tenant;
    }

    /// <summary>
    /// Get tenant by slug
    /// </summary>
    public async Task<Tenant> GetTenantBySlugAsync(string slug)
    {
        // Fix: Changed TenantNotResolvedException to ArgumentException for string validation.
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Tenant slug cannot be null or whitespace.", nameof(slug));

        var tenants = await _dynamicTenantStore.GetAllActiveTenantsAsync();
        var tenant = tenants.FirstOrDefault(t => t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        if (tenant == null)
            throw new TenantNotResolvedException("slug", slug);

        return tenant;
    }

    /// <summary>
    /// Activate tenant
    /// </summary>
    public async Task<bool> ActivateTenantAsync(Guid tenantId)
    {
        var tenant = await GetTenantAsync(tenantId);

        if (!tenant.CanActivate())
            throw new TenantNotActiveException(tenantId, "Tenant cannot be activated in its current state");

        return await _tenantRepository.ActivateTenantAsync(tenantId);
    }

    /// <summary>
    /// Suspend tenant access
    /// </summary>
    public async Task<bool> SuspendTenantAsync(Guid tenantId, string? reason = null)
    {
        var tenant = await GetTenantAsync(tenantId);
        return await _tenantRepository.SuspendTenantAsync(tenantId, reason);
    }

    /// <summary>
    /// Delete tenant (soft delete)
    /// </summary>
    public async Task<bool> DeleteTenantAsync(Guid tenantId)
    {
        var tenant = await GetTenantAsync(tenantId);
        tenant.Delete();
        await _tenantRepository.UpdateAsync(tenant);
        _logger.LogInformation("Deleted tenant {TenantId}", tenantId);
        return true;
    }

    /// <summary>
    /// Update tenant information
    /// </summary>
    public async Task<Tenant> UpdateTenantAsync(Guid tenantId, Action<Tenant> updateAction)
    {
        // Fix: Added null check for updateAction parameter.
        if (updateAction == null)
            throw new ArgumentNullException(nameof(updateAction));

        var tenant = await GetTenantAsync(tenantId);
        updateAction(tenant);
        tenant.UpdatedAt = DateTime.UtcNow;
        await _tenantRepository.UpdateAsync(tenant);
        return tenant;
    }

    /// <summary>
    /// Check tenant subscription validity
    /// </summary>
    public async Task<bool> IsSubscriptionValidAsync(Guid tenantId)
    {
        var tenant = await GetTenantAsync(tenantId);
        return tenant.IsSubscriptionValid();
    }

    /// <summary>
    /// Get all active tenants
    /// </summary>
    public async Task<List<Tenant>> GetActiveTenantsAsync()
    {
        return (await _dynamicTenantStore.GetAllActiveTenantsAsync()).ToList();
    }

    /// <summary>
    /// Get tenants by status
    /// </summary>
    public async Task<List<Tenant>> GetTenantsByStatusAsync(TenantStatus status)
    {
        // This will query the cached tenants, might need to hit repo if a fresh list is required always
        return (await _dynamicTenantStore.GetAllActiveTenantsAsync()).Where(t => t.Status == status).ToList();
    }

    /// <summary>
    /// Get upcoming subscription expirations
    /// </summary>
    public async Task<List<Tenant>> GetExpiringSubscriptionsAsync(int daysUntilExpiry = 30)
    {
        return await _tenantRepository.GetExpiringSubscriptionsAsync(daysUntilExpiry);
    }

    /// <summary>
    /// Search tenants
    /// </summary>
    public async Task<List<Tenant>> SearchTenantsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Tenant>();

        // This will search in cached tenants
        var searchTerm = query.ToLower();
        return (await _dynamicTenantStore.GetAllActiveTenantsAsync())
            .Where(t => (t.Name != null && t.Name.ToLower().Contains(searchTerm)) ||
                       (t.Slug != null && t.Slug.ToLower().Contains(searchTerm)) ||
                       (t.AdminEmail != null && t.AdminEmail.ToLower().Contains(searchTerm)))
            .ToList();
    }

    /// <summary>
    /// Check if tenant is in trial
    /// </summary>
    public async Task<bool> IsInTrialAsync(Guid tenantId)
    {
        var tenant = await GetTenantAsync(tenantId);
        return tenant.IsInTrial();
    }

    /// <summary>
    /// Get tenant usage statistics
    /// </summary>
    public async Task<TenantStatistics> GetTenantStatisticsAsync()
    {
        // For statistics, it's better to hit the repository directly for fresh data
        var statusCounts = await _tenantRepository.GetStatusCountsAsync();
        var totalTenants = statusCounts.Values.Sum();
        var activeTenants = statusCounts.TryGetValue(TenantStatus.Active, out var count) ? count : 0;
        var suspendedTenants = statusCounts.TryGetValue(TenantStatus.Suspended, out var suspendedCount) ? suspendedCount : 0;
        var deletedTenants = statusCounts.TryGetValue(TenantStatus.Archived, out var deletedCount) ? deletedCount : 0;

        return new TenantStatistics
        {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            SuspendedTenants = suspendedTenants,
            DeletedTenants = deletedTenants
        };
    }
}
