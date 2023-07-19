#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TenantIsolation.Models;

/// <summary>
/// Defines data isolation policies for a tenant
/// </summary>
public class DataIsolationPolicy
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
    /// Policy type (Strict, Relaxed, Custom)
    /// </summary>
    public DataIsolationPolicyType PolicyType { get; set; } = DataIsolationPolicyType.Strict;

    /// <summary>
    /// Entity type this policy applies to (e.g., "Order", "Customer")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EntityType { get; set; } = null!;

    /// <summary>
    /// Policy description
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Filter rule in SQL/LINQ format
    /// </summary>
    public string? FilterRule { get; set; }

    /// <summary>
    /// Allowed field access list (comma-separated)
    /// </summary>
    public string? AllowedFields { get; set; }

    /// <summary>
    /// Denied field access list (comma-separated)
    /// </summary>
    public string? DeniedFields { get; set; }

    /// <summary>
    /// Allowed cross-tenant access list (comma-separated tenant IDs)
    /// </summary>
    public string? AllowedCrossTenantAccess { get; set; }

    /// <summary>
    /// Is this policy active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Policy priority (lower = higher priority)
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// When policy was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When policy was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to tenant
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// Parse allowed fields into list
    /// </summary>
    public List<string> GetAllowedFields()
    {
        if (string.IsNullOrWhiteSpace(AllowedFields))
            return new List<string>();

        return AllowedFields
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();
    }

    /// <summary>
    /// Parse denied fields into list
    /// </summary>
    public List<string> GetDeniedFields()
    {
        if (string.IsNullOrWhiteSpace(DeniedFields))
            return new List<string>();

        return DeniedFields
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();
    }

    /// <summary>
    /// Check if field access is allowed
    /// </summary>
    public bool IsFieldAccessAllowed(string fieldName)
    {
        var deniedFields = GetDeniedFields();
        if (deniedFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
            return false;

        var allowedFields = GetAllowedFields();
        if (allowedFields.Count > 0)
            return allowedFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase);

        return true;
    }

    /// <summary>
    /// Check if cross-tenant access is allowed
    /// </summary>
    public bool IsCrossTenantAccessAllowed(Guid otherTenantId)
    {
        if (PolicyType == DataIsolationPolicyType.Strict)
            return false;

        if (string.IsNullOrWhiteSpace(AllowedCrossTenantAccess))
            return false;

        var allowedTenants = AllowedCrossTenantAccess
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => Guid.Parse(t.Trim()))
            .ToList();

        return allowedTenants.Contains(otherTenantId);
    }

    /// <summary>
    /// Validate policy configuration
    /// </summary>
    public bool IsValidPolicy(out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(EntityType))
        {
            errorMessage = "Entity type is required";
            return false;
        }

        if (PolicyType == DataIsolationPolicyType.Custom && string.IsNullOrWhiteSpace(FilterRule))
        {
            errorMessage = "Filter rule is required for custom policies";
            return false;
        }

        var deniedFields = GetDeniedFields();
        var allowedFields = GetAllowedFields();

        if (deniedFields.Count > 0 && allowedFields.Count > 0)
        {
            var overlap = deniedFields.Intersect(allowedFields, StringComparer.OrdinalIgnoreCase).ToList();
            if (overlap.Count > 0)
            {
                errorMessage = $"Fields cannot be in both allowed and denied lists: {string.Join(", ", overlap)}";
                return false;
            }
        }

        return true;
    }
}
