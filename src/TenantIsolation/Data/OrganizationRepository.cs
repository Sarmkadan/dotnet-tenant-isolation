#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using TenantIsolation.Models;

namespace TenantIsolation.Data;

/// <summary>
/// Repository for organization management within tenants
/// </summary>
public class OrganizationRepository : Repository<Organization>
{
    public OrganizationRepository(ITenantDbContextFactory<TenantDbContext> contextFactory)
        : base(contextFactory) { }

    /// <summary>
    /// Get organization by slug within tenant
    /// </summary>
    public async Task<Organization?> GetBySlugAsync(Guid tenantId, string slug)
    {
        return await DbSet.FirstOrDefaultAsync(o => o.TenantId == tenantId &&
                                                     o.Slug == slug &&
                                                     !o.IsDeleted);
    }

    /// <summary>
    /// Get all active organizations in tenant
    /// </summary>
    public async Task<List<Organization>> GetActiveOrganizationsAsync(Guid tenantId)
    {
        return await DbSet
            .Where(o => o.TenantId == tenantId &&
                       o.IsActive &&
                       !o.IsDeleted)
            .OrderBy(o => o.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Get organization with users
    /// </summary>
    public async Task<Organization?> GetWithUsersAsync(Guid id)
    {
        return await DbSet
            .Include(o => o.Users.Where(u => !u.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
    }

    /// <summary>
    /// Get organizations by industry
    /// </summary>
    public async Task<List<Organization>> GetByIndustryAsync(Guid tenantId, string industry)
    {
        return await DbSet
            .Where(o => o.TenantId == tenantId &&
                       o.Industry == industry &&
                       o.IsActive &&
                       !o.IsDeleted)
            .ToListAsync();
    }

    /// <summary>
    /// Get organizations by country
    /// </summary>
    public async Task<List<Organization>> GetByCountryAsync(Guid tenantId, string countryCode)
    {
        return await DbSet
            .Where(o => o.TenantId == tenantId &&
                       o.CountryCode == countryCode &&
                       o.IsActive &&
                       !o.IsDeleted)
            .ToListAsync();
    }

    /// <summary>
    /// Search organizations by name or email
    /// </summary>
    public async Task<List<Organization>> SearchAsync(Guid tenantId, string query)
    {
        var searchTerm = query.ToLower();
        return await DbSet
            .Where(o => o.TenantId == tenantId &&
                       (o.Name.ToLower().Contains(searchTerm) ||
                        o.ContactEmail.ToLower().Contains(searchTerm) ||
                        o.Slug.ToLower().Contains(searchTerm)) &&
                       !o.IsDeleted)
            .OrderBy(o => o.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Get organization count in tenant
    /// </summary>
    public async Task<int> GetOrganizationCountAsync(Guid tenantId)
    {
        return await DbSet.CountAsync(o => o.TenantId == tenantId &&
                                           o.IsActive &&
                                           !o.IsDeleted);
    }

    /// <summary>
    /// Check if slug is unique in tenant
    /// </summary>
    public async Task<bool> IsSlugUniqueAsync(Guid tenantId, string slug, Guid? excludeId = null)
    {
        var query = DbSet.Where(o => o.TenantId == tenantId &&
                                     o.Slug == slug &&
                                     !o.IsDeleted);
        if (excludeId.HasValue)
            query = query.Where(o => o.Id != excludeId.Value);

        return !await query.AnyAsync();
    }

    /// <summary>
    /// Get organizations with user count
    /// </summary>
    public async Task<List<dynamic>> GetOrganizationsWithUserCountAsync(Guid tenantId)
    {
        var organizations = await DbSet
            .Where(o => o.TenantId == tenantId && !o.IsDeleted)
            .Select(o => new
            {
                Organization = o,
                UserCount = o.Users.Count(u => !u.IsDeleted)
            })
            .ToListAsync();

        return organizations
            .Select(x => (dynamic)new { x.Organization, x.UserCount })
            .ToList();
    }

    /// <summary>
    /// Get organizations by registration number
    /// </summary>
    public async Task<Organization?> GetByRegistrationNumberAsync(Guid tenantId, string registrationNumber)
    {
        return await DbSet
            .FirstOrDefaultAsync(o => o.TenantId == tenantId &&
                                      o.RegistrationNumber == registrationNumber &&
                                      !o.IsDeleted);
    }

    /// <summary>
    /// Deactivate organization
    /// </summary>
    public async Task<bool> DeactivateAsync(Guid id)
    {
        var org = await GetByIdAsync(id);
        if (org == null)
            return false;

        org.IsActive = false;
        org.UpdatedAt = DateTime.UtcNow;
        await UpdateAsync(org);
        return true;
    }

    /// <summary>
    /// Get organization statistics
    /// </summary>
    public async Task<object> GetStatisticsAsync(Guid tenantId)
    {
        return await DbSet
            .Where(o => o.TenantId == tenantId && !o.IsDeleted)
            .Select(o => new
            {
                TotalOrganizations = DbSet.Count(x => x.TenantId == tenantId && !x.IsDeleted),
                ActiveOrganizations = DbSet.Count(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted),
                ByIndustry = DbSet
                    .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                    .GroupBy(x => x.Industry)
                    .Select(g => new { Industry = g.Key, Count = g.Count() })
                    .ToList(),
                ByCountry = DbSet
                    .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                    .GroupBy(x => x.CountryCode)
                    .Select(g => new { Country = g.Key, Count = g.Count() })
                    .ToList()
            })
            .FirstAsync();
    }

    /// <summary>
    /// Bulk activate organizations
    /// </summary>
    public async Task<int> BulkActivateAsync(Guid tenantId, List<Guid> organizationIds)
    {
        return await DbSet
            .Where(o => o.TenantId == tenantId && organizationIds.Contains(o.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsActive, true));
    }

    /// <summary>
    /// Get recent organizations
    /// </summary>
    public async Task<List<Organization>> GetRecentAsync(Guid tenantId, int count = 10)
    {
        return await DbSet
            .Where(o => o.TenantId == tenantId && !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}
