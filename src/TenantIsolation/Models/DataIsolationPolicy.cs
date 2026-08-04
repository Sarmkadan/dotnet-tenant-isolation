#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TenantIsolation.Constants;

namespace TenantIsolation.Models;

/// <summary>
/// Defines data isolation policies for a tenant.
/// </summary>
public class DataIsolationPolicy
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the associated tenant identifier.
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the policy type (Strict, Relaxed, Custom).
    /// </summary>
    public DataIsolationPolicyType PolicyType { get; set; } = DataIsolationPolicyType.Strict;

    /// <summary>
    /// Gets or sets the entity type this policy applies to (e.g., "Order", "Customer").
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EntityType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the policy description.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the filter rule in SQL/LINQ format.
    /// </summary>
    public string? FilterRule { get; set; }

    /// <summary>
    /// Gets or sets the allowed field access list (comma-separated).
    /// </summary>
    public string? AllowedFields { get; set; }

    /// <summary>
    /// Gets or sets the denied field access list (comma-separated).
    /// </summary>
    public string? DeniedFields { get; set; }

    /// <summary>
    /// Gets or sets the allowed cross-tenant access list (comma-separated tenant IDs).
    /// </summary>
    public string? AllowedCrossTenantAccess { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this policy is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the policy priority (lower = higher priority).
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Gets or sets the date and time when the policy was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the policy was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the navigation property to the associated tenant.
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// Parses the allowed fields string into a list of field names.
    /// </summary>
    /// <returns>A list of allowed field names.</returns>
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
    /// Parses the denied fields string into a list of field names.
    /// </summary>
    /// <returns>A list of denied field names.</returns>
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
    /// Checks if access to the specified field is allowed based on the policy.
    /// </summary>
    /// <param name="fieldName">The name of the field to check.</param>
    /// <returns><c>true</c> if access is allowed; otherwise, <c>false</c>.</returns>
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
    /// Checks if cross-tenant access to the specified tenant is allowed.
    /// </summary>
    /// <param name="otherTenantId">The unique identifier of the target tenant.</param>
    /// <returns><c>true</c> if access is allowed; otherwise, <c>false</c>.</returns>
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
    /// Validates the policy configuration.
    /// </summary>
    /// <param name="errorMessage">When this method returns, contains an error message if the policy is invalid; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the policy is valid; otherwise, <c>false</c>.</returns>
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
