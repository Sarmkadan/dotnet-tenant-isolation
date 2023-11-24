#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace TenantIsolation.Configuration;

/// <summary>
/// Extension methods for <see cref="ValidationResult"/> to provide fluent validation API and
/// convenient methods for working with validation results.
/// </summary>
public static class ValidationResultExtensions
{
    /// <summary>
    /// Combine multiple validation results into one.
    /// </summary>
    /// <param name="results">Array of validation results to combine.</param>
    /// <returns>Combined validation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="results"/> is <see langword="null"/>.</exception>
    public static ValidationResult Combine(this ValidationResult[] results)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (results.Length == 0)
        {
            return new ValidationResult { IsValid = true };
        }

        var combined = new ValidationResult { IsValid = true };

        foreach (var result in results)
        {
            if (result == null)
            {
                continue;
            }

            if (!result.IsValid)
            {
                combined.IsValid = false;
            }

            combined.Errors.AddRange(result.Errors);
            combined.Warnings.AddRange(result.Warnings);
        }

        return combined;
    }

    /// <summary>
    /// Combine multiple validation results into one (params overload).
    /// </summary>
    /// <param name="results">Validation results to combine.</param>
    /// <returns>Combined validation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="results"/> is <see langword="null"/>.</exception>
    public static ValidationResult Combine(this IEnumerable<ValidationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.ToArray().Combine();
    }

    /// <summary>
    /// Add an error to the validation result with formatting.
    /// </summary>
    /// <param name="result">Validation result to add error to.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <param name="propertyName">Optional property name for context.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> or <paramref name="errorMessage"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="errorMessage"/> is null or whitespace.</exception>
    public static void AddError(this ValidationResult result, string errorMessage, string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        if (propertyName != null)
        {
            result.Errors.Add($"[{propertyName}] {errorMessage}");
        }
        else
        {
            result.Errors.Add(errorMessage);
        }

        result.IsValid = false;
    }

    /// <summary>
    /// Add a warning to the validation result with formatting.
    /// </summary>
    /// <param name="result">Validation result to add warning to.</param>
    /// <param name="warningMessage">Warning message.</param>
    /// <param name="propertyName">Optional property name for context.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> or <paramref name="warningMessage"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="warningMessage"/> is null or whitespace.</exception>
    public static void AddWarning(this ValidationResult result, string warningMessage, string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(warningMessage);

        if (propertyName != null)
        {
            result.Warnings.Add($"[{propertyName}] {warningMessage}");
        }
        else
        {
            result.Warnings.Add(warningMessage);
        }
    }

    /// <summary>
    /// Check if validation result has any errors.
    /// </summary>
    /// <param name="result">Validation result to check.</param>
    /// <returns>True if validation has errors, false otherwise.</returns>
    public static bool HasErrors(this ValidationResult? result) => result?.Errors.Count > 0;

    /// <summary>
    /// Check if validation result has any warnings.
    /// </summary>
    /// <param name="result">Validation result to check.</param>
    /// <returns>True if validation has warnings, false otherwise.</returns>
    public static bool HasWarnings(this ValidationResult? result) => result?.Warnings.Count > 0;

    /// <summary>
    /// Get the first error message if validation failed.
    /// </summary>
    /// <param name="result">Validation result to get error from.</param>
    /// <returns>First error message or null if no errors.</returns>
    public static string? GetFirstError(this ValidationResult? result) => result?.Errors.FirstOrDefault();

    /// <summary>
    /// Get the first warning message if validation has warnings.
    /// </summary>
    /// <param name="result">Validation result to get warning from.</param>
    /// <returns>First warning message or null if no warnings.</returns>
    public static string? GetFirstWarning(this ValidationResult? result) => result?.Warnings.FirstOrDefault();

    /// <summary>
    /// Log validation result using ILogger.
    /// </summary>
    /// <param name="result">Validation result to log.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="context">Optional context for logging.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is <see langword="null"/>.</exception>
    public static void Log(this ValidationResult result, ILogger logger, string? context = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var logContext = context != null ? $"[{context}] " : string.Empty;

        if (result.IsValid)
        {
            if (result.Warnings.Count > 0)
            {
                logger.LogInformation("{Context}Configuration validation passed with {WarningCount} warnings",
                    logContext, result.Warnings.Count);
            }
            else
            {
                logger.LogInformation("{Context}Configuration validation passed", logContext);
            }
        }
        else
        {
            logger.LogError("{Context}Configuration validation failed with {ErrorCount} errors and {WarningCount} warnings",
                logContext, result.Errors.Count, result.Warnings.Count);
        }
    }

    /// <summary>
    /// Throw exception if validation result has errors.
    /// </summary>
    /// <param name="result">Validation result to check.</param>
    /// <param name="message">Optional custom exception message.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if validation has errors.</exception>
    public static void ThrowIfInvalid(this ValidationResult result, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsValid && result.Errors.Count > 0)
        {
            var errorMessage = message ?? "Configuration validation failed";
            if (result.Errors.Count == 1)
            {
                throw new InvalidOperationException($"{errorMessage}: {result.Errors[0]}");
            }
            else
            {
                throw new InvalidOperationException($"{errorMessage}:{Environment.NewLine}{string.Join(Environment.NewLine, result.Errors)}");
            }
        }
    }

    /// <summary>
    /// Merge another validation result into this one.
    /// </summary>
    /// <param name="result">Target validation result.</param>
    /// <param name="other">Validation result to merge.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> is <see langword="null"/>.</exception>
    public static void Merge(this ValidationResult result, ValidationResult? other)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (other == null)
        {
            return;
        }

        if (!other.IsValid)
        {
            result.IsValid = false;
        }

        result.Errors.AddRange(other.Errors);
        result.Warnings.AddRange(other.Warnings);
    }
}