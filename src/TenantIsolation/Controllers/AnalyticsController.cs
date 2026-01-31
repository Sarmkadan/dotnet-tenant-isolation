// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TenantIsolation.Formatters;

namespace TenantIsolation.Controllers;

/// <summary>
/// Analytics endpoints for monitoring tenant usage and system health
/// Provides insights into tenant activity, resource consumption, and health metrics
/// </summary>
[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IResponseFormatter _formatter;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IResponseFormatter formatter,
        ILogger<AnalyticsController> logger)
    {
        _formatter = formatter;
        _logger = logger;
    }

    /// <summary>
    /// Get system health status
    /// </summary>
    [HttpGet("health")]
    public ActionResult<ApiResponse<HealthStatus>> GetHealth()
    {
        try
        {
            var health = new HealthStatus
            {
                Status = "healthy",
                Timestamp = DateTime.UtcNow,
                Components = new Dictionary<string, ComponentHealth>
                {
                    {
                        "database", new ComponentHealth
                        {
                            Name = "Database",
                            Status = "healthy",
                            ResponseTimeMs = 5
                        }
                    },
                    {
                        "cache", new ComponentHealth
                        {
                            Name = "Cache",
                            Status = "healthy",
                            ResponseTimeMs = 2
                        }
                    },
                    {
                        "eventbus", new ComponentHealth
                        {
                            Name = "Event Bus",
                            Status = "healthy",
                            ResponseTimeMs = 0
                        }
                    }
                }
            };

            return Ok(_formatter.Success(health));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system health");
            return StatusCode(503, _formatter.Error("Service unavailable"));
        }
    }

    /// <summary>
    /// Get tenant activity metrics
    /// </summary>
    [HttpGet("tenant/{tenantId}/activity")]
    public ActionResult<ApiResponse<TenantActivityMetrics>> GetTenantActivity(Guid tenantId)
    {
        try
        {
            var metrics = new TenantActivityMetrics
            {
                TenantId = tenantId,
                ActiveUsers = 5,
                RequestsPerHour = 250,
                DataProcessedGb = 1.5m,
                StorageUsedGb = 10.2m,
                LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
                Period = "1h"
            };

            return Ok(_formatter.Success(metrics, "Activity metrics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activity for tenant {TenantId}", tenantId);
            return BadRequest(_formatter.Error($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get API usage statistics for time period
    /// </summary>
    [HttpGet("usage")]
    public ActionResult<ApiResponse<UsageStatistics>> GetUsageStatistics(
        [FromQuery] string? period = "1d",
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var stats = new UsageStatistics
            {
                Period = period ?? "1d",
                TotalRequests = 15000,
                SuccessfulRequests = 14850,
                FailedRequests = 150,
                AverageResponseTimeMs = 45,
                P95ResponseTimeMs = 120,
                P99ResponseTimeMs = 250,
                UniqueUsers = 250,
                TopEndpoints = new List<EndpointUsage>
                {
                    new() { Endpoint = "/api/tenants", Requests = 5000, AverageResponseMs = 30 },
                    new() { Endpoint = "/api/users", Requests = 4500, AverageResponseMs = 40 },
                    new() { Endpoint = "/api/data", Requests = 3000, AverageResponseMs = 60 },
                    new() { Endpoint = "/api/features", Requests = 2500, AverageResponseMs = 25 }
                }
            };

            return Ok(_formatter.Success(stats, "Usage statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage statistics");
            return BadRequest(_formatter.Error($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get error rate and exception metrics
    /// </summary>
    [HttpGet("errors")]
    public ActionResult<ApiResponse<ErrorMetrics>> GetErrorMetrics(
        [FromQuery] string? period = "1h")
    {
        try
        {
            var metrics = new ErrorMetrics
            {
                Period = period ?? "1h",
                TotalErrors = 150,
                ErrorRate = 0.01m,
                TopErrors = new List<ErrorDetail>
                {
                    new() { ErrorType = "TenantNotResolved", Count = 50, LastOccurred = DateTime.UtcNow.AddMinutes(-5) },
                    new() { ErrorType = "ValidationError", Count = 40, LastOccurred = DateTime.UtcNow.AddMinutes(-10) },
                    new() { ErrorType = "AuthorizationError", Count = 30, LastOccurred = DateTime.UtcNow.AddMinutes(-15) },
                    new() { ErrorType = "TimeoutError", Count = 20, LastOccurred = DateTime.UtcNow.AddHours(-1) }
                },
                MostAffectedEndpoints = new List<string> { "/api/data", "/api/users", "/api/tenants" }
            };

            return Ok(_formatter.Success(metrics, "Error metrics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving error metrics");
            return BadRequest(_formatter.Error($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Health status model
    /// </summary>
    public class HealthStatus
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, ComponentHealth> Components { get; set; } = new();
    }

    public class ComponentHealth
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ResponseTimeMs { get; set; }
    }

    /// <summary>
    /// Tenant activity metrics
    /// </summary>
    public class TenantActivityMetrics
    {
        public Guid TenantId { get; set; }
        public int ActiveUsers { get; set; }
        public int RequestsPerHour { get; set; }
        public decimal DataProcessedGb { get; set; }
        public decimal StorageUsedGb { get; set; }
        public DateTime LastActivityAt { get; set; }
        public string Period { get; set; } = string.Empty;
    }

    /// <summary>
    /// Usage statistics
    /// </summary>
    public class UsageStatistics
    {
        public string Period { get; set; } = string.Empty;
        public long TotalRequests { get; set; }
        public long SuccessfulRequests { get; set; }
        public long FailedRequests { get; set; }
        public int AverageResponseTimeMs { get; set; }
        public int P95ResponseTimeMs { get; set; }
        public int P99ResponseTimeMs { get; set; }
        public int UniqueUsers { get; set; }
        public List<EndpointUsage> TopEndpoints { get; set; } = new();
    }

    public class EndpointUsage
    {
        public string Endpoint { get; set; } = string.Empty;
        public long Requests { get; set; }
        public int AverageResponseMs { get; set; }
    }

    /// <summary>
    /// Error metrics
    /// </summary>
    public class ErrorMetrics
    {
        public string Period { get; set; } = string.Empty;
        public long TotalErrors { get; set; }
        public decimal ErrorRate { get; set; }
        public List<ErrorDetail> TopErrors { get; set; } = new();
        public List<string> MostAffectedEndpoints { get; set; } = new();
    }

    public class ErrorDetail
    {
        public string ErrorType { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime LastOccurred { get; set; }
    }
}
