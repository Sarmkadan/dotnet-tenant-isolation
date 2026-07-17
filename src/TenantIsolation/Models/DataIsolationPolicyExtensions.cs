namespace TenantIsolation.Models;

/// <summary>
/// Provides extension methods for <see cref="DataIsolationPolicy"/>.
/// </summary>
public static class DataIsolationPolicyExtensions
{
    /// <summary>
    /// Determines if a <see cref="DataIsolationPolicy"/> allows access to a specific field.
    /// </summary>
    /// <param name="policy">The policy to check.</param>
    /// <param name="fieldName">The name of the field to check.</param>
    /// <returns>true if the field is allowed; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fieldName"/> is null or empty.</exception>
    public static bool IsFieldAllowed(this DataIsolationPolicy policy, string fieldName) =>
        policy.GetAllowedFields().Contains(fieldName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Determines if a <see cref="DataIsolationPolicy"/> denies access to a specific field.
    /// </summary>
    /// <param name="policy">The policy to check.</param>
    /// <param name="fieldName">The name of the field to check.</param>
    /// <returns>true if the field is denied; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fieldName"/> is null or empty.</exception>
    public static bool IsFieldDenied(this DataIsolationPolicy policy, string fieldName) =>
        policy.GetDeniedFields().Contains(fieldName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a string representation of the policy's filter rule and allowed/denied fields.
    /// </summary>
    /// <param name="policy">The policy to get a string representation for.</param>
    /// <returns>A string representation of the policy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
    public static string GetPolicySummary(this DataIsolationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var filterRule = policy.FilterRule ?? string.Empty;
        var allowedFields = string.Join(", ", policy.GetAllowedFields());
        var deniedFields = string.Join(", ", policy.GetDeniedFields());

        return $"Filter Rule: {filterRule}, Allowed Fields: {allowedFields}, Denied Fields: {deniedFields}";
    }
}
