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
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/></exception>
    public static TenantIsolationException WithDetail(this TenantIsolationException exception, string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(key);

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
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="details"/> is <see langword="null"/></exception>
    public static TenantIsolationException WithDetails(this TenantIsolationException exception, Dictionary<string, object?> details)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(details);

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
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="newErrorCode"/> is <see langword="null"/></exception>
    public static TenantIsolationException WithErrorCode(this TenantIsolationException exception, string newErrorCode)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(newErrorCode);

        var errorDetails = exception.ErrorDetails?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object?>();
        return new TenantIsolationException(exception.Message, newErrorCode, errorDetails)
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
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/></exception>
    public static TenantIsolationException WithContext(this TenantIsolationException exception, string context)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(context);

        var errorDetails = exception.ErrorDetails?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object?>();
        return new TenantIsolationException(exception.Message + " " + context, exception.ErrorCode ?? string.Empty, errorDetails)
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
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
    public static bool TryGetTenantId(this TenantIsolationException exception, out Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(exception);
        tenantId = Guid.Empty;

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
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
    public static bool TryGetEntityType(this TenantIsolationException exception, out string? entityType)
    {
        ArgumentNullException.ThrowIfNull(exception);
        entityType = null;

        if (exception is DataIsolationViolationException dataViolation)
        {
            entityType = dataViolation.EntityType;
            return true;
        }

        return false;
    }
}