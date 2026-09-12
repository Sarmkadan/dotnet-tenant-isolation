#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace TenantIsolation.Exceptions;

/// <summary>
/// Base exception for all tenant isolation framework errors
/// </summary>
public class TenantIsolationException : Exception
{
    /// <summary>
    /// Gets or sets a machine-readable code identifying the error.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of additional structured details about the error.
    /// </summary>
    public Dictionary<string, object?>? ErrorDetails { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantIsolationException"/> class.
    /// </summary>
    public TenantIsolationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantIsolationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TenantIsolationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantIsolationException"/> class with a specified error message and error code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The machine-readable code identifying the error.</param>
    public TenantIsolationException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantIsolationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public TenantIsolationException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantIsolationException"/> class with a specified error message, error code, and error details.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The machine-readable code identifying the error.</param>
    /// <param name="errorDetails">A dictionary of additional structured details about the error.</param>
    public TenantIsolationException(string message, string errorCode, Dictionary<string, object?> errorDetails)
        : base(message)
    {
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
    }

    /// <summary>
    /// Returns a string representation of the exception, including the error code when present.
    /// </summary>
    /// <returns>A string that represents the current exception.</returns>
    public override string ToString()
    {
        var details = ErrorCode != null ? $" [Code: {ErrorCode}]" : "";
        return base.ToString() + details;
    }
}

/// <summary>
/// Thrown when tenant cannot be resolved from request context
/// </summary>
public class TenantNotResolvedException : TenantIsolationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantNotResolvedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TenantNotResolvedException(string message = "Failed to resolve tenant from request context")
        : base(message, "TENANT_NOT_RESOLVED") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantNotResolvedException"/> class describing the source and identifier used when resolution failed.
    /// </summary>
    /// <param name="source">The source from which the tenant could not be resolved.</param>
    /// <param name="identifier">The identifier used during resolution, if any.</param>
    public TenantNotResolvedException(string source, string? identifier)
        : base(
            $"Tenant could not be resolved from {source}" +
            (identifier != null ? $" using identifier: {identifier}" : ""),
            "TENANT_NOT_RESOLVED")
    {
    }
}

/// <summary>
/// Thrown when tenant is inactive or disabled
/// </summary>
public class TenantNotActiveException : TenantIsolationException
{
    /// <summary>
    /// Gets the identifier of the tenant that is not active.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantNotActiveException"/> class with a specified tenant identifier and optional reason.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant that is not active.</param>
    /// <param name="reason">Optional reason why the tenant is not active.</param>
    public TenantNotActiveException(Guid tenantId, string? reason = null)
        : base($"Tenant {tenantId} is not active" + (reason != null ? $": {reason}" : ""),
            "TENANT_NOT_ACTIVE")
    {
        TenantId = tenantId;
    }
}

/// <summary>
/// Thrown when accessing tenant configuration fails
/// </summary>
public class TenantConfigurationException : TenantIsolationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TenantConfigurationException(string message)
        : base(message, "TENANT_CONFIG_ERROR") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfigurationException"/> class with a specified configuration key and error message.
    /// </summary>
    /// <param name="configKey">The configuration key that caused the error.</param>
    /// <param name="message">The message that describes the error.</param>
    public TenantConfigurationException(string configKey, string message)
        : base($"Configuration error for key '{configKey}': {message}",
            "TENANT_CONFIG_ERROR")
    {
    }
}

/// <summary>
/// Thrown when data isolation rules are violated
/// </summary>
public class DataIsolationViolationException : TenantIsolationException
{
    /// <summary>
    /// Gets the identifier of the tenant involved in the violation.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets the type of entity that was accessed in violation of isolation rules.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataIsolationViolationException"/> class with a specified tenant identifier and error message.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant involved in the violation.</param>
    /// <param name="message">The message that describes the error.</param>
    public DataIsolationViolationException(Guid tenantId, string message)
        : base($"Data isolation violation for tenant {tenantId}: {message}",
            "DATA_ISOLATION_VIOLATION")
    {
        TenantId = tenantId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataIsolationViolationException"/> class with a specified tenant identifier, entity type, and error message.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant involved in the violation.</param>
    /// <param name="entityType">The type of entity that was accessed in violation of isolation rules.</param>
    /// <param name="message">The message that describes the error.</param>
    public DataIsolationViolationException(Guid tenantId, string entityType, string message)
        : base($"Data isolation violation for tenant {tenantId} accessing {entityType}: {message}",
            "DATA_ISOLATION_VIOLATION")
    {
        TenantId = tenantId;
        EntityType = entityType;
    }
}

/// <summary>
/// Thrown when database connection fails
/// </summary>
public class TenantDatabaseException : TenantIsolationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantDatabaseException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TenantDatabaseException(string message)
        : base(message, "TENANT_DB_ERROR") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantDatabaseException"/> class with a specified tenant identifier, error message, and inner exception.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant whose database operation failed.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public TenantDatabaseException(Guid tenantId, string message, Exception innerException)
        : base($"Database error for tenant {tenantId}: {message}", innerException)
    {
        ErrorCode = "TENANT_DB_ERROR";
    }
}
