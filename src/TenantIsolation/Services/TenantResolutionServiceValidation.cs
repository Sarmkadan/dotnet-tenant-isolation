#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics.CodeAnalysis;

namespace TenantIsolation.Services;

/// <summary>
/// Provides validation helpers for <see cref="TenantResolutionService"/> instances.
/// </summary>
public static class TenantResolutionServiceValidation
{
    /// <summary>
    /// Validates a <see cref="TenantResolutionService"/> instance.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <returns>A list of validation problems; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate([DisallowNull] this TenantResolutionService value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate injected dependencies
        ValidateDependencies(value, problems);

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="TenantResolutionService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    public static bool IsValid([NotNullWhen(true)] this TenantResolutionService? value)
        => value?.Validate() is { Count: 0 };

    /// <summary>
    /// Ensures that a <see cref="TenantResolutionService"/> instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid, containing a list of validation problems.</exception>
    public static void EnsureValid([DisallowNull] this TenantResolutionService value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"TenantResolutionService instance is invalid. {string.Join(" ", problems)}",
                nameof(value));
        }
    }

    private static void ValidateDependencies(TenantResolutionService service, List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(problems);

        // Validate IHttpContextAccessor
        if (service.GetHttpContextAccessor() is null)
        {
            problems.Add("IHttpContextAccessor dependency is null.");
        }

        // Validate IDynamicTenantStore
        if (service.GetDynamicTenantStore() is null)
        {
            problems.Add("IDynamicTenantStore dependency is null.");
        }

        // Validate ILogger
        if (service.GetLogger() is null)
        {
            problems.Add("ILogger<TenantResolutionService> dependency is null.");
        }
    }

    /// <summary>
    /// Gets the <see cref="IHttpContextAccessor"/> dependency from the service.
    /// </summary>
    /// <param name="service">The service instance.</param>
    /// <returns>The <see cref="IHttpContextAccessor"/> instance, or null if not set.</returns>
    private static IHttpContextAccessor? GetHttpContextAccessor(this TenantResolutionService service)
        => service.GetFieldValue<IHttpContextAccessor>("_httpContextAccessor");

    /// <summary>
    /// Gets the <see cref="IDynamicTenantStore"/> dependency from the service.
    /// </summary>
    /// <param name="service">The service instance.</param>
    /// <returns>The <see cref="IDynamicTenantStore"/> instance, or null if not set.</returns>
    private static IDynamicTenantStore? GetDynamicTenantStore(this TenantResolutionService service)
        => service.GetFieldValue<IDynamicTenantStore>("_dynamicTenantStore");

    /// <summary>
    /// Gets the <see cref="ILogger{T}"/> dependency from the service.
    /// </summary>
    /// <param name="service">The service instance.</param>
    /// <returns>The <see cref="ILogger{T}"/> instance, or null if not set.</returns>
    private static ILogger<TenantResolutionService>? GetLogger(this TenantResolutionService service)
        => service.GetFieldValue<ILogger<TenantResolutionService>>("_logger");

    /// <summary>
    /// Generic helper to safely retrieve a field value from a service instance.
    /// </summary>
    /// <typeparam name="T">The type of the field to retrieve.</typeparam>
    /// <param name="service">The service instance.</param>
    /// <param name="fieldName">The name of the field to retrieve.</param>
    /// <returns>The field value cast to type T, or null if the field doesn't exist or can't be cast.</returns>
    private static T? GetFieldValue<T>(this TenantResolutionService service, string fieldName) where T : class
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(fieldName);

        var field = service.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return field?.GetValue(service) as T;
    }
}