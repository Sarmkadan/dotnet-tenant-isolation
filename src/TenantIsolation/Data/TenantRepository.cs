#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using TenantIsolation.Constants;
using TenantIsolation.Models;

namespace TenantIsolation.Data;

/// <summary>
/// Repository for tenant management operations
/// </summary>
public class TenantRepository : Repository<Tenant>
{
    public TenantRepository(ITenantDbContextFactory<TenantDbContext> contextFactory) : base(contextFactory) { }

    /// <summary>
    /// Get tenant by slug
    /// </summary>
    public async Task<Tenant?> GetBySlugAsync(string slug)
    {
        // Fix: Validate slug parameter to prevent null or whitespace issues in queries.
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be null or whitespace.", nameof(slug));

        return await DbSet.FirstOrDefaultAsync(t => t.Slug == slug && !t.IsDeleted && t.IsActive);
    }

    /// <summary>
    /// Get all active tenants
    /// </summary>
    public async Task<List<Tenant>> GetActiveTenantAsync()
    {
        return await DbSet
            .Where(t => t.Status == TenantStatus.Active && !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Get tenants by status
    /// </summary>
    public async Task<List<Tenant>> GetByStatusAsync(TenantStatus status)
    {
        return await DbSet
            .Where(t => t.Status == status && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get tenants in trial period
    /// </summary>
    public async Task<List<Tenant>> GetTrialTenantsAsync()
    {
        return await DbSet
            .Where(t => t.Status == TenantStatus.Trial && !t.IsDeleted)
            .ToListAsync();
    }

    /// <summary>
    /// Get tenants with expiring subscriptions
    /// </summary>
    public async Task<List<Tenant>> GetExpiringSubscriptionsAsync(int daysUntilExpiry = 30)
    {
        // Fix: Validate daysUntilExpiry to ensure it's not negative.
        if (daysUntilExpiry < 0)
            throw new ArgumentOutOfRangeException(nameof(daysUntilExpiry), "Days until expiry cannot be negative.");

        var expiryDate = DateTime.UtcNow.AddDays(daysUntilExpiry);
        return await DbSet
            .Where(t => t.SubscriptionExpiresAt.HasValue &&
                       t.SubscriptionExpiresAt <= expiryDate &&
                       t.SubscriptionExpiresAt > DateTime.UtcNow &&
                       !t.IsDeleted)
            .OrderBy(t => t.SubscriptionExpiresAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get recently created tenants
    /// </summary>
    public async Task<List<Tenant>> GetRecentlyCreatedAsync(int days = 7)
    {
        var sinceDate = DateTime.UtcNow.AddDays(-days);
        return await DbSet
            .Where(t => t.CreatedAt >= sinceDate && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Search tenants by name or slug
    /// </summary>
    public async Task<List<Tenant>> SearchAsync(string query)
    {
        // Fix: Add null checks for tenant properties before performing string operations to prevent NullReferenceExceptions.
        // Also, guard against null/whitespace query at the repository level for robustness.
        if (string.IsNullOrWhiteSpace(query))
            return new List<Tenant>();

        var searchTerm = query.ToLower();
        return await DbSet
            .Where(t => (t.Name != null && t.Name.ToLower().Contains(searchTerm)) ||
                       (t.Slug != null && t.Slug.ToLower().Contains(searchTerm)) ||
                       (t.AdminEmail != null && t.AdminEmail.ToLower().Contains(searchTerm)))
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Get tenant with all related data
    /// </summary>
    public async Task<Tenant?> GetWithDetailsAsync(Guid id)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
    }

    /// <summary>
    /// Get count of tenants by status
    /// </summary>
    public async Task<Dictionary<TenantStatus, int>> GetStatusCountsAsync()
    {
        return await DbSet
            .Where(t => !t.IsDeleted)
            .GroupBy(t => t.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    /// <summary>
    /// Check if slug is unique
    /// </summary>
    public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null)
    {
        // Fix: Validate slug parameter to prevent null or whitespace issues in queries.
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be null or whitespace.", nameof(slug));

        var query = DbSet.Where(t => t.Slug == slug && !t.IsDeleted);
        if (excludeId.HasValue)
            query = query.Where(t => t.Id != excludeId.Value);

        return !await query.AnyAsync();
    }

    /// <summary>
    /// Get inactive tenants (no login for X days)
    /// </summary>
    public async Task<List<Tenant>> GetInactiveTenantsAsync(int inactiveDays = 90)
    {
        var threshold = DateTime.UtcNow.AddDays(-inactiveDays);
        return await DbSet
            .Where(t => t.UpdatedAt < threshold && !t.IsDeleted)
            .OrderBy(t => t.UpdatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Activate tenant
    /// </summary>
    public async Task<bool> ActivateTenantAsync(Guid tenantId)
    {
        var tenant = await GetByIdAsync(tenantId);
        if (tenant == null || !tenant.CanActivate())
            return false;

        tenant.Status = TenantStatus.Active;
        tenant.UpdatedAt = DateTime.UtcNow;
        await UpdateAsync(tenant);
        return true;
    }

    /// <summary>
    /// Suspend tenant
    /// </summary>
    public async Task<bool> SuspendTenantAsync(Guid tenantId, string? reason = null)
    {
        var tenant = await GetByIdAsync(tenantId);
        if (tenant == null)
            return false;

        tenant.Suspend(reason);
        await UpdateAsync(tenant);
        return true;
    }

    /// <summary>
    /// Get billing summary
    /// </summary>
    public async Task<object> GetBillingSummaryAsync()
    {
        return await DbSet
            .Where(t => !t.IsDeleted)
            .GroupBy(t => t.PlanId)
            .Select(g => new
            {
                PlanId = g.Key,
                Count = g.Count(),
                TotalUsers = g.Sum(t => 1),
                ActiveCount = g.Count(t => t.Status == TenantStatus.Active)
            })
            .ToListAsync();
    }
}
