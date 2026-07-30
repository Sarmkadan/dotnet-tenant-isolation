using System;

namespace TenantIsolation.Models;

/// <summary>
/// Extension methods for <see cref="Organization"/>.
/// </summary>
public static class OrganizationExtensions
{
    /// <summary>
    /// Determines whether the organization is a valid contact point.
    /// </summary>
    /// <param name="organization">The organization to check.</param>
    /// <returns><see langword="true"/> if the organization has a contact email or phone number; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="organization"/> is <see langword="null"/>.</exception>
    public static bool IsValidContactPoint(this Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        return !string.IsNullOrEmpty(organization.ContactEmail) || !string.IsNullOrEmpty(organization.ContactPhone);
    }

    /// <summary>
    /// Gets a short description of the organization.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>A short description of the organization. Returns the organization name if no description is available.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="organization"/> is <see langword="null"/>.</exception>
    public static string GetShortDescription(this Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        return organization.Description is not null && !string.IsNullOrEmpty(organization.Description)
            ? organization.Description
            : organization.Name;
    }

    /// <summary>
    /// Determines whether the organization is considered active.
    /// </summary>
    /// <param name="organization">The organization to evaluate.</param>
    /// <returns><see langword="true"/> if the organization is active and not soft‑deleted; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="organization"/> is <see langword="null"/>.</exception>
    public static bool IsActive(this Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        // An organization is active when its IsActive flag is true and it hasn't been soft‑deleted.
        return organization.IsActive && !organization.IsDeleted;
    }

    /// <summary>
    /// Gets a display name for the organization, falling back to its identifier when the name is missing.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>
    /// The organization name (including slug if present) when a name is available; otherwise the organization <see cref="Guid"/> as a string.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="organization"/> is <see langword="null"/>.</exception>
    public static string GetDisplayName(this Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        if (!string.IsNullOrWhiteSpace(organization.Name))
        {
            return !string.IsNullOrEmpty(organization.Slug)
                ? $"{organization.Name} ({organization.Slug})"
                : organization.Name;
        }

        // Fallback to the identifier when the name is empty.
        return organization.Id.ToString();
    }

    /// <summary>
    /// Returns a one‑line summary of the organization.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>A concise string containing the Id, name (or Id fallback), optional slug, and active status.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="organization"/> is <see langword="null"/>.</exception>
    public static string ToSummary(this Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        var namePart = !string.IsNullOrWhiteSpace(organization.Name) ? organization.Name : organization.Id.ToString();
        var slugPart = !string.IsNullOrEmpty(organization.Slug) ? $" [{organization.Slug}]" : string.Empty;
        var statusPart = organization.IsActive && !organization.IsDeleted ? "Active" : "Inactive";

        return $"{organization.Id} - {namePart}{slugPart} - {statusPart}";
    }
}
