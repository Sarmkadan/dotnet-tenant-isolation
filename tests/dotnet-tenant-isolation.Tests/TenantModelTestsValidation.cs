#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using TenantIsolation.Models;
using TenantIsolation.Constants;

namespace TenantIsolation.Tests;

/// <summary>
/// Validation helpers for Tenant model
/// </summary>
public static class TenantModelTestsValidation
{
    /// <summary>
    /// Validates a Tenant instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="tenant">The tenant to validate</param>
    /// <returns>List of validation errors; empty list if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if tenant is null</exception>
    public static IReadOnlyList<string> Validate(this Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        var errors = new List<string>();

        // Validate required properties
        if (string.IsNullOrWhiteSpace(tenant.Slug))
        {
            errors.Add("Slug is required and cannot be null or whitespace.");
        }
        else if (tenant.Slug.Length > 100)
        {
            errors.Add("Slug must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(tenant.Name))
        {
            errors.Add("Name is required and cannot be null or whitespace.");
        }
        else if (tenant.Name.Length > 255)
        {
            errors.Add("Name must be 255 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(tenant.AdminEmail))
        {
            errors.Add("AdminEmail is required and cannot be null or whitespace.");
        }
        else if (!tenant.AdminEmail.Contains("@"))
        {
            errors.Add("AdminEmail must be a valid email address.");
        }

        // Validate Status enum
        if (!Enum.IsDefined(typeof(TenantStatus), tenant.Status))
        {
            errors.Add($"Status '{tenant.Status}' is not a valid TenantStatus value.");
        }

        // Validate IsolationStrategy enum
        if (!Enum.IsDefined(typeof(TenantIsolationStrategy), tenant.IsolationStrategy))
        {
            errors.Add($"IsolationStrategy '{tenant.IsolationStrategy}' is not a valid TenantIsolationStrategy value.");
        }

        // Validate optional properties with ranges
        if (tenant.MaxUsers.HasValue && tenant.MaxUsers.Value <= 0)
        {
            errors.Add("MaxUsers must be a positive number or null.");
        }

        if (tenant.MaxStorageGb.HasValue && tenant.MaxStorageGb.Value <= 0)
        {
            errors.Add("MaxStorageGb must be a positive number or null.");
        }

        // Validate dates
        if (tenant.CreatedAt == default)
        {
            errors.Add("CreatedAt cannot be default(DateTime).");
        }
        else if (tenant.CreatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("CreatedAt cannot be in the future.");
        }

        if (tenant.UpdatedAt == default)
        {
            errors.Add("UpdatedAt cannot be default(DateTime).");
        }
        else if (tenant.UpdatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("UpdatedAt cannot be in the future.");
        }

        // Validate SubscriptionExpiresAt
        if (tenant.SubscriptionExpiresAt.HasValue)
        {
            if (tenant.SubscriptionExpiresAt.Value <= DateTime.UtcNow.AddDays(-1))
            {
                errors.Add("SubscriptionExpiresAt cannot be in the past.");
            }
        }

        // Validate soft delete consistency
        if (tenant.DeletedAt.HasValue)
        {
            if (tenant.DeletedAt.Value > DateTime.UtcNow.AddMinutes(5))
            {
                errors.Add("DeletedAt cannot be in the future.");
            }

            if (!tenant.IsDeleted)
            {
                errors.Add("If DeletedAt is set, IsDeleted must be true.");
            }
        }
        else
        {
            if (tenant.IsDeleted)
            {
                errors.Add("If IsDeleted is true, DeletedAt must be set.");
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Checks if a Tenant instance is valid.
    /// </summary>
    /// <param name="tenant">The tenant to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(this Tenant tenant)
    {
        return Validate(tenant).Count == 0;
    }

    /// <summary>
    /// Ensures a Tenant instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="tenant">The tenant to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if tenant is null</exception>
    /// <exception cref="ArgumentException">Thrown if tenant is invalid, containing validation errors</exception>
    public static void EnsureValid(this Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        var errors = Validate(tenant);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Tenant validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }
}
