// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TenantIsolation.Data;

namespace TenantIsolation.Services;

/// <summary>
/// Health check status
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

/// <summary>
/// Component health information
/// </summary>
public class ComponentHealthInfo
{
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; } = HealthStatus.Healthy;
    public string Message { get; set; } = string.Empty;
    public long ResponseTimeMs { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Overall system health report
/// </summary>
public class HealthReport
{
    public HealthStatus Status { get; set; } = HealthStatus.Healthy;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, ComponentHealthInfo> Components { get; set; } = new();
    public TimeSpan TotalCheckDuration { get; set; }

    /// <summary>
    /// Get overall message describing system health
    /// </summary>
    public string GetMessage()
    {
        return Status switch
        {
            HealthStatus.Healthy => "All systems operational",
            HealthStatus.Degraded => $"{Components.Values.Count(c => c.Status != HealthStatus.Healthy)} component(s) degraded",
            HealthStatus.Unhealthy => "System is unavailable",
            _ => "Unknown status"
        };
    }
}

/// <summary>
/// Health check service interface
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Perform comprehensive health check
    /// </summary>
    Task<HealthReport> PerformHealthCheckAsync();

    /// <summary>
    /// Check specific component health
    /// </summary>
    Task<ComponentHealthInfo> CheckComponentAsync(string componentName);

    /// <summary>
    /// Get cached health report
    /// </summary>
    HealthReport? GetCachedHealthReport();
}

/// <summary>
/// Health check service implementation
/// Monitors database, cache, and event bus health
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly TenantDbContext _dbContext;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly ConcurrentDictionary<string, ComponentHealthInfo> _componentCache;

    private HealthReport? _cachedReport;
    private DateTime _lastCheckTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromSeconds(30);

    public HealthCheckService(TenantDbContext dbContext, ILogger<HealthCheckService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _componentCache = new ConcurrentDictionary<string, ComponentHealthInfo>();
    }

    public async Task<HealthReport> PerformHealthCheckAsync()
    {
        // Return cached report if still valid
        if (_cachedReport != null && DateTime.UtcNow - _lastCheckTime < _cacheExpiry)
        {
            _logger.LogDebug("Returning cached health report");
            return _cachedReport;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var components = new Dictionary<string, ComponentHealthInfo>();

        // Check database
        var dbHealth = await CheckComponentAsync("database");
        components["database"] = dbHealth;

        // Check cache (simulated)
        var cacheHealth = new ComponentHealthInfo
        {
            Name = "cache",
            Status = HealthStatus.Healthy,
            Message = "In-memory cache operational",
            ResponseTimeMs = 2,
            CheckedAt = DateTime.UtcNow
        };
        components["cache"] = cacheHealth;

        // Check event bus (simulated)
        var eventBusHealth = new ComponentHealthInfo
        {
            Name = "eventbus",
            Status = HealthStatus.Healthy,
            Message = "Event bus operational",
            ResponseTimeMs = 1,
            CheckedAt = DateTime.UtcNow
        };
        components["eventbus"] = eventBusHealth;

        stopwatch.Stop();

        // Determine overall health
        var unhealthyCount = components.Values.Count(c => c.Status == HealthStatus.Unhealthy);
        var degradedCount = components.Values.Count(c => c.Status == HealthStatus.Degraded);

        var overallStatus = unhealthyCount > 0
            ? HealthStatus.Unhealthy
            : degradedCount > 0
            ? HealthStatus.Degraded
            : HealthStatus.Healthy;

        _cachedReport = new HealthReport
        {
            Status = overallStatus,
            Components = components,
            TotalCheckDuration = stopwatch.Elapsed,
            CheckedAt = DateTime.UtcNow
        };

        _lastCheckTime = DateTime.UtcNow;

        _logger.LogInformation("Health check completed. Status: {Status}. Duration: {Duration}ms",
            overallStatus, stopwatch.ElapsedMilliseconds);

        return _cachedReport;
    }

    public async Task<ComponentHealthInfo> CheckComponentAsync(string componentName)
    {
        return componentName.ToLowerInvariant() switch
        {
            "database" => await CheckDatabaseAsync(),
            "cache" => CheckCache(),
            "eventbus" => CheckEventBus(),
            _ => new ComponentHealthInfo
            {
                Name = componentName,
                Status = HealthStatus.Unhealthy,
                Message = "Unknown component"
            }
        };
    }

    public HealthReport? GetCachedHealthReport()
    {
        return _cachedReport;
    }

    /// <summary>
    /// Check database connectivity
    /// </summary>
    private async Task<ComponentHealthInfo> CheckDatabaseAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Try to execute a simple query
            var canConnect = await _dbContext.Database.CanConnectAsync();
            stopwatch.Stop();

            if (!canConnect)
            {
                _logger.LogWarning("Database connection check failed");
                return new ComponentHealthInfo
                {
                    Name = "database",
                    Status = HealthStatus.Unhealthy,
                    Message = "Cannot connect to database",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            // Check if we can query
            var tenantCount = await _dbContext.Tenants.CountAsync();

            return new ComponentHealthInfo
            {
                Name = "database",
                Status = stopwatch.ElapsedMilliseconds > 1000 ? HealthStatus.Degraded : HealthStatus.Healthy,
                Message = $"Database operational. {tenantCount} tenants found.",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database health check failed");

            return new ComponentHealthInfo
            {
                Name = "database",
                Status = HealthStatus.Unhealthy,
                Message = $"Database check failed: {ex.Message}",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// Check in-memory cache
    /// </summary>
    private ComponentHealthInfo CheckCache()
    {
        try
        {
            // In a real scenario, you'd test cache operations
            return new ComponentHealthInfo
            {
                Name = "cache",
                Status = HealthStatus.Healthy,
                Message = "In-memory cache operational",
                ResponseTimeMs = 2
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache health check failed");
            return new ComponentHealthInfo
            {
                Name = "cache",
                Status = HealthStatus.Unhealthy,
                Message = $"Cache check failed: {ex.Message}",
                ResponseTimeMs = 0
            };
        }
    }

    /// <summary>
    /// Check event bus
    /// </summary>
    private ComponentHealthInfo CheckEventBus()
    {
        try
        {
            // In a real scenario, you'd test event publishing/subscribing
            return new ComponentHealthInfo
            {
                Name = "eventbus",
                Status = HealthStatus.Healthy,
                Message = "Event bus operational",
                ResponseTimeMs = 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event bus health check failed");
            return new ComponentHealthInfo
            {
                Name = "eventbus",
                Status = HealthStatus.Unhealthy,
                Message = $"Event bus check failed: {ex.Message}",
                ResponseTimeMs = 0
            };
        }
    }
}

/// <summary>
/// Extension method to register health check service
/// </summary>
public static class HealthCheckServiceExtensions
{
    public static IServiceCollection AddHealthCheckService(this IServiceCollection services)
    {
        services.AddScoped<IHealthCheckService, HealthCheckService>();
        return services;
    }
}
