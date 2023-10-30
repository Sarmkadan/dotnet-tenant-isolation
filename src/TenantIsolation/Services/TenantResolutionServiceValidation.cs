#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

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
        ValidateDependency(value, problems);

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
                "TenantResolutionService instance is invalid. " +
                string.Join(" ", problems),
                nameof(value));
        }
    }

    private static void ValidateDependency(TenantResolutionService service, List<string> problems)
    {
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

    // Reflection-based property accessors for testing dependencies without exposing internals
    private static object? GetHttpContextAccessor(this TenantResolutionService service)
        => service.GetType().GetField("_httpContextAccessor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(service);

    private static object? GetDynamicTenantStore(this TenantResolutionService service)
        => service.GetType().GetField("_dynamicTenantStore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(service);

    private static object? GetLogger(this TenantResolutionService service)
        => service.GetType().GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(service);
}