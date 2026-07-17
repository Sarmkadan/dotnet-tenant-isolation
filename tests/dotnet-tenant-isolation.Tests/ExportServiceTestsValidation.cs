using System;
using System.Collections.Generic;

namespace TenantIsolation.Tests;

public static class ExportServiceTestsValidation
{
    /// <summary>
    /// Validates an ExportServiceTests instance for common issues.
    /// </summary>
    /// <param name="value">The ExportServiceTests instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this ExportServiceTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate private fields and dependencies
        // _loggerMock should not be null
        if (value.GetType().GetField("_loggerMock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(value) is null)
        {
            problems.Add("Logger mock field is null");
        }

        // _exportService should not be null
        if (value.GetType().GetField("_exportService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(value) is null)
        {
            problems.Add("Export service field is null");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an ExportServiceTests instance is valid.
    /// </summary>
    /// <param name="value">The ExportServiceTests instance to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static bool IsValid(this ExportServiceTests value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that an ExportServiceTests instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The ExportServiceTests instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentException">Thrown if value is not valid, containing the validation problems.</exception>
    public static void EnsureValid(this ExportServiceTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException($"ExportServiceTests instance is not valid. Problems: {string.Join("; ", problems)}");
        }
    }
}