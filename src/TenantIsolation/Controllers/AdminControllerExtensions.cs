#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TenantIsolation.Formatters;

namespace TenantIsolation.Controllers;

/// <summary>
/// Extension methods for AdminController providing additional convenience methods
/// </summary>
public static class AdminControllerExtensions
{
    /// <summary>
    /// Bulk suspend multiple tenants with a single reason
    /// </summary>
    /// <param name="controller">The admin controller instance</param>
    /// <param name="tenantIds">List of tenant IDs to suspend</param>
    /// <param name="reason">Suspension reason</param>
    /// <returns>Action result with success/failure information</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> or <paramref name="tenantIds"/> is null</exception>
    public static async Task<ActionResult<ApiResponse<object>>> BulkSuspendTenants(
        this AdminController controller,
        IEnumerable<Guid> tenantIds,
        string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(tenantIds);

        if (!tenantIds.Any())
        {
            return controller.BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "No tenant IDs provided"
            });
        }

        var failedTenants = new List<Guid>();
        var totalCount = tenantIds.Count();

        foreach (var tenantId in tenantIds)
        {
            try
            {
                var result = await controller.SuspendTenant(tenantId, new AdminController.SuspensionRequest { Reason = reason });
                if (result.Value is BadRequestObjectResult)
                {
                    failedTenants.Add(tenantId);
                }
            }
            catch
            {
                failedTenants.Add(tenantId);
            }
        }

        var successCount = totalCount - failedTenants.Count;
        var message = failedTenants.Count == 0
            ? $"Successfully suspended {successCount} tenants"
            : $"Suspended {successCount} tenants, failed for {failedTenants.Count} tenants";

        return controller.Ok(new ApiResponse<object>
        {
            Success = true,
            Message = message,
            Data = new
            {
                SuccessCount = successCount,
                FailedCount = failedTenants.Count,
                FailedTenants = failedTenants,
                TotalProcessed = totalCount
            }
        });
    }

    /// <summary>
    /// Bulk activate multiple tenants
    /// </summary>
    /// <param name="controller">The admin controller instance</param>
    /// <param name="tenantIds">List of tenant IDs to activate</param>
    /// <returns>Action result with success/failure information</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> or <paramref name="tenantIds"/> is null</exception>
    public static async Task<ActionResult<ApiResponse<object>>> BulkActivateTenants(
        this AdminController controller,
        IEnumerable<Guid> tenantIds)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(tenantIds);

        if (!tenantIds.Any())
        {
            return controller.BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "No tenant IDs provided"
            });
        }

        var failedTenants = new List<Guid>();
        var totalCount = tenantIds.Count();

        foreach (var tenantId in tenantIds)
        {
            try
            {
                var result = await controller.ActivateTenant(tenantId);
                if (result.Value is BadRequestObjectResult)
                {
                    failedTenants.Add(tenantId);
                }
            }
            catch
            {
                failedTenants.Add(tenantId);
            }
        }

        var successCount = totalCount - failedTenants.Count;
        var message = failedTenants.Count == 0
            ? $"Successfully activated {successCount} tenants"
            : $"Activated {successCount} tenants, failed for {failedTenants.Count} tenants";

        return controller.Ok(new ApiResponse<object>
        {
            Success = true,
            Message = message,
            Data = new
            {
                SuccessCount = successCount,
                FailedCount = failedTenants.Count,
                FailedTenants = failedTenants,
                TotalProcessed = totalCount
            }
        });
    }

    /// <summary>
    /// Get tenants by specific status filter
    /// </summary>
    /// <param name="controller">The admin controller instance</param>
    /// <param name="status">Tenant status to filter by</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated response of tenants matching the status</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="status"/> is invalid</exception>
    public static async Task<ActionResult<ApiResponse<PaginatedResponse<object>>>> GetTenantsByStatus(
        this AdminController controller,
        string status,
        int page = 1,
        int pageSize = 20)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentException.ThrowIfNullOrEmpty(status);

        try
        {
            var tenants = status.ToLowerInvariant() switch
            {
                "active" => await controller.GetAllTenants(page, pageSize, "active"),
                "suspended" => await controller.GetAllTenants(page, pageSize, "suspended"),
                "provisioning" => await controller.GetAllTenants(page, pageSize, "provisioning"),
                "inactive" => await controller.GetAllTenants(page, pageSize, "inactive"),
                _ => throw new ArgumentException("Invalid status filter")
            };

            return controller.Ok(tenants.Value);
        }
        catch (Exception ex)
        {
            return controller.BadRequest(new ApiResponse<PaginatedResponse<object>>
            {
                Success = false,
                Message = $"Failed to retrieve tenants: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Enqueue cleanup task for specific tenant(s)
    /// </summary>
    /// <param name="controller">The admin controller instance</param>
    /// <param name="taskName">Name of the cleanup task</param>
    /// <param name="tenantIds">Optional list of tenant IDs to associate with the task</param>
    /// <param name="priority">Task priority (1-5)</param>
    /// <returns>Action result with task information</returns>
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> or <paramref name="taskName"/> is null</exception>
    public static ActionResult<ApiResponse<object>> EnqueueCleanupTask(
        this AdminController controller,
        string taskName,
        IEnumerable<Guid>? tenantIds = null,
        int priority = 3)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentException.ThrowIfNullOrEmpty(taskName);

        try
        {
            var taskRequest = new AdminController.TaskRequest
            {
                TaskName = taskName,
                Priority = priority
            };

            var result = controller.EnqueueTask(taskRequest);

            return controller.Ok(result.Value);
        }
        catch (Exception ex)
        {
            return controller.BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            });
        }
    }
}