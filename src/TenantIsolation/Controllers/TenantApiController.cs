#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.AspNetCore.Mvc;
using TenantIsolation.Models;
using TenantIsolation.Services;

namespace TenantIsolation.Controllers;

/// <summary>
/// API endpoints for tenant management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TenantApiController : ControllerBase
{
    private readonly TenantService _tenantService;
    private readonly ITenantResolutionService _resolutionService;
    private readonly ITenantUsageMeteringService _usageMeteringService;
    private readonly ILogger<TenantApiController> _logger;

    public TenantApiController(
        TenantService tenantService,
        ITenantResolutionService resolutionService,
        ITenantUsageMeteringService usageMeteringService,
        ILogger<TenantApiController> logger)
    {
        _tenantService = tenantService;
        _resolutionService = resolutionService;
        _usageMeteringService = usageMeteringService;
        _logger = logger;
    }

    /// <summary>
    /// Create new tenant
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        // Add input validation for request fields
        if (request == null || string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
        {
            return BadRequest(new { error = "Invalid tenant name" });
        }

        if (string.IsNullOrWhiteSpace(request.Slug) || request.Slug.Length > 100)
        {
            return BadRequest(new { error = "Invalid tenant slug" });
        }

        if (string.IsNullOrWhiteSpace(request.AdminEmail) || request.AdminEmail.Length > 254)
        {
            return BadRequest(new { error = "Invalid admin email" });
        }

        try
        {
            var tenant = await _tenantService.CreateTenantAsync(
                request.Name,
                request.Slug,
                request.AdminEmail);

            _logger.LogInformation("Created tenant {TenantId}", tenant.Id);

            return CreatedAtAction(nameof(GetTenantById), new { id = tenant.Id }, tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTenantById(Guid id)
    {
        try
        {
            var tenant = await _tenantService.GetTenantAsync(id);
            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant {TenantId}", id);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get tenant by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetTenantBySlug(string slug)
    {
        // Add input validation for slug length
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 100)
        {
            return BadRequest(new { error = "Invalid slug parameter" });
        }

        try
        {
            var tenant = await _tenantService.GetTenantBySlugAsync(slug);
            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant by slug {Slug}", slug);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get active tenants
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveTenants()
    {
        try
        {
            var tenants = await _tenantService.GetActiveTenantsAsync();
            return Ok(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active tenants");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Activate tenant
    /// </summary>
    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> ActivateTenant(Guid id)
    {
        try
        {
            var result = await _tenantService.ActivateTenantAsync(id);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating tenant {TenantId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Suspend tenant
    /// </summary>
    [HttpPut("{id:guid}/suspend")]
    public async Task<IActionResult> SuspendTenant(Guid id, [FromBody] SuspendTenantRequest? request = null)
    {
        try
        {
            var result = await _tenantService.SuspendTenantAsync(id, request?.Reason);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending tenant {TenantId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete tenant (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        try
        {
            var result = await _tenantService.DeleteTenantAsync(id);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant {TenantId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get current tenant
    /// </summary>
    [HttpGet("current")]
    public IActionResult GetCurrentTenant()
    {
        try
        {
            var tenant = _resolutionService.GetCurrentTenant();
            if (tenant == null)
                return NotFound(new { error = "No tenant in context" });

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current tenant");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get tenant statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var stats = await _tenantService.GetTenantStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant statistics");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search tenants
    /// </summary>
    [HttpGet("search/{query}")]
    public async Task<IActionResult> SearchTenants(string query)
    {
        try
        {
            var tenants = await _tenantService.SearchTenantsAsync(query);
            return Ok(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching tenants");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get quota status for a tenant
    /// </summary>
    [HttpGet("{id:guid}/quota")]
    public async Task<IActionResult> GetTenantQuota(Guid id, [FromQuery] string metric)
    {
        // Add input validation for metric
        if (string.IsNullOrWhiteSpace(metric) || metric.Length > 100)
        {
            return BadRequest(new { error = "Invalid metric parameter" });
        }

        try
        {
            var isWithinQuota = await _usageMeteringService.IsWithinQuotaAsync(id, metric);

            // Get quota limit from configuration
            var quotaCheckResult = await _usageMeteringService.CheckQuotaAsync(id, metric);

            var response = new TenantQuotaStatusResponse
            {
                TenantId = id,
                Metric = metric,
                IsWithinQuota = isWithinQuota,
                CurrentUsage = quotaCheckResult.CurrentUsage,
                QuotaLimit = quotaCheckResult.QuotaLimit,
                IsAllowed = quotaCheckResult.IsAllowed,
                IsExceeded = quotaCheckResult.IsExceeded,
                UsagePercentage = quotaCheckResult.UsagePercentage
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quota status for tenant {TenantId} metric {Metric}", id, metric);
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Response model for tenant quota status
/// </summary>
public class TenantQuotaStatusResponse
{
    public Guid TenantId { get; set; }
    public string Metric { get; set; } = null!;
    public bool IsWithinQuota { get; set; }
    public long CurrentUsage { get; set; }
    public long? QuotaLimit { get; set; }
    public bool IsAllowed { get; set; }
    public bool IsExceeded { get; set; }
    public double UsagePercentage { get; set; }
}

/// <summary>
/// Request to create new tenant
/// </summary>
public class CreateTenantRequest
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string AdminEmail { get; set; } = null!;
}

/// <summary>
/// Request to suspend tenant
/// </summary>
public class SuspendTenantRequest
{
    public string? Reason { get; set; }
}