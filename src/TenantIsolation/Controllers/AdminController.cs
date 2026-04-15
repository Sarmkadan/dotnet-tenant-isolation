#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TenantIsolation.BackgroundTasks;
using TenantIsolation.Formatters;
using TenantIsolation.Services;

namespace TenantIsolation.Controllers;

/// <summary>
/// Administrative endpoints for tenant management and system operations
/// Requires administrative authorization in production
/// </summary>
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly TenantService _tenantService;
    private readonly IResponseFormatter _formatter;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        TenantService tenantService,
        IResponseFormatter formatter,
        IBackgroundTaskQueue taskQueue,
        ILogger<AdminController> logger)
    {
        _tenantService = tenantService;
        _formatter = formatter;
        _taskQueue = taskQueue;
        _logger = logger;
    }

    /// <summary>
    /// Get system statistics and tenant overview
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<object>>> GetStatistics()
    {
        try
        {
            var stats = await _tenantService.GetTenantStatisticsAsync();
            return Ok(_formatter.Success(stats, "Statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system statistics");
            return BadRequest(_formatter.Error("Failed to retrieve statistics"));
        }
    }

    /// <summary>
    /// Get list of all tenants with optional filters
    /// </summary>
    [HttpGet("tenants")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<object>>>> GetAllTenants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        try
        {
            var allTenants = status switch
            {
                "active" => await _tenantService.GetActiveTenantsAsync(),
                "provisioning" => await _tenantService.GetTenantsByStatusAsync(TenantStatus.Provisioning),
                "suspended" => await _tenantService.GetTenantsByStatusAsync(TenantStatus.Suspended),
                _ => await _tenantService.GetActiveTenantsAsync()
            };

            var paginatedTenants = allTenants
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = _formatter.Paginated(
                paginatedTenants.Cast<object>().ToList(),
                allTenants.Count,
                page,
                pageSize,
                "Tenants retrieved successfully"
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenants");
            return BadRequest(_formatter.Error("Failed to retrieve tenants"));
        }
    }

    /// <summary>
    /// Suspend a tenant (administrative action)
    /// </summary>
    [HttpPost("tenants/{tenantId}/suspend")]
    public async Task<ActionResult<ApiResponse<object>>> SuspendTenant(
        Guid tenantId,
        [FromBody] SuspensionRequest request)
    {
        try
        {
            var result = await _tenantService.SuspendTenantAsync(tenantId, request.Reason);
            if (result)
            {
                _logger.LogWarning("Tenant {TenantId} suspended administratively. Reason: {Reason}",
                    tenantId, request.Reason ?? "Not specified");
                return Ok(_formatter.Success("Tenant suspended successfully"));
            }

            return BadRequest(_formatter.Error("Failed to suspend tenant"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending tenant {TenantId}", tenantId);
            return BadRequest(_formatter.Error($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Activate a tenant (restore from suspended state)
    /// </summary>
    [HttpPost("tenants/{tenantId}/activate")]
    public async Task<ActionResult<ApiResponse<object>>> ActivateTenant(Guid tenantId)
    {
        try
        {
            var result = await _tenantService.ActivateTenantAsync(tenantId);
            if (result)
            {
                _logger.LogInformation("Tenant {TenantId} activated administratively", tenantId);
                return Ok(_formatter.Success("Tenant activated successfully"));
            }

            return BadRequest(_formatter.Error("Failed to activate tenant"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating tenant {TenantId}", tenantId);
            return BadRequest(_formatter.Error($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get background task queue statistics
    /// </summary>
    [HttpGet("queue-statistics")]
    public ActionResult<ApiResponse<QueueStatistics>> GetQueueStatistics()
    {
        try
        {
            var stats = _taskQueue.GetStatistics();
            return Ok(_formatter.Success(stats, "Queue statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue statistics");
            return BadRequest(_formatter.Error("Failed to retrieve queue statistics"));
        }
    }

    /// <summary>
    /// Enqueue a background task for cleanup
    /// </summary>
    [HttpPost("queue-task")]
    public ActionResult<ApiResponse<object>> EnqueueTask([FromBody] TaskRequest request)
    {
        try
        {
            var task = new BackgroundTask
            {
                Name = request.TaskName,
                Priority = (BackgroundTaskPriority)request.Priority,
                WorkItem = async (ct) =>
                {
                    _logger.LogInformation("Executing manual task: {TaskName}", request.TaskName);
                    await Task.Delay(100, ct); // Simulate work
                }
            };

            _taskQueue.QueueTask(task);

            return Ok(_formatter.Success(new { taskId = task.Id }, "Task queued successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing task");
            return BadRequest(_formatter.Error($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get expiring subscriptions report
    /// </summary>
    [HttpGet("subscriptions/expiring")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetExpiringSubscriptions(
        [FromQuery] int daysUntilExpiry = 30)
    {
        try
        {
            var expiringTenants = await _tenantService.GetExpiringSubscriptionsAsync(daysUntilExpiry);
            return Ok(_formatter.Success(
                expiringTenants.Cast<object>().ToList(),
                $"Found {expiringTenants.Count} tenants with expiring subscriptions"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring subscriptions");
            return BadRequest(_formatter.Error("Failed to retrieve subscriptions"));
        }
    }

    /// <summary>
    /// Request model for suspension
    /// </summary>
    public class SuspensionRequest
    {
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Request model for task enqueueing
    /// </summary>
    public class TaskRequest
    {
        public string TaskName { get; set; } = string.Empty;
        public int Priority { get; set; } = 1;
    }
}
