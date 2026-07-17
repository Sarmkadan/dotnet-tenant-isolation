#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using TenantIsolation.Models;

namespace TenantIsolation.Data;

/// <summary>
/// Validation helpers for OrganizationRepository to ensure data integrity
/// </summary>
public static class OrganizationRepositoryValidation
{
    /// <summary>
    /// Validates an OrganizationRepository instance and returns a list of validation problems
    /// </summary>
    /// <param name="value">The repository instance to validate</param>
    /// <returns>List of human-readable validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this OrganizationRepository? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return [];
    }

    /// <summary>
    /// Checks if an OrganizationRepository instance is valid
    /// </summary>
    /// <param name="value">The repository instance to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(this OrganizationRepository? value) => Validate(value).Count == 0;

    /// <summary>
    /// Ensures an OrganizationRepository instance is valid, throwing if not
    /// </summary>
    /// <param name="value">The repository instance to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown with validation problems if invalid</exception>
    public static void EnsureValid(this OrganizationRepository? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _ = Validate(value);
    }

    /// <summary>
    /// Validates parameters for GetBySlugAsync method
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="slug">Organization slug</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if tenantId is empty</exception>
    public static IReadOnlyList<string> ValidateGetBySlugAsync(Guid tenantId, string slug)
    {
        var problems = new List<string>();

        if (tenantId == Guid.Empty)
        {
            problems.Add("Tenant ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            problems.Add("Slug cannot be null or whitespace");
        }
        else if (slug.Length > 100)
        {
            problems.Add("Slug cannot exceed 100 characters");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for GetActiveOrganizationsAsync method
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if tenantId is empty</exception>
    public static IReadOnlyList<string> ValidateGetActiveOrganizationsAsync(Guid tenantId)
    {
        return tenantId == Guid.Empty
            ? ["Tenant ID cannot be empty"]
            : [];
    }

    /// <summary>
    /// Validates parameters for GetWithUsersAsync method
    /// </summary>
    /// <param name="id">Organization identifier</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if id is empty</exception>
    public static IReadOnlyList<string> ValidateGetWithUsersAsync(Guid id)
    {
        return id == Guid.Empty
            ? ["Organization ID cannot be empty"]
            : [];
    }

    /// <summary>
    /// Validates parameters for GetByIndustryAsync method
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="industry">Industry classification</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if tenantId is empty or industry is invalid</exception>
    public static IReadOnlyList<string> ValidateGetByIndustryAsync(Guid tenantId, string industry)
    {
        var problems = new List<string>();

        if (tenantId == Guid.Empty)
        {
            problems.Add("Tenant ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(industry))
        {
            problems.Add("Industry cannot be null or whitespace");
        }
        else if (industry.Length > 100)
        {
            problems.Add("Industry cannot exceed 100 characters");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for GetByCountryAsync method
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="countryCode">Country code (ISO 3166-1 alpha-2)</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if tenantId is empty or countryCode is invalid</exception>
    public static IReadOnlyList<string> ValidateGetByCountryAsync(Guid tenantId, string countryCode)
    {
        var problems = new List<string>();

        if (tenantId == Guid.Empty)
        {
            problems.Add("Tenant ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            problems.Add("Country code cannot be null or whitespace");
        }
        else if (countryCode.Length != 2)
        {
            problems.Add("Country code must be exactly 2 characters (ISO 3166-1 alpha-2)");
        }
        else if (!countryCode.All(char.IsLetter))
        {
            problems.Add("Country code must contain only letters");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for SearchAsync method
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="query">Search query</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if tenantId is empty or query is invalid</exception>
    public static IReadOnlyList<string> ValidateSearchAsync(Guid tenantId, string query)
    {
        var problems = new List<string>();

        if (tenantId == Guid.Empty)
        {
            problems.Add("Tenant ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            problems.Add("Search query cannot be null or whitespace");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for GetOrganizationCountAsync method
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if tenantId is empty</exception>
    public static IReadOnlyList<string> ValidateGetOrganizationCountAsync(Guid tenantId)
    {
        return tenantId == Guid.Empty
            ? ["Tenant ID cannot be empty"]
            : [];
    }

    /// <summary>
    /// Validates parameters for IsSlugUniqueAsync method
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="slug">Organization slug to check</param>
    /// <param name="excludeId">Optional organization ID to exclude from check</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if tenantId is empty or slug/excludeId is invalid</exception>
    public static IReadOnlyList<string> ValidateIsSlugUniqueAsync(Guid tenantId, string slug, Guid? excludeId = null)
    {
        var problems = new List<string>();

        if (tenantId == Guid.Empty)
        {
            problems.Add("Tenant ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            problems.Add("Slug cannot be null or whitespace");
        }
        else if (slug.Length > 100)
        {
            problems.Add("Slug cannot exceed 100 characters");
        }

        if (excludeId.HasValue && excludeId.Value == Guid.Empty)
        {
            problems.Add("Exclude ID cannot be empty if provided");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for GetOrganizationsWithUserCountAsync method
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if tenantId is empty</exception>
    public static IReadOnlyList<string> ValidateGetOrganizationsWithUserCountAsync(Guid tenantId)
    {
        return tenantId == Guid.Empty
            ? ["Tenant ID cannot be empty"]
            : [];
    }

    /// <summary>
    /// Validates parameters for GetByRegistrationNumberAsync method
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="registrationNumber">Registration number to search for</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if tenantId is empty or registrationNumber is invalid</exception>
    public static IReadOnlyList<string> ValidateGetByRegistrationNumberAsync(Guid tenantId, string registrationNumber)
    {
        var problems = new List<string>();

        if (tenantId == Guid.Empty)
        {
            problems.Add("Tenant ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(registrationNumber))
        {
            problems.Add("Registration number cannot be null or whitespace");
        }
        else if (registrationNumber.Length > 50)
        {
            problems.Add("Registration number cannot exceed 50 characters");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for DeactivateAsync method
    /// </summary>
    /// <param name="id">Organization identifier to deactivate</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if id is empty</exception>
    public static IReadOnlyList<string> ValidateDeactivateAsync(Guid id)
    {
        return id == Guid.Empty
            ? ["Organization ID cannot be empty"]
            : [];
    }

    /// <summary>
    /// Validates parameters for GetStatisticsAsync method
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if tenantId is empty</exception>
    public static IReadOnlyList<string> ValidateGetStatisticsAsync(Guid tenantId)
    {
        return tenantId == Guid.Empty
            ? ["Tenant ID cannot be empty"]
            : [];
    }

    /// <summary>
    /// Validates parameters for BulkActivateAsync method
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="organizationIds">List of organization identifiers to activate</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if tenantId is empty or organizationIds is invalid</exception>
    public static IReadOnlyList<string> ValidateBulkActivateAsync(Guid tenantId, List<Guid>? organizationIds)
    {
        var problems = new List<string>();

        if (tenantId == Guid.Empty)
        {
            problems.Add("Tenant ID cannot be empty");
        }

        if (organizationIds is null)
        {
            problems.Add("Organization IDs list cannot be null");
        }
        else if (organizationIds.Any(id => id == Guid.Empty))
        {
            problems.Add("Organization IDs cannot contain empty GUIDs");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for GetRecentAsync method
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="count">Number of recent organizations to retrieve</param>
    /// <returns>List of validation problems, or empty list if valid</returns>
    /// <exception cref="ArgumentException">Thrown if tenantId is empty or count is invalid</exception>
    public static IReadOnlyList<string> ValidateGetRecentAsync(Guid tenantId, int count = 10)
    {
        var problems = new List<string>();

        if (tenantId == Guid.Empty)
        {
            problems.Add("Tenant ID cannot be empty");
        }

        if (count <= 0)
        {
            problems.Add("Count must be greater than zero");
        }
        else if (count > 1000)
        {
            problems.Add("Count cannot exceed 1000");
        }

        return problems.AsReadOnly();
    }
}