#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;

namespace TenantIsolation.Models;

/// <summary>
/// Represents a tenant in the multi-tenancy system. Each tenant has its own data isolation
/// boundary, user limits, subscription plan, and lifecycle (provisioning -> active -> suspended/archived).
/// </summary>
/// <remarks>
/// <para>
/// Tenants support three isolation strategies via <see cref="IsolationStrategy"/>:
/// <list type="bullet">
///   <item><see cref="TenantIsolationStrategy.DatabasePerTenant"/> - each tenant gets a separate database</item>
///   <item><see cref="TenantIsolationStrategy.SchemaPerTenant"/> - shared database, separate schemas</item>
///   <item><see cref="TenantIsolationStrategy.SharedDatabase"/> - shared tables with tenant ID column filtering</item>
/// </list>
/// </para>
/// <para>
/// Soft-delete is supported via <see cref="Delete"/> and <see cref="Restore"/>.
/// Deleted tenants are excluded from resolution but their data is preserved.
/// </para>
/// </remarks>
public class Tenant
{
    /// <summary>
    /// Unique identifier for the tenant
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Unique slug/subdomain for the tenant
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Slug { get; set; } = null!;

    /// <summary>
    /// Display name of the tenant
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Description of the tenant
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Contact email for tenant administrator
    /// </summary>
    [Required]
    [EmailAddress]
    public string AdminEmail { get; set; } = null!;

    /// <summary>
    /// Contact phone number
    /// </summary>
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Current status of the tenant
    /// </summary>
    public TenantStatus Status { get; set; } = TenantStatus.Provisioning;

    /// <summary>
    /// Data isolation strategy used for this tenant
    /// </summary>
    public TenantIsolationStrategy IsolationStrategy { get; set; } = TenantIsolationStrategy.DatabasePerTenant;

    /// <summary>
    /// Subscription plan identifier
    /// </summary>
    [StringLength(50)]
    public string? PlanId { get; set; }

    /// <summary>
    /// Maximum number of users allowed (null = unlimited)
    /// </summary>
    public int? MaxUsers { get; set; }

    /// <summary>
    /// Maximum storage in GB (null = unlimited)
    /// </summary>
    public decimal? MaxStorageGb { get; set; }

    /// <summary>
    /// When the tenant was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the tenant was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the tenant subscription expires (null = no expiration)
    /// </summary>
    public DateTime? SubscriptionExpiresAt { get; set; }

    /// <summary>
    /// Custom metadata stored as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether this tenant is deleted (soft delete)
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When the tenant was deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Validate tenant can be activated
    /// </summary>
    public bool CanActivate()
    {
        return !IsDeleted && Status != TenantStatus.Archived &&
               (SubscriptionExpiresAt == null || SubscriptionExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    /// Check if tenant has exceeded user limit
    /// </summary>
    public bool IsUserLimitExceeded(int currentUserCount)
    {
        return MaxUsers.HasValue && currentUserCount >= MaxUsers.Value;
    }

    /// <summary>
    /// Check if subscription is valid
    /// </summary>
    public bool IsSubscriptionValid()
    {
        return SubscriptionExpiresAt == null || SubscriptionExpiresAt > DateTime.UtcNow;
    }

    /// <summary>
    /// Check if tenant is in trial period
    /// </summary>
    public bool IsInTrial()
    {
        return Status == TenantStatus.Trial;
    }

    /// <summary>
    /// Mark tenant as deleted (soft delete)
    /// </summary>
    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Status = TenantStatus.Archived;
    }

    /// <summary>
    /// Restore soft-deleted tenant
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        Status = TenantStatus.Active;
    }

    /// <summary>
    /// Suspend tenant access
    /// </summary>
    public void Suspend(string? reason = null)
    {
        Status = TenantStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }
}
