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
}
