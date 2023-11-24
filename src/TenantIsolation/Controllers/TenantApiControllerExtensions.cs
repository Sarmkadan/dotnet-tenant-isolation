#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

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
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null</exception>
    /// <exception cref="ArgumentNullException"><paramref name="ids"/> is null</exception>
    public static async Task<IActionResult> GetTenantsByIds(
        this TenantApiController controller,
        [FromBody] IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(ids);

        if (!ids.Any())
        {
            return new BadRequestObjectResult(new { error = "At least one tenant ID is required" });
        }

        try
        {
            var tenants = new List<Tenant>();
            foreach (var id in ids)
            {
                var tenantResult = await controller.GetTenantById(id);
                if (tenantResult is OkObjectResult okResult && okResult.Value is Tenant tenant)
                {
                    tenants.Add(tenant);
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
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null</exception>
    public static async Task<IActionResult> GetTenantsByStatus(
        this TenantApiController controller,
        bool includeInactive = false)
    {
        ArgumentNullException.ThrowIfNull(controller);

        try
        {
            if (includeInactive)
            {
                // For simplicity, return all tenants when includeInactive is true
                // In a real implementation, this would call a service method
                var allTenantsResult = await controller.GetActiveTenants();
                if (allTenantsResult is OkObjectResult okResult && okResult.Value is List<Tenant> allTenants)
                {
                    return new OkObjectResult(allTenants);
                }
            }

            var activeTenantsResult = await controller.GetActiveTenants();
            return activeTenantsResult;
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
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null</exception>
    /// <exception cref="ArgumentNullException"><paramref name="ids"/> is null</exception>
    public static async Task<IActionResult> BulkActivateTenants(
        this TenantApiController controller,
        [FromBody] IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(ids);

        if (!ids.Any())
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

        return new OkObjectResult(new
        {
            Total = ids.Count(),
            SuccessCount = results.Count(r => r is { } obj && (bool)obj.GetType().GetProperty("Success")?.GetValue(obj)!),
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
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null</exception>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null</exception>
    public static async Task<IActionResult> BulkSuspendTenants(
        this TenantApiController controller,
        [FromBody] BulkTenantOperationRequest request)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(request);

        if (request.TenantIds == null || !request.TenantIds.Any())
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

        return new OkObjectResult(new
        {
            Total = request.TenantIds.Count,
            SuccessCount = results.Count(r => r is { } obj && (bool)obj.GetType().GetProperty("Success")?.GetValue(obj)!),
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
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is invalid</exception>
    public static async Task<IActionResult> GetTenantByName(
        this TenantApiController controller,
        string name)
    {
        ArgumentNullException.ThrowIfNull(controller);

        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            return new BadRequestObjectResult(new { error = "Invalid tenant name parameter" });
        }

        try
        {
            var tenantsResult = await controller.SearchTenants(name);
            if (tenantsResult is OkObjectResult okResult && okResult.Value is List<Tenant> tenantList)
            {
                var exactMatch = tenantList.FirstOrDefault(t =>
                    string.Equals(t?.Name, name, StringComparison.OrdinalIgnoreCase));
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
    /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null</exception>
    public static async Task<IActionResult> GetDashboardStatistics(
        this TenantApiController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        try
        {
            var statsResult = await controller.GetStatistics();
            if (statsResult is OkObjectResult okResult && okResult.Value is TenantStatistics stats)
            {
                return new OkObjectResult(new
                {
                    TotalTenants = stats.TotalTenants,
                    ActiveTenants = stats.ActiveTenants,
                    InactiveTenants = stats.TotalTenants - stats.ActiveTenants,
                    Status = new
                    {
                        Active = stats.ActiveTenants,
                        Suspended = stats.SuspendedTenants,
                        Deleted = stats.DeletedTenants
                    }
                });
            }

            return new BadRequestObjectResult(new { error = "Failed to retrieve statistics" });
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request for bulk tenant operations
/// </summary>
public sealed class BulkTenantOperationRequest
{
    /// <summary>
    /// Gets or sets the collection of tenant IDs to operate on
    /// </summary>
    public List<Guid> TenantIds { get; set; } = new();

    /// <summary>
    /// Gets or sets an optional reason for the operation
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Statistics for tenant dashboard
/// </summary>
/// <param name="TotalTenants">Total number of tenants</param>
/// <param name="ActiveTenants">Number of active tenants</param>
/// <param name="SuspendedTenants">Number of suspended tenants</param>
/// <param name="DeletedTenants">Number of deleted tenants</param>
public sealed class TenantStatistics
{
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int SuspendedTenants { get; set; }
    public int DeletedTenants { get; set; }
}