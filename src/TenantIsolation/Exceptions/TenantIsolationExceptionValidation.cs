#nullable enable

namespace TenantIsolation.Exceptions;

/// <summary>
/// Provides validation helpers for <see cref="TenantIsolationException"/> and derived exception types
/// </summary>
public static class TenantIsolationExceptionValidation
{
    /// <summary>
    /// Validates that a <see cref="TenantIsolationException"/> instance is valid
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <returns>A list of validation errors; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this TenantIsolationException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate ErrorCode
        if (value.ErrorCode is { Length: 0 })
        {
            errors.Add("ErrorCode cannot be an empty string");
        }

        // Validate ErrorDetails
        if (value.ErrorDetails != null)
        {
            if (value.ErrorDetails.Count == 0)
            {
                errors.Add("ErrorDetails dictionary cannot be empty");
            }

            foreach (var kvp in value.ErrorDetails)
            {
                if (kvp.Key is not { Length: > 0 })
                {
                    errors.Add("ErrorDetails contains an entry with null or empty key");
                    break;
                }
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="TenantIsolationException"/> is valid
    /// </summary>
    /// <param name="value">The exception to check</param>
    /// <returns>True if valid; otherwise false</returns>
    public static bool IsValid(this TenantIsolationException? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="TenantIsolationException"/> is valid, throwing an <see cref="ArgumentException"/> if not
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static void EnsureValid(this TenantIsolationException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"TenantIsolationException validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }

    /// <summary>
    /// Validates that a <see cref="TenantNotResolvedException"/> instance is valid
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <returns>A list of validation errors; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this TenantNotResolvedException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Base validation
        errors.AddRange(((TenantIsolationException)value).Validate());

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="TenantNotResolvedException"/> is valid
    /// </summary>
    /// <param name="value">The exception to check</param>
    /// <returns>True if valid; otherwise false</returns>
    public static bool IsValid(this TenantNotResolvedException? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="TenantNotResolvedException"/> is valid, throwing an <see cref="ArgumentException"/> if not
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static void EnsureValid(this TenantNotResolvedException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"TenantNotResolvedException validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }

    /// <summary>
    /// Validates that a <see cref="TenantNotActiveException"/> instance is valid
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <returns>A list of validation errors; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this TenantNotActiveException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Base validation
        errors.AddRange(((TenantIsolationException)value).Validate());

        // Validate TenantId
        if (value.TenantId == Guid.Empty)
        {
            errors.Add("TenantId cannot be an empty GUID");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="TenantNotActiveException"/> is valid
    /// </summary>
    /// <param name="value">The exception to check</param>
    /// <returns>True if valid; otherwise false</returns>
    public static bool IsValid(this TenantNotActiveException? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="TenantNotActiveException"/> is valid, throwing an <see cref="ArgumentException"/> if not
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static void EnsureValid(this TenantNotActiveException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"TenantNotActiveException validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }

    /// <summary>
    /// Validates that a <see cref="TenantConfigurationException"/> instance is valid
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <returns>A list of validation errors; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this TenantConfigurationException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Base validation
        errors.AddRange(((TenantIsolationException)value).Validate());

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="TenantConfigurationException"/> is valid
    /// </summary>
    /// <param name="value">The exception to check</param>
    /// <returns>True if valid; otherwise false</returns>
    public static bool IsValid(this TenantConfigurationException? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="TenantConfigurationException"/> is valid, throwing an <see cref="ArgumentException"/> if not
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static void EnsureValid(this TenantConfigurationException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"TenantConfigurationException validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }

    /// <summary>
    /// Validates that a <see cref="DataIsolationViolationException"/> instance is valid
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <returns>A list of validation errors; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this DataIsolationViolationException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Base validation
        errors.AddRange(((TenantIsolationException)value).Validate());

        // Validate TenantId
        if (value.TenantId == Guid.Empty)
        {
            errors.Add("TenantId cannot be an empty GUID");
        }

        // Validate EntityType
        if (value.EntityType is { Length: 0 })
        {
            errors.Add("EntityType cannot be an empty string");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="DataIsolationViolationException"/> is valid
    /// </summary>
    /// <param name="value">The exception to check</param>
    /// <returns>True if valid; otherwise false</returns>
    public static bool IsValid(this DataIsolationViolationException? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="DataIsolationViolationException"/> is valid, throwing an <see cref="ArgumentException"/> if not
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static void EnsureValid(this DataIsolationViolationException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"DataIsolationViolationException validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }

    /// <summary>
    /// Validates that a <see cref="TenantDatabaseException"/> instance is valid
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <returns>A list of validation errors; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this TenantDatabaseException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Base validation
        errors.AddRange(((TenantIsolationException)value).Validate());

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="TenantDatabaseException"/> is valid
    /// </summary>
    /// <param name="value">The exception to check</param>
    /// <returns>True if valid; otherwise false</returns>
    public static bool IsValid(this TenantDatabaseException? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="TenantDatabaseException"/> is valid, throwing an <see cref="ArgumentException"/> if not
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static void EnsureValid(this TenantDatabaseException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"TenantDatabaseException validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }
}
