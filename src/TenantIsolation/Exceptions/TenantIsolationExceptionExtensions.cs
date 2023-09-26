#nullable enable

namespace TenantIsolation.Exceptions;

/// <summary>
/// Extension methods for <see cref="TenantIsolationException"/> and derived exception types
/// </summary>
public static class TenantIsolationExceptionExtensions
{
    /// <summary>
    /// Adds or updates an error detail entry in the exception's ErrorDetails dictionary
    /// </summary>
    /// <param name="exception">The exception instance</param>
    /// <param name="key">The key for the error detail</param>
    /// <param name="value">The value to store</param>
    /// <returns>The same exception instance for method chaining</returns>
    public static TenantIsolationException WithDetail(this TenantIsolationException exception, string key, object? value)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        exception.ErrorDetails ??= new Dictionary<string, object?>();
        exception.ErrorDetails[key] = value;
        return exception;
    }

    /// <summary>
    /// Adds multiple error details at once
    /// </summary>
    /// <param name="exception">The exception instance</param>
    /// <param name="details">Dictionary of key-value pairs to add</param>
    /// <returns>The same exception instance for method chaining</returns>
    public static TenantIsolationException WithDetails(this TenantIsolationException exception, Dictionary<string, object?> details)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        if (details == null)
        {
            throw new ArgumentNullException(nameof(details));
        }

        exception.ErrorDetails ??= new Dictionary<string, object?>();
        foreach (var kvp in details)
        {
            exception.ErrorDetails[kvp.Key] = kvp.Value;
        }
        return exception;
    }

    /// <summary>
    /// Creates a new exception with the same properties but a different error code
    /// </summary>
    /// <param name="exception">The exception instance</param>
    /// <param name="newErrorCode">The new error code to set</param>
    /// <returns>A new exception instance with the specified error code</returns>
    public static TenantIsolationException WithErrorCode(this TenantIsolationException exception, string newErrorCode)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        if (newErrorCode == null)
        {
            throw new ArgumentNullException(nameof(newErrorCode));
        }

        return new TenantIsolationException(exception.Message, newErrorCode, exception.ErrorDetails?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
        {
            Source = exception.Source,
            HelpLink = exception.HelpLink,
            HResult = exception.HResult,
            Data = { ["OriginalException"] = exception }
        };
    }

    /// <summary>
    /// Creates a new exception with additional context appended to the message
    /// </summary>
    /// <param name="exception">The exception instance</param>
    /// <param name="context">Additional context to append to the message</param>
    /// <returns>A new exception instance with enhanced message</returns>
    public static TenantIsolationException WithContext(this TenantIsolationException exception, string context)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return new TenantIsolationException(exception.Message + " " + context, exception.ErrorCode, exception.ErrorDetails?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
        {
            Source = exception.Source,
            HelpLink = exception.HelpLink,
            HResult = exception.HResult,
            Data = { ["Context"] = context }
        };
    }

    /// <summary>
    /// Gets the tenant ID if the exception is a TenantNotActiveException or DataIsolationViolationException
    /// </summary>
    /// <param name="exception">The exception instance</param>
    /// <param name="tenantId">Output parameter containing the tenant ID if available</param>
    /// <returns>True if tenant ID is available and was retrieved; false otherwise</returns>
    public static bool TryGetTenantId(this TenantIsolationException exception, out Guid tenantId)
    {
        tenantId = Guid.Empty;
        if (exception == null)
        {
            return false;
        }

        switch (exception)
        {
            case TenantNotActiveException tenantNotActive:
                tenantId = tenantNotActive.TenantId;
                return true;

            case DataIsolationViolationException dataViolation:
                tenantId = dataViolation.TenantId;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Gets the entity type if the exception is a DataIsolationViolationException
    /// </summary>
    /// <param name="exception">The exception instance</param>
    /// <param name="entityType">Output parameter containing the entity type if available</param>
    /// <returns>True if entity type is available and was retrieved; false otherwise</returns>
    public static bool TryGetEntityType(this TenantIsolationException exception, out string? entityType)
    {
        entityType = null;
        if (exception == null)
        {
            return false;
        }

        if (exception is DataIsolationViolationException dataViolation)
        {
            entityType = dataViolation.EntityType;
            return true;
        }

        return false;
    }
}