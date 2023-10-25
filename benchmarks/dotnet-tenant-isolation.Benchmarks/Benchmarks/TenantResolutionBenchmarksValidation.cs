#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================
using System;
using System.Collections.Generic;

namespace TenantIsolation.Benchmarks;

/// <summary>
/// Validation helpers for <see cref="TenantResolutionBenchmarks"/> class.
/// Ensures benchmark setup is valid before execution.
/// </summary>
public static class TenantResolutionBenchmarksValidation
{
    /// <summary>
    /// Validates a <see cref="TenantResolutionBenchmarks"/> instance.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this TenantResolutionBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate that the instance has been properly initialized
        // We can't access private fields, so we validate based on observable state

        // The Setup() method must have been called for the benchmarks to work
        // We can't directly check private fields, but we can check if the
        // instance appears to be in a usable state by attempting to access
        // the resolution service through its public interface
        try
        {
            // If Setup() was called, _resolutionService should be non-null
            // We can't access it directly, but we can check if the instance
            // is in a state where it would throw if used
            var dummy = value.GetCurrentTenant();
        }
        catch (NullReferenceException)
        {
            problems.Add("Setup() was not called or failed - internal services are not initialized.");
        }
        catch (InvalidOperationException)
        {
            // Expected if Setup() wasn't called
            problems.Add("Setup() was not called or failed - internal services are not initialized.");
        }
        catch (Exception ex) when (ex is not ArgumentNullException and not ArgumentException)
        {
            // Any other exception during basic operations indicates setup failure
            problems.Add($"Setup() failed or instance is in invalid state: {ex.Message}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="TenantResolutionBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this TenantResolutionBenchmarks value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="TenantResolutionBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails, containing a list of problems.</exception>
    public static void EnsureValid(this TenantResolutionBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"TenantResolutionBenchmarks instance is not valid. Problems:\n{string.Join("\n", problems)}",
                nameof(value));
        }
    }
}
