#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using TenantIsolation.Services;

namespace TenantIsolation.Controllers;

/// <summary>
/// API endpoints for feature toggle management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FeaturesController : ControllerBase
{
    private readonly TenantFeatureService _featureService;
    private readonly ITenantResolutionService _resolutionService;
    private readonly ILogger<FeaturesController> _logger;

    public FeaturesController(
        TenantFeatureService featureService,
        ITenantResolutionService resolutionService,
        ILogger<FeaturesController> logger)
    {
        _featureService = featureService;
        _resolutionService = resolutionService;
        _logger = logger;
    }

    /// <summary>
    /// Check if feature is enabled for current tenant
    /// </summary>
    [HttpGet("{featureKey}/enabled")]
    public async Task<IActionResult> IsFeatureEnabled(string featureKey)
    {
        try
        {
            var tenant = _resolutionService.GetCurrentTenant();
            if (tenant == null)
                return BadRequest(new { error = "No tenant in context" });

            var isEnabled = await _featureService.IsFeatureEnabledAsync(tenant.Id, featureKey);
            return Ok(new { featureKey, isEnabled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature {FeatureKey}", featureKey);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get feature details
    /// </summary>
    [HttpGet("{featureKey}")]
    public async Task<IActionResult> GetFeature(string featureKey)
    {
        try
        {
            var tenant = _resolutionService.GetCurrentTenant();
            if (tenant == null)
                return BadRequest(new { error = "No tenant in context" });

            var feature = await _featureService.GetFeatureAsync(tenant.Id, featureKey);
            if (feature == null)
                return NotFound(new { error = "Feature not found" });

            return Ok(feature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature {FeatureKey}", featureKey);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all enabled features
    /// </summary>
    [HttpGet("enabled")]
    public async Task<IActionResult> GetEnabledFeatures()
    {
        try
        {
            var tenant = _resolutionService.GetCurrentTenant();
            if (tenant == null)
                return BadRequest(new { error = "No tenant in context" });

            var features = await _featureService.GetEnabledFeaturesAsync(tenant.Id);
            return Ok(features);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enabled features");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all features for tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllFeatures()
    {
        try
        {
            var tenant = _resolutionService.GetCurrentTenant();
            if (tenant == null)
                return BadRequest(new { error = "No tenant in context" });

            var features = await _featureService.GetAllFeaturesAsync(tenant.Id);
            return Ok(features);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all features");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Enable feature
    /// </summary>
    [HttpPost("{featureKey}/enable")]
    public async Task<IActionResult> EnableFeature(string featureKey)
    {
        try
        {
            var tenant = _resolutionService.GetCurrentTenant();
            if (tenant == null)
                return BadRequest(new { error = "No tenant in context" });

            var feature = await _featureService.EnableFeatureAsync(tenant.Id, featureKey);
            return Ok(feature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling feature {FeatureKey}", featureKey);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Disable feature
    /// </summary>
    [HttpPost("{featureKey}/disable")]
    public async Task<IActionResult> DisableFeature(string featureKey)
    {
        try
        {
            var tenant = _resolutionService.GetCurrentTenant();
            if (tenant == null)
                return BadRequest(new { error = "No tenant in context" });

            var result = await _featureService.DisableFeatureAsync(tenant.Id, featureKey);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling feature {FeatureKey}", featureKey);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Set feature rollout percentage
    /// </summary>
    [HttpPut("{featureKey}/rollout")]
    public async Task<IActionResult> SetRolloutPercentage(string featureKey, [FromBody] SetRolloutRequest request)
    {
        try
        {
            var tenant = _resolutionService.GetCurrentTenant();
            if (tenant == null)
                return BadRequest(new { error = "No tenant in context" });

            var result = await _featureService.SetRolloutPercentageAsync(
                tenant.Id, featureKey, request.Percentage);

            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting rollout for feature {FeatureKey}", featureKey);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Record feature usage
    /// </summary>
    [HttpPost("{featureKey}/usage")]
    public async Task<IActionResult> RecordUsage(string featureKey, [FromBody] RecordUsageRequest? request = null)
    {
        try
        {
            var tenant = _resolutionService.GetCurrentTenant();
            if (tenant == null)
                return BadRequest(new { error = "No tenant in context" });

            var amount = request?.Amount ?? 1;
            var result = await _featureService.RecordFeatureUsageAsync(tenant.Id, featureKey, amount);

            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording usage for feature {FeatureKey}", featureKey);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Check feature usage limit
    /// </summary>
    [HttpGet("{featureKey}/check-limit")]
    public async Task<IActionResult> CheckUsageLimit(string featureKey)
    {
        try
        {
            var tenant = _resolutionService.GetCurrentTenant();
            if (tenant == null)
                return BadRequest(new { error = "No tenant in context" });

            var withinLimit = await _featureService.CheckUsageLimitAsync(tenant.Id, featureKey);
            return Ok(new { featureKey, withinLimit });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking usage limit for feature {FeatureKey}", featureKey);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get feature statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var tenant = _resolutionService.GetCurrentTenant();
            if (tenant == null)
                return BadRequest(new { error = "No tenant in context" });

            var stats = await _featureService.GetStatisticsAsync(tenant.Id);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature statistics");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Initialize default features
    /// </summary>
    [HttpPost("init-defaults")]
    public async Task<IActionResult> InitializeDefaults()
    {
        try
        {
            var tenant = _resolutionService.GetCurrentTenant();
            if (tenant == null)
                return BadRequest(new { error = "No tenant in context" });

            await _featureService.InitializeDefaultFeaturesAsync(tenant.Id);
            return Ok(new { message = "Default features initialized" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing default features");
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request to set feature rollout percentage
/// </summary>
public class SetRolloutRequest
{
    public int Percentage { get; set; }
}

/// <summary>
/// Request to record feature usage
/// </summary>
public class RecordUsageRequest
{
    public long Amount { get; set; } = 1;
}
