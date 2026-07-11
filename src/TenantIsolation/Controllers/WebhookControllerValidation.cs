#nullable enable

namespace TenantIsolation.Controllers;

/// <summary>
/// Validation helpers for WebhookController
/// </summary>
public static class WebhookControllerValidation
{
    /// <summary>
    /// Validates a WebhookController instance and returns a list of validation problems
    /// </summary>
    /// <param name="value">The WebhookController instance to validate</param>
    /// <returns>List of human-readable validation problems; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> Validate(this WebhookController value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate TenantId (should be a valid GUID string)
        if (string.IsNullOrWhiteSpace(value.TenantId))
        {
            problems.Add("TenantId cannot be null or empty");
        }
        else if (!Guid.TryParse(value.TenantId, out _))
        {
            problems.Add("TenantId must be a valid GUID");
        }

        // Validate EventType (should not be null or empty, reasonable length)
        if (string.IsNullOrWhiteSpace(value.EventType))
        {
            problems.Add("EventType cannot be null or empty");
        }
        else if (value.EventType.Length > 100)
        {
            problems.Add("EventType cannot exceed 100 characters");
        }

        // Validate Url (should be a valid absolute URL)
        if (string.IsNullOrWhiteSpace(value.Url))
        {
            problems.Add("Url cannot be null or empty");
        }
        else if (!Uri.TryCreate(value.Url, UriKind.Absolute, out var uriResult) ||
                 !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
            problems.Add("Url must be a valid absolute HTTP/HTTPS URL");
        }
        else if (uriResult.Host.Length > 253)
        {
            problems.Add("Url host cannot exceed 253 characters");
        }

        // Validate Secret (optional but if provided, should be reasonable length)
        if (!string.IsNullOrEmpty(value.Secret) && value.Secret.Length > 1000)
        {
            problems.Add("Secret cannot exceed 1000 characters");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if a WebhookController instance is valid
    /// </summary>
    /// <param name="value">The WebhookController instance to check</param>
    /// <returns>True if valid; false otherwise</returns>
    public static bool IsValid(this WebhookController value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures a WebhookController instance is valid, throwing ArgumentException if not
    /// </summary>
    /// <param name="value">The WebhookController instance to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    /// <exception cref="ArgumentException">Thrown if value has validation problems</exception>
    public static void EnsureValid(this WebhookController value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"WebhookController validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}