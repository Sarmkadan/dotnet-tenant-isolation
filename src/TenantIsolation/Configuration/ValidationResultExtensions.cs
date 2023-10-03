#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace TenantIsolation.Configuration;

/// <summary>
/// Extension methods for ValidationResult to provide fluent validation API and
/// convenient methods for working with validation results
/// </summary>
public static class ValidationResultExtensions
{
    /// <summary>
    /// Combine multiple validation results into one
    /// </summary>
    /// <param name="results">Array of validation results to combine</param>
    /// <returns>Combined validation result</returns>
    public static ValidationResult Combine(this ValidationResult[] results)
    {
        if (results == null || results.Length == 0)
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
    /// Combine multiple validation results into one (params overload)
    /// </summary>
    /// <param name="results">Validation results to combine</param>
    /// <returns>Combined validation result</returns>
    public static ValidationResult Combine(this IEnumerable<ValidationResult> results)
    {
        if (results == null)
        {
            return new ValidationResult { IsValid = true };
        }

        return results.ToArray().Combine();
    }

    /// <summary>
    /// Add an error to the validation result with formatting
    /// </summary>
    /// <param name="result">Validation result to add error to</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="propertyName">Optional property name for context</param>
    public static void AddError(this ValidationResult result, string errorMessage, string? propertyName = null)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Error message cannot be null or whitespace.", nameof(errorMessage));
        }

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
    /// Add a warning to the validation result with formatting
    /// </summary>
    /// <param name="result">Validation result to add warning to</param>
    /// <param name="warningMessage">Warning message</param>
    /// <param name="propertyName">Optional property name for context</param>
    public static void AddWarning(this ValidationResult result, string warningMessage, string? propertyName = null)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (string.IsNullOrWhiteSpace(warningMessage))
        {
            throw new ArgumentException("Warning message cannot be null or whitespace.", nameof(warningMessage));
        }

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
    /// Check if validation result has any errors
    /// </summary>
    /// <param name="result">Validation result to check</param>
    /// <returns>True if validation has errors, false otherwise</returns>
    public static bool HasErrors(this ValidationResult result)
    {
        return result?.Errors.Count > 0;
    }

    /// <summary>
    /// Check if validation result has any warnings
    /// </summary>
    /// <param name="result">Validation result to check</param>
    /// <returns>True if validation has warnings, false otherwise</returns>
    public static bool HasWarnings(this ValidationResult result)
    {
        return result?.Warnings.Count > 0;
    }

    /// <summary>
    /// Get the first error message if validation failed
    /// </summary>
    /// <param name="result">Validation result to get error from</param>
    /// <returns>First error message or null if no errors</returns>
    public static string? GetFirstError(this ValidationResult result)
    {
        return result?.Errors.FirstOrDefault();
    }

    /// <summary>
    /// Get the first warning message if validation has warnings
    /// </summary>
    /// <param name="result">Validation result to get warning from</param>
    /// <returns>First warning message or null if no warnings</returns>
    public static string? GetFirstWarning(this ValidationResult result)
    {
        return result?.Warnings.FirstOrDefault();
    }

    /// <summary>
    /// Log validation result using ILogger
    /// </summary>
    /// <param name="result">Validation result to log</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="context">Optional context for logging</param>
    public static void Log(this ValidationResult result, ILogger logger, string? context = null)
    {
        if (result == null || logger == null)
        {
            return;
        }

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
    /// Throw exception if validation result has errors
    /// </summary>
    /// <param name="result">Validation result to check</param>
    /// <param name="message">Optional custom exception message</param>
    /// <exception cref="InvalidOperationException">Thrown if validation has errors</exception>
    public static void ThrowIfInvalid(this ValidationResult result, string? message = null)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

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
    /// Merge another validation result into this one
    /// </summary>
    /// <param name="result">Target validation result</param>
    /// <param name="other">Validation result to merge</param>
    public static void Merge(this ValidationResult result, ValidationResult other)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

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
