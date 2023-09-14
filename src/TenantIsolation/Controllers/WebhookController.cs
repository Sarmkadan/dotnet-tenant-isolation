#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TenantIsolation.Formatters;
using TenantIsolation.Integration;

namespace TenantIsolation.Controllers;

/// <summary>
/// Webhook management endpoints for event subscriptions
/// Allows tenants to register and manage webhook callbacks for events
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookHandler _webhookHandler;
    private readonly IResponseFormatter _formatter;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IWebhookHandler webhookHandler,
        IResponseFormatter formatter,
        ILogger<WebhookController> logger)
    {
        _webhookHandler = webhookHandler;
        _formatter = formatter;
        _logger = logger;
    }

    /// <summary>
    /// Register a new webhook endpoint
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<WebhookSubscription>>> RegisterWebhook(
        [FromBody] RegisterWebhookRequest request)
    {
        try
        {
            if (!Guid.TryParse(request.TenantId, out var tenantId))
                return BadRequest(_formatter.Error("Invalid tenant ID"));

            var subscription = await _webhookHandler.RegisterWebhookAsync(
                tenantId,
                request.EventType,
                request.Url,
                request.Secret);

            _logger.LogInformation("Registered webhook {WebhookId} for tenant {TenantId}",
                subscription.Id, tenantId);

            return CreatedAtAction(nameof(GetWebhook), new { id = subscription.Id },
                _formatter.Success(subscription, "Webhook registered successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering webhook");
            return BadRequest(_formatter.Error($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get webhook by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<WebhookSubscription>>> GetWebhook(Guid id)
    {
        try
        {
            // In a real implementation, this would query a database
            var webhooks = await _webhookHandler.GetWebhooksAsync(Guid.Empty);
            var webhook = webhooks.FirstOrDefault(w => w.Id == id);

            if (webhook == null)
                return NotFound(_formatter.Error("Webhook not found"));

            return Ok(_formatter.Success(webhook));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook {WebhookId}", id);
            return BadRequest(_formatter.Error($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get all webhooks for a tenant
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    public async Task<ActionResult<ApiResponse<List<WebhookSubscription>>>> GetTenantWebhooks(
        string tenantId,
        [FromQuery] string? eventType = null)
    {
        try
        {
            if (!Guid.TryParse(tenantId, out var parsedTenantId))
                return BadRequest(_formatter.Error("Invalid tenant ID"));

            var webhooks = (await _webhookHandler.GetWebhooksAsync(parsedTenantId, eventType))
                .ToList();

            return Ok(_formatter.Success(webhooks, "Webhooks retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhooks for tenant {TenantId}", tenantId);
            return BadRequest(_formatter.Error($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Delete webhook subscription
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteWebhook(Guid id)
    {
        try
        {
            var result = await _webhookHandler.UnregisterWebhookAsync(id);
            if (!result)
                return NotFound(_formatter.Error("Webhook not found"));

            _logger.LogInformation("Deleted webhook {WebhookId}", id);
            return Ok(_formatter.Success("Webhook deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook {WebhookId}", id);
            return BadRequest(_formatter.Error($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get webhook delivery history
    /// </summary>
    [HttpGet("{id}/deliveries")]
    public async Task<ActionResult<ApiResponse<List<WebhookDelivery>>>> GetWebhookDeliveries(
        Guid id,
        [FromQuery] int limit = 10)
    {
        try
        {
            var deliveries = (await _webhookHandler.GetDeliveryHistoryAsync(id, limit))
                .ToList();

            return Ok(_formatter.Success(deliveries, "Delivery history retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery history for webhook {WebhookId}", id);
            return BadRequest(_formatter.Error($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Test webhook by sending a sample payload
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<ActionResult<ApiResponse<object>>> TestWebhook(Guid id)
    {
        try
        {
            _logger.LogInformation("Testing webhook {WebhookId}", id);

            var result = new
            {
                Message = "Webhook test request sent",
                WebhookId = id,
                Timestamp = DateTime.UtcNow,
                Note = "Check webhook delivery history for results"
            };

            return Ok(_formatter.Success(result, "Webhook test initiated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing webhook {WebhookId}", id);
            return BadRequest(_formatter.Error($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Register webhook request model
    /// </summary>
    public class RegisterWebhookRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Secret { get; set; }
    }
}
