// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TenantIsolation.Services;

/// <summary>
/// Audit log entry for tracking important system events and user actions
/// Used for compliance, security monitoring, and troubleshooting
/// </summary>
public class AuditLogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public AuditAction ActionType { get; set; }
    public string? Details { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? IpAddress { get; set; }
    public Dictionary<string, object> ChangeSet { get; set; } = new();
}

/// <summary>
/// Audit action types
/// </summary>
public enum AuditAction
{
    Create,
    Read,
    Update,
    Delete,
    Activate,
    Suspend,
    Export,
    Import,
    Login,
    Logout,
    ConfigChange,
    PermissionChange
}

/// <summary>
/// Audit logger interface
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Log audit event
    /// </summary>
    Task LogAsync(AuditLogEntry entry);

    /// <summary>
    /// Get audit logs for tenant
    /// </summary>
    Task<IEnumerable<AuditLogEntry>> GetLogsAsync(Guid tenantId, int limit = 100);

    /// <summary>
    /// Get audit logs for specific user
    /// </summary>
    Task<IEnumerable<AuditLogEntry>> GetUserLogsAsync(string userId, int limit = 100);

    /// <summary>
    /// Get audit logs for resource
    /// </summary>
    Task<IEnumerable<AuditLogEntry>> GetResourceLogsAsync(Guid tenantId, string resource, string resourceId, int limit = 100);

    /// <summary>
    /// Clear old logs (retention policy)
    /// </summary>
    Task ClearOldLogsAsync(int retentionDays = 90);
}

/// <summary>
/// Audit logger implementation
/// Stores logs in memory (should be replaced with persistent storage in production)
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly ConcurrentDictionary<string, AuditLogEntry> _logs;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logs = new ConcurrentDictionary<string, AuditLogEntry>();
        _logger = logger;
    }

    public async Task LogAsync(AuditLogEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        if (!_logs.TryAdd(entry.Id, entry))
            throw new InvalidOperationException("Failed to log audit entry");

        _logger.LogInformation(
            "[AUDIT] {Action} {Resource} {ResourceId} by {UserId} for tenant {TenantId}. Success: {Success}",
            entry.Action, entry.Resource, entry.ResourceId, entry.UserId, entry.TenantId, entry.Success);

        await Task.CompletedTask;
    }

    public async Task<IEnumerable<AuditLogEntry>> GetLogsAsync(Guid tenantId, int limit = 100)
    {
        var logs = _logs.Values
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToList();

        return await Task.FromResult(logs);
    }

    public async Task<IEnumerable<AuditLogEntry>> GetUserLogsAsync(string userId, int limit = 100)
    {
        var logs = _logs.Values
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToList();

        return await Task.FromResult(logs);
    }

    public async Task<IEnumerable<AuditLogEntry>> GetResourceLogsAsync(
        Guid tenantId,
        string resource,
        string resourceId,
        int limit = 100)
    {
        var logs = _logs.Values
            .Where(l => l.TenantId == tenantId &&
                       l.Resource == resource &&
                       l.ResourceId == resourceId)
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToList();

        return await Task.FromResult(logs);
    }

    public async Task ClearOldLogsAsync(int retentionDays = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var oldLogs = _logs.Values
            .Where(l => l.Timestamp < cutoffDate)
            .Select(l => l.Id)
            .ToList();

        var removedCount = 0;
        foreach (var logId in oldLogs)
        {
            if (_logs.TryRemove(logId, out _))
                removedCount++;
        }

        _logger.LogInformation("Cleared {Count} old audit logs", removedCount);
        await Task.CompletedTask;
    }
}

/// <summary>
/// Helper extensions for audit logging
/// </summary>
public static class AuditLoggerExtensions
{
    /// <summary>
    /// Log create action
    /// </summary>
    public static async Task LogCreateAsync(
        this IAuditLogger logger,
        Guid tenantId,
        string? userId,
        string resource,
        string resourceId,
        Dictionary<string, object>? changeSet = null)
    {
        var entry = new AuditLogEntry
        {
            TenantId = tenantId,
            UserId = userId,
            Action = $"Create {resource}",
            Resource = resource,
            ResourceId = resourceId,
            ActionType = AuditAction.Create,
            Success = true,
            ChangeSet = changeSet ?? new Dictionary<string, object>()
        };

        await logger.LogAsync(entry);
    }

    /// <summary>
    /// Log update action
    /// </summary>
    public static async Task LogUpdateAsync(
        this IAuditLogger logger,
        Guid tenantId,
        string? userId,
        string resource,
        string resourceId,
        Dictionary<string, object>? changeSet = null)
    {
        var entry = new AuditLogEntry
        {
            TenantId = tenantId,
            UserId = userId,
            Action = $"Update {resource}",
            Resource = resource,
            ResourceId = resourceId,
            ActionType = AuditAction.Update,
            Success = true,
            ChangeSet = changeSet ?? new Dictionary<string, object>()
        };

        await logger.LogAsync(entry);
    }

    /// <summary>
    /// Log delete action
    /// </summary>
    public static async Task LogDeleteAsync(
        this IAuditLogger logger,
        Guid tenantId,
        string? userId,
        string resource,
        string resourceId)
    {
        var entry = new AuditLogEntry
        {
            TenantId = tenantId,
            UserId = userId,
            Action = $"Delete {resource}",
            Resource = resource,
            ResourceId = resourceId,
            ActionType = AuditAction.Delete,
            Success = true
        };

        await logger.LogAsync(entry);
    }

    /// <summary>
    /// Log failed action
    /// </summary>
    public static async Task LogFailureAsync(
        this IAuditLogger logger,
        Guid tenantId,
        string? userId,
        string action,
        string resource,
        string? errorMessage = null)
    {
        var entry = new AuditLogEntry
        {
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            Resource = resource,
            Success = false,
            ErrorMessage = errorMessage
        };

        await logger.LogAsync(entry);
    }
}

/// <summary>
/// Extension method to register audit logger
/// </summary>
public static class AuditLoggerServiceExtensions
{
    public static IServiceCollection AddAuditLogger(this IServiceCollection services)
    {
        services.AddSingleton<IAuditLogger, AuditLogger>();
        return services;
    }
}
