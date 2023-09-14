#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TenantIsolation.Models;

/// <summary>
/// Manages feature toggles for tenants
/// </summary>
public class TenantFeature
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Associated tenant identifier
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Feature key/identifier
    /// </summary>
    [Required]
    [StringLength(100)]
    public string FeatureKey { get; set; } = null!;

    /// <summary>
    /// Feature display name
    /// </summary>
    [StringLength(255)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Feature description
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Is feature enabled for this tenant
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Feature category
    /// </summary>
    [StringLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Feature rollout percentage (0-100)
    /// </summary>
    public int RolloutPercentage { get; set; } = 100;

    /// <summary>
    /// Feature availability level (Beta, GA, Deprecated, etc.)
    /// </summary>
    [StringLength(50)]
    public string? AvailabilityLevel { get; set; } = "GA";

    /// <summary>
    /// When feature becomes available (null = immediately)
    /// </summary>
    public DateTime? AvailableFrom { get; set; }

    /// <summary>
    /// When feature will be discontinued (null = indefinitely)
    /// </summary>
    public DateTime? DeprecatedAt { get; set; }

    /// <summary>
    /// Feature usage limit (null = unlimited)
    /// </summary>
    public long? UsageLimit { get; set; }

    /// <summary>
    /// Current feature usage count
    /// </summary>
    public long CurrentUsage { get; set; }

    /// <summary>
    /// Feature metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When feature was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When feature was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to tenant
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// Check if feature is available for tenant
    /// </summary>
    public bool IsAvailable()
    {
        if (!IsEnabled)
            return false;

        if (AvailableFrom.HasValue && AvailableFrom > DateTime.UtcNow)
            return false;

        if (DeprecatedAt.HasValue && DeprecatedAt <= DateTime.UtcNow)
            return false;

        if (RolloutPercentage < 100)
            return Random.Shared.Next(1, 101) <= RolloutPercentage;

        return true;
    }

    /// <summary>
    /// Check if usage limit exceeded
    /// </summary>
    public bool IsUsageLimitExceeded()
    {
        if (!UsageLimit.HasValue)
            return false;

        return CurrentUsage >= UsageLimit.Value;
    }

    /// <summary>
    /// Check if feature can be used
    /// </summary>
    public bool CanUseFeature(out string? errorMessage)
    {
        errorMessage = null;

        if (!IsAvailable())
        {
            if (DeprecatedAt.HasValue && DeprecatedAt <= DateTime.UtcNow)
                errorMessage = $"This feature was deprecated on {DeprecatedAt:O}";
            else if (AvailableFrom.HasValue && AvailableFrom > DateTime.UtcNow)
                errorMessage = $"This feature will be available from {AvailableFrom:O}";
            else
                errorMessage = "This feature is not enabled";
            return false;
        }

        if (IsUsageLimitExceeded())
        {
            errorMessage = $"Usage limit of {UsageLimit} has been reached";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Record feature usage
    /// </summary>
    public void RecordUsage(long amount = 1)
    {
        CurrentUsage += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reset usage counter
    /// </summary>
    public void ResetUsage()
    {
        CurrentUsage = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get feature status
    /// </summary>
    public string GetStatus()
    {
        if (DeprecatedAt.HasValue && DeprecatedAt <= DateTime.UtcNow)
            return "Deprecated";

        if (!IsEnabled)
            return "Disabled";

        if (AvailableFrom.HasValue && AvailableFrom > DateTime.UtcNow)
            return "Pending";

        if (RolloutPercentage < 100)
            return $"Beta ({RolloutPercentage}%)";

        return "Active";
    }
}
