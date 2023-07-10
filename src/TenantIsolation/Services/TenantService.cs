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

namespace TenantIsolation.Services;

/// <summary>
/// Core service for tenant management operations
/// </summary>
public class TenantService
{
    private readonly TenantRepository _tenantRepository;
    private readonly TenantDbContext _context;
    private readonly ILogger<TenantService> _logger;

    public TenantService(TenantRepository tenantRepository, TenantDbContext context, ILogger<TenantService> logger)
    {
        _tenantRepository = tenantRepository;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create new tenant with initialization
    /// </summary>
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

        if (!await _tenantRepository.IsSlugUniqueAsync(slug))
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
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
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

        var tenant = await _tenantRepository.GetBySlugAsync(slug);
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
        return await _tenantRepository.GetActiveTenantAsync();
    }

    /// <summary>
    /// Get tenants by status
    /// </summary>
    public async Task<List<Tenant>> GetTenantsByStatusAsync(TenantStatus status)
    {
        return await _tenantRepository.GetByStatusAsync(status);
    }

    /// <summary>
    /// Get upcoming subscription expirations
    /// </summary>
    public async Task<List<Tenant>> GetExpiringSubscriptionsAsync(int daysUntilExpiry = 30)
    {
        return await _tenantRepository.GetExpiringSubscriptionsAsync(daysUntilExpiry);
    }

    /// <summary>
    /// Set current tenant in context
    /// </summary>
    public void SetCurrentTenant(Guid tenantId)
    {
        _context.SetCurrentTenant(tenantId);
    }

    /// <summary>
    /// Clear current tenant context
    /// </summary>
    public void ClearCurrentTenant()
    {
        _context.ClearCurrentTenant();
    }

    /// <summary>
    /// Search tenants
    /// </summary>
    public async Task<List<Tenant>> SearchTenantsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Tenant>();

        return await _tenantRepository.SearchAsync(query);
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
    public async Task<object> GetTenantStatisticsAsync()
    {
        var statusCounts = await _tenantRepository.GetStatusCountsAsync();
        var totalTenants = statusCounts.Values.Sum();
        var activeTenants = statusCounts.TryGetValue(TenantStatus.Active, out var count) ? count : 0;

        return new
        {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            StatusDistribution = statusCounts,
            ExpiringSubscriptions = await _tenantRepository.GetExpiringSubscriptionsAsync(30)
        };
    }
}
