#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;
using TenantIsolation.Constants;

namespace TenantIsolation.Models;

/// <summary>
/// Provides validation helpers for <see cref="DataIsolationPolicy"/> instances
/// </summary>
public static class DataIsolationPolicyValidation
{
    /// <summary>
    /// Validates a DataIsolationPolicy instance and returns a list of human-readable problems
    /// </summary>
    /// <param name="value">The policy to validate</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> Validate(this DataIsolationPolicy value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate required fields
        if (value.Id == Guid.Empty)
        {
            errors.Add("Id must be a non-empty GUID");
        }

        if (value.TenantId == Guid.Empty)
        {
            errors.Add("TenantId must be a non-empty GUID");
        }

        if (string.IsNullOrWhiteSpace(value.EntityType))
        {
            errors.Add("EntityType is required");
        }
        else if (value.EntityType.Length > 100)
        {
            errors.Add("EntityType must be 100 characters or less");
        }

        // Validate PolicyType
        if (!Enum.IsDefined(typeof(DataIsolationPolicyType), value.PolicyType))
        {
            errors.Add("PolicyType must be a valid DataIsolationPolicyType value");
        }

        // Validate Priority range
        if (value.Priority < 0 || value.Priority > 1000)
        {
            errors.Add("Priority must be between 0 and 1000");
        }

        // Validate CreatedAt and UpdatedAt dates
        if (value.CreatedAt == default)
        {
            errors.Add("CreatedAt must be set to a valid DateTime");
        }
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("CreatedAt cannot be in the future");
        }

        if (value.UpdatedAt == default)
        {
            errors.Add("UpdatedAt must be set to a valid DateTime");
        }
        else if (value.UpdatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("UpdatedAt cannot be in the future");
        }

        // Validate Custom policy requires FilterRule
        if (value.PolicyType == DataIsolationPolicyType.Custom && string.IsNullOrWhiteSpace(value.FilterRule))
        {
            errors.Add("FilterRule is required for Custom policy type");
        }

        // Validate field lists
        if (!string.IsNullOrWhiteSpace(value.AllowedFields))
        {
            var allowedFields = value.GetAllowedFields();
            if (allowedFields.Any(f => string.IsNullOrWhiteSpace(f)))
            {
                errors.Add("AllowedFields contains empty or whitespace field names");
            }
        }

        if (!string.IsNullOrWhiteSpace(value.DeniedFields))
        {
            var deniedFields = value.GetDeniedFields();
            if (deniedFields.Any(f => string.IsNullOrWhiteSpace(f)))
            {
                errors.Add("DeniedFields contains empty or whitespace field names");
            }
        }

        // Validate cross-tenant access list if present
        if (!string.IsNullOrWhiteSpace(value.AllowedCrossTenantAccess))
        {
            var tenants = value.AllowedCrossTenantAccess.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var tenant in tenants)
            {
                if (!Guid.TryParse(tenant.Trim(), out _))
                {
                    errors.Add("AllowedCrossTenantAccess contains invalid GUID format");
                    break;
                }
            }
        }

        // Validate field overlap
        var deniedFieldsList = value.GetDeniedFields();
        var allowedFieldsList = value.GetAllowedFields();

        if (deniedFieldsList.Count > 0 && allowedFieldsList.Count > 0)
        {
            var overlap = deniedFieldsList.Intersect(allowedFieldsList, StringComparer.OrdinalIgnoreCase).ToList();
            if (overlap.Count > 0)
            {
                errors.Add(string.Format("Fields cannot be in both allowed and denied lists: {0}", string.Join(", ", overlap)));
            }
        }

        // Validate Description length
        if (!string.IsNullOrWhiteSpace(value.Description) && value.Description.Length > 1000)
        {
            errors.Add("Description must be 1000 characters or less");
        }

        // Validate FilterRule length
        if (!string.IsNullOrWhiteSpace(value.FilterRule) && value.FilterRule.Length > 10000)
        {
            errors.Add("FilterRule must be 10000 characters or less");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a DataIsolationPolicy instance is valid
    /// </summary>
    /// <param name="value">The policy to check</param>
    /// <returns>True if the policy is valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static bool IsValid(this DataIsolationPolicy value)
    {
        try
        {
            return value.Validate().Count == 0;
        }
        catch (ArgumentNullException)
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures that a DataIsolationPolicy instance is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The policy to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    /// <exception cref="ArgumentException">Thrown if the policy is invalid, containing a list of problems</exception>
    public static void EnsureValid(this DataIsolationPolicy value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count == 0)
        {
            return;
        }

        var message = string.Join("- ", errors);
        throw new ArgumentException("DataIsolationPolicy validation failed:" + Environment.NewLine + "- " + message);
    }
}
