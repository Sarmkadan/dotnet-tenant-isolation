#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TenantIsolation.Models;

/// <summary>
/// Represents a user within a tenant and organization
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant this user belongs to
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Organization this user belongs to
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// User's email address (unique within tenant)
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = null!;

    /// <summary>
    /// User's first name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// User's last name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = null!;

    /// <summary>
    /// User's role within the organization
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Role { get; set; } = "User";

    /// <summary>
    /// Hashed password (should never be null unless SSO only)
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Is user account active/enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Has user verified their email
    /// </summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>
    /// Is user's two-factor authentication enabled
    /// </summary>
    public bool IsTwoFactorEnabled { get; set; }

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Number of failed login attempts
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Account locked until this time
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// Phone number for MFA
    /// </summary>
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Avatar/profile picture URL
    /// </summary>
    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// User preferences as JSON
    /// </summary>
    public string? Preferences { get; set; }

    /// <summary>
    /// Last password change date
    /// </summary>
    public DateTime? LastPasswordChangeAt { get; set; }

    /// <summary>
    /// When user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When user was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

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
    /// Navigation property to organization
    /// </summary>
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    /// <summary>
    /// Get user's full name
    /// </summary>
    public string GetFullName()
    {
        return $"{FirstName} {LastName}".Trim();
    }

    /// <summary>
    /// Check if account is locked
    /// </summary>
    public bool IsAccountLocked()
    {
        if (!LockedUntil.HasValue)
            return false;

        if (LockedUntil < DateTime.UtcNow)
        {
            LockedUntil = null;
            FailedLoginAttempts = 0;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Record a failed login attempt
    /// </summary>
    public void RecordFailedLoginAttempt(int maxAttempts = 5, int lockoutMinutes = 15)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maxAttempts)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Record successful login
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reset password after successful change
    /// </summary>
    public void SetPasswordHashAndReset(string passwordHash)
    {
        PasswordHash = passwordHash;
        LastPasswordChangeAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if password change is required
    /// </summary>
    public bool IsPasswordChangeRequired(int maxPasswordAgeDays = 90)
    {
        if (!LastPasswordChangeAt.HasValue)
            return true;

        return (DateTime.UtcNow - LastPasswordChangeAt.Value).TotalDays > maxPasswordAgeDays;
    }

    /// <summary>
    /// Verify user credentials are valid for login
    /// </summary>
    public bool CanLogin(out string? errorMessage)
    {
        errorMessage = null;

        if (!IsActive)
        {
            errorMessage = "User account is disabled";
            return false;
        }

        if (IsDeleted)
        {
            errorMessage = "User account has been deleted";
            return false;
        }

        if (IsAccountLocked())
        {
            errorMessage = $"Account is locked until {LockedUntil:O}";
            return false;
        }

        if (!IsEmailVerified)
        {
            errorMessage = "Email address must be verified";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Mark user as deleted (soft delete)
    /// </summary>
    public void Delete()
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
