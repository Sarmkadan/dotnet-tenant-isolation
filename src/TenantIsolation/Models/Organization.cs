// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TenantIsolation.Models;

/// <summary>
/// Represents an organization within a tenant
/// </summary>
public class Organization
{
    /// <summary>
    /// Unique identifier for the organization
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant this organization belongs to
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Organization name
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional slug for URL-friendly identifier
    /// </summary>
    [StringLength(100)]
    public string? Slug { get; set; }

    /// <summary>
    /// Organization description
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Website URL
    /// </summary>
    [StringLength(500)]
    public string? Website { get; set; }

    /// <summary>
    /// Logo URL
    /// </summary>
    [StringLength(500)]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Primary contact email
    /// </summary>
    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = null!;

    /// <summary>
    /// Primary contact phone
    /// </summary>
    [StringLength(20)]
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Organization type (e.g., Company, NonProfit, Government)
    /// </summary>
    [StringLength(50)]
    public string? OrganizationType { get; set; }

    /// <summary>
    /// Number of employees/users in organization
    /// </summary>
    public int? EmployeeCount { get; set; }

    /// <summary>
    /// Industry classification
    /// </summary>
    [StringLength(100)]
    public string? Industry { get; set; }

    /// <summary>
    /// Country of operation (ISO 3166-1 alpha-2)
    /// </summary>
    [StringLength(2)]
    public string? CountryCode { get; set; }

    /// <summary>
    /// Business registration number
    /// </summary>
    [StringLength(50)]
    public string? RegistrationNumber { get; set; }

    /// <summary>
    /// Tax ID
    /// </summary>
    [StringLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// Is organization active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When organization was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When organization was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Custom metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Navigation property to tenant
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// Navigation property to users
    /// </summary>
    public virtual ICollection<User> Users { get; set; } = new List<User>();

    /// <summary>
    /// Validate organization can be activated
    /// </summary>
    public bool CanActivate(out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            errorMessage = "Organization name is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ContactEmail))
        {
            errorMessage = "Contact email is required";
            return false;
        }

        if (IsDeleted)
        {
            errorMessage = "Cannot activate a deleted organization";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get organization display name with tenant context
    /// </summary>
    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(Slug)
            ? $"{Name} ({Slug})"
            : Name;
    }

    /// <summary>
    /// Mark organization as deleted (soft delete)
    /// </summary>
    public void Delete()
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restore soft-deleted organization
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
