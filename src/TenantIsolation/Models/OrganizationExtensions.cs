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
    /// <returns<true if the organization has a contact email or phone number; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="organization"/> is null.</exception>
    public static bool IsValidContactPoint(this Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        return !string.IsNullOrEmpty(organization.ContactEmail) || !string.IsNullOrEmpty(organization.ContactPhone);
    }

    /// <summary>
    /// Gets a short description of the organization.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>A short description of the organization.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="organization"/> is null.</exception>
    public static string GetShortDescription(this Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        var description = organization.Description ?? string.Empty;
        if (string.IsNullOrEmpty(description))
        {
            description = organization.Name;
        }

        return description;
    }
}
