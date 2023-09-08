// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using TenantIsolation.Models;

namespace TenantIsolation.Data;

/// <summary>
/// Repository for user management with tenant isolation
/// </summary>
public class UserRepository : Repository<User>
{
    public UserRepository(TenantDbContext context, Guid? tenantId = null)
        : base(context, tenantId) { }

    /// <summary>
    /// Get user by email within tenant
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email, Guid tenantId)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Email == email &&
                                                     u.TenantId == tenantId &&
                                                     !u.IsDeleted);
    }

    /// <summary>
    /// Get active users in organization
    /// </summary>
    public async Task<List<User>> GetActiveUsersInOrganizationAsync(Guid organizationId)
    {
        return await DbSet
            .Where(u => u.OrganizationId == organizationId &&
                       u.IsActive &&
                       !u.IsDeleted)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    /// <summary>
    /// Get users in tenant by role
    /// </summary>
    public async Task<List<User>> GetByRoleAsync(Guid tenantId, string role)
    {
        return await DbSet
            .Where(u => u.TenantId == tenantId &&
                       u.Role == role &&
                       !u.IsDeleted)
            .ToListAsync();
    }

    /// <summary>
    /// Get unverified users
    /// </summary>
    public async Task<List<User>> GetUnverifiedUsersAsync(Guid tenantId)
    {
        return await DbSet
            .Where(u => u.TenantId == tenantId &&
                       !u.IsEmailVerified &&
                       !u.IsDeleted)
            .ToListAsync();
    }

    /// <summary>
    /// Get users who never logged in
    /// </summary>
    public async Task<List<User>> GetNeverLoggedInAsync(Guid tenantId)
    {
        return await DbSet
            .Where(u => u.TenantId == tenantId &&
                       !u.LastLoginAt.HasValue &&
                       !u.IsDeleted)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get locked accounts
    /// </summary>
    public async Task<List<User>> GetLockedAccountsAsync(Guid tenantId)
    {
        return await DbSet
            .Where(u => u.TenantId == tenantId &&
                       u.LockedUntil.HasValue &&
                       u.LockedUntil > DateTime.UtcNow &&
                       !u.IsDeleted)
            .ToListAsync();
    }

    /// <summary>
    /// Get user count for tenant
    /// </summary>
    public async Task<int> GetUserCountAsync(Guid tenantId)
    {
        return await DbSet.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted);
    }

    /// <summary>
    /// Get recently logged in users
    /// </summary>
    public async Task<List<User>> GetRecentlyActiveAsync(Guid tenantId, int days = 7)
    {
        var sinceDate = DateTime.UtcNow.AddDays(-days);
        return await DbSet
            .Where(u => u.TenantId == tenantId &&
                       u.LastLoginAt.HasValue &&
                       u.LastLoginAt >= sinceDate &&
                       !u.IsDeleted)
            .OrderByDescending(u => u.LastLoginAt)
            .ToListAsync();
    }

    /// <summary>
    /// Search users by name or email
    /// </summary>
    public async Task<List<User>> SearchAsync(Guid tenantId, string query)
    {
        var searchTerm = query.ToLower();
        return await DbSet
            .Where(u => u.TenantId == tenantId &&
                       (u.Email.ToLower().Contains(searchTerm) ||
                        u.FirstName.ToLower().Contains(searchTerm) ||
                        u.LastName.ToLower().Contains(searchTerm)) &&
                       !u.IsDeleted)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    /// <summary>
    /// Check if email is unique in tenant
    /// </summary>
    public async Task<bool> IsEmailUniqueAsync(string email, Guid tenantId, Guid? excludeUserId = null)
    {
        var query = DbSet.Where(u => u.Email == email &&
                                     u.TenantId == tenantId &&
                                     !u.IsDeleted);
        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return !await query.AnyAsync();
    }

    /// <summary>
    /// Get users needing password change
    /// </summary>
    public async Task<List<User>> GetUsersRequiringPasswordChangeAsync(Guid tenantId, int maxAgeDays = 90)
    {
        var threshold = DateTime.UtcNow.AddDays(-maxAgeDays);
        return await DbSet
            .Where(u => u.TenantId == tenantId &&
                       (!u.LastPasswordChangeAt.HasValue ||
                        u.LastPasswordChangeAt < threshold) &&
                       !u.IsDeleted)
            .ToListAsync();
    }

    /// <summary>
    /// Deactivate all users in organization
    /// </summary>
    public async Task<int> DeactivateOrganizationUsersAsync(Guid organizationId)
    {
        return await DbSet
            .Where(u => u.OrganizationId == organizationId && u.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false));
    }

    /// <summary>
    /// Get user activity statistics
    /// </summary>
    public async Task<object> GetUserStatisticsAsync(Guid tenantId)
    {
        return await DbSet
            .Where(u => u.TenantId == tenantId && !u.IsDeleted)
            .GroupBy(u => u.Role)
            .Select(g => new
            {
                Role = g.Key,
                Total = g.Count(),
                Active = g.Count(u => u.IsActive),
                VerifiedEmail = g.Count(u => u.IsEmailVerified),
                TwoFactorEnabled = g.Count(u => u.IsTwoFactorEnabled)
            })
            .ToListAsync();
    }
}
