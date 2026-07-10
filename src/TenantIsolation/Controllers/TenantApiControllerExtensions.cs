#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TenantIsolation.Models;

namespace TenantIsolation.Controllers;

/// <summary>
/// Extension methods for TenantApiController providing additional functionality
/// </summary>
public static class TenantApiControllerExtensions
{
    /// <summary>
    /// Gets tenants by their IDs in a single batch operation
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <param name="ids">Collection of tenant IDs to retrieve</param>
    /// <returns>Collection of tenants or empty if none found</returns>
    public static async Task<IActionResult> GetTenantsByIds(
        this TenantApiController controller,
        [FromBody] IEnumerable<Guid> ids)
    {
        if (ids == null || !ids.Any())
        {
            return new BadRequestObjectResult(new { error = "At least one tenant ID is required" });
        }

        try
        {
            var tenants = new List<object>();
            foreach (var id in ids)
            {
                var tenantResult = await controller.GetTenantById(id);
                if (tenantResult is OkObjectResult okResult && okResult.Value != null)
                {
                    tenants.Add(okResult.Value);
                }
            }

            return new OkObjectResult(tenants);
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets tenants filtered by active status
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <param name="includeInactive">Whether to include inactive tenants</param>
    /// <returns>Filtered list of tenants</returns>
    public static async Task<IActionResult> GetTenantsByStatus(
        this TenantApiController controller,
        bool includeInactive = false)
    {
        try
        {
            return await controller.GetActiveTenants();
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Bulk activate multiple tenants
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <param name="ids">Collection of tenant IDs to activate</param>
    /// <returns>Summary of activation results</returns>
    public static async Task<IActionResult> BulkActivateTenants(
        this TenantApiController controller,
        [FromBody] IEnumerable<Guid> ids)
    {
        if (ids == null || !ids.Any())
        {
            return new BadRequestObjectResult(new { error = "At least one tenant ID is required" });
        }

        var results = new List<object>();
        var errors = 0;

        foreach (var id in ids)
        {
            try
            {
                var result = await controller.ActivateTenant(id);
                if (result is OkObjectResult okResult)
                {
                    results.Add(new { TenantId = id, Success = true });
                }
                else
                {
                    errors++;
                    results.Add(new { TenantId = id, Success = false, Error = "Activation failed" });
                }
            }
            catch (Exception ex)
            {
                errors++;
                results.Add(new { TenantId = id, Success = false, Error = ex.Message });
            }
        }

        return new OkObjectResult(new {
            Total = ids.Count(),
            SuccessCount = results.Count(r => (bool)r.GetType().GetProperty("Success")?.GetValue(r)!),
            FailedCount = errors,
            Results = results
        });
    }

    /// <summary>
    /// Bulk suspend multiple tenants with optional reason
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <param name="request">Collection of tenant IDs and optional reasons</param>
    /// <returns>Summary of suspension results</returns>
    public static async Task<IActionResult> BulkSuspendTenants(
        this TenantApiController controller,
        [FromBody] BulkTenantOperationRequest request)
    {
        if (request?.TenantIds == null || !request.TenantIds.Any())
        {
            return new BadRequestObjectResult(new { error = "At least one tenant ID is required" });
        }

        var results = new List<object>();
        var errors = 0;

        foreach (var id in request.TenantIds)
        {
            try
            {
                var result = await controller.SuspendTenant(id, new SuspendTenantRequest { Reason = request.Reason });
                if (result is OkObjectResult okResult)
                {
                    results.Add(new { TenantId = id, Success = true });
                }
                else
                {
                    errors++;
                    results.Add(new { TenantId = id, Success = false, Error = "Suspension failed" });
                }
            }
            catch (Exception ex)
            {
                errors++;
                results.Add(new { TenantId = id, Success = false, Error = ex.Message });
            }
        }

        return new OkObjectResult(new {
            Total = request.TenantIds.Count(),
            SuccessCount = results.Count(r => (bool)r.GetType().GetProperty("Success")?.GetValue(r)!),
            FailedCount = errors,
            Reason = request.Reason,
            Results = results
        });
    }

    /// <summary>
    /// Gets tenant by name (exact match)
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <param name="name">Tenant name to search for</param>
    /// <returns>Matching tenant or NotFound</returns>
    public static async Task<IActionResult> GetTenantByName(
        this TenantApiController controller,
        string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            return new BadRequestObjectResult(new { error = "Invalid tenant name parameter" });
        }

        try
        {
            var tenantsResult = await controller.SearchTenants(name);
            if (tenantsResult is OkObjectResult okResult && okResult.Value is List<object> tenantList)
            {
                var exactMatch = tenantList.FirstOrDefault(t =>
                {
                    var nameProp = t?.GetType().GetProperty("Name");
                    return nameProp != null && string.Equals(nameProp.GetValue(t) as string, name, StringComparison.OrdinalIgnoreCase);
                });
                if (exactMatch != null)
                {
                    return new OkObjectResult(exactMatch);
                }
            }

            return new NotFoundObjectResult(new { error = $"Tenant with name '{name}' not found" });
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets tenant statistics summary for dashboard display
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <returns>Simplified statistics for dashboard</returns>
    public static async Task<IActionResult> GetDashboardStatistics(
        this TenantApiController controller)
    {
        try
        {
            var statsResult = await controller.GetStatistics();
            if (statsResult is OkObjectResult okResult)
            {
                var stats = okResult.Value;
                var totalTenants = GetPropertyValue(stats, "TotalTenants") as int? ?? 0;
                var activeTenants = GetPropertyValue(stats, "ActiveTenants") as int? ?? 0;
                var dashboardStats = new {
                    TotalTenants = totalTenants,
                    ActiveTenants = activeTenants,
                    InactiveTenants = totalTenants - activeTenants,
                    Status = new {
                        Active = activeTenants,
                        Suspended = GetPropertyValue(stats, "SuspendedTenants"),
                        Deleted = GetPropertyValue(stats, "DeletedTenants")
                    }
                };

                return new OkObjectResult(dashboardStats);
            }

            return new BadRequestObjectResult(new { error = "Failed to retrieve statistics" });
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new { error = ex.Message });
        }
    }

    private static object? GetPropertyValue(object? obj, string propertyName)
    {
        if (obj == null) return null;
        var prop = obj.GetType().GetProperty(propertyName);
        return prop?.GetValue(obj);
    }
}

/// <summary>
/// Request for bulk tenant operations
/// </summary>
public class BulkTenantOperationRequest
{
    public List<Guid> TenantIds { get; set; } = new();
    public string? Reason { get; set; }
}