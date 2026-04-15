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
    public string? ErrorCode { get; set; }
    public Dictionary<string, object?>? ErrorDetails { get; set; }

    public TenantIsolationException() { }

    public TenantIsolationException(string message) : base(message) { }

    public TenantIsolationException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public TenantIsolationException(string message, Exception innerException)
        : base(message, innerException) { }

    public TenantIsolationException(string message, string errorCode, Dictionary<string, object?> errorDetails)
        : base(message)
    {
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
    }

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
    public TenantNotResolvedException(string message = "Failed to resolve tenant from request context")
        : base(message, "TENANT_NOT_RESOLVED") { }

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
    public Guid TenantId { get; set; }

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
    public TenantConfigurationException(string message)
        : base(message, "TENANT_CONFIG_ERROR") { }

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
    public Guid TenantId { get; set; }
    public string? EntityType { get; set; }

    public DataIsolationViolationException(Guid tenantId, string message)
        : base($"Data isolation violation for tenant {tenantId}: {message}",
            "DATA_ISOLATION_VIOLATION")
    {
        TenantId = tenantId;
    }

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
    public TenantDatabaseException(string message)
        : base(message, "TENANT_DB_ERROR") { }

    public TenantDatabaseException(Guid tenantId, string message, Exception innerException)
        : base($"Database error for tenant {tenantId}: {message}", innerException)
    {
        ErrorCode = "TENANT_DB_ERROR";
    }
}
