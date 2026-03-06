// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TenantIsolation.Events;
using TenantIsolation.Utilities;

namespace TenantIsolation.Integration;

/// <summary>
/// Webhook payload sent to registered endpoints
/// </summary>
public class WebhookPayload
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("tenantId")]
    public Guid TenantId { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// Webhook subscription for event notifications
/// </summary>
public class WebhookSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTriggeredAt { get; set; }
    public int FailureCount { get; set; }
    public DateTime? DisabledAt { get; set; }
}

/// <summary>
/// Webhook handler for publishing events to external services
/// Implements retry logic and signature generation for security
/// </summary>
public interface IWebhookHandler
{
    /// <summary>
    /// Register webhook subscription
    /// </summary>
    Task<WebhookSubscription> RegisterWebhookAsync(Guid tenantId, string eventType, string url, string? secret = null);

    /// <summary>
    /// Unregister webhook
    /// </summary>
    Task<bool> UnregisterWebhookAsync(Guid webhookId);

    /// <summary>
    /// Send webhook to registered endpoints
    /// </summary>
    Task SendWebhookAsync(TenantEvent @event);

    /// <summary>
    /// Get webhooks for tenant
    /// </summary>
    Task<IEnumerable<WebhookSubscription>> GetWebhooksAsync(Guid tenantId, string? eventType = null);

    /// <summary>
    /// Get webhook delivery history
    /// </summary>
    Task<IEnumerable<WebhookDelivery>> GetDeliveryHistoryAsync(Guid webhookId, int limit = 10);
}

/// <summary>
/// Webhook delivery history record
/// </summary>
public class WebhookDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WebhookId { get; set; }
    public string EventId { get; set; } = string.Empty;
    public int HttpStatusCode { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; }
}

/// <summary>
/// Webhook handler implementation
/// </summary>
public class WebhookHandler : IWebhookHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookHandler> _logger;
    private readonly ConcurrentDictionary<Guid, WebhookSubscription> _subscriptions;
    private readonly ConcurrentDictionary<Guid, List<WebhookDelivery>> _deliveryHistory;

    public WebhookHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _subscriptions = new ConcurrentDictionary<Guid, WebhookSubscription>();
        _deliveryHistory = new ConcurrentDictionary<Guid, List<WebhookDelivery>>();
    }

    public async Task<WebhookSubscription> RegisterWebhookAsync(Guid tenantId, string eventType, string url, string? secret = null)
    {
        ValidationUtility.RequireValidUrl(url, nameof(url));

        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = eventType,
            Url = url,
            Secret = secret
        };

        if (!_subscriptions.TryAdd(subscription.Id, subscription))
            throw new InvalidOperationException("Failed to register webhook");

        // Initialize delivery history
        _deliveryHistory.TryAdd(subscription.Id, new List<WebhookDelivery>());

        _logger.LogInformation("Registered webhook {WebhookId} for tenant {TenantId} event {EventType}",
            subscription.Id, tenantId, eventType);

        return await Task.FromResult(subscription);
    }

    public async Task<bool> UnregisterWebhookAsync(Guid webhookId)
    {
        var result = _subscriptions.TryRemove(webhookId, out _);
        _deliveryHistory.TryRemove(webhookId, out _);

        if (result)
            _logger.LogInformation("Unregistered webhook {WebhookId}", webhookId);

        return await Task.FromResult(result);
    }

    public async Task SendWebhookAsync(TenantEvent @event)
    {
        var webhooks = _subscriptions.Values
            .Where(w => w.TenantId == @event.TenantId &&
                       w.EventType == @event.GetType().Name &&
                       w.IsActive)
            .ToList();

        if (!webhooks.Any())
        {
            _logger.LogDebug("No active webhooks for event {EventType}",
                @event.GetType().Name);
            return;
        }

        var tasks = webhooks.Select(w => SendToWebhookAsync(@event, w));
        await Task.WhenAll(tasks);
    }

    public async Task<IEnumerable<WebhookSubscription>> GetWebhooksAsync(Guid tenantId, string? eventType = null)
    {
        var webhooks = _subscriptions.Values
            .Where(w => w.TenantId == tenantId &&
                       (string.IsNullOrEmpty(eventType) || w.EventType == eventType))
            .ToList();

        return await Task.FromResult(webhooks);
    }

    public async Task<IEnumerable<WebhookDelivery>> GetDeliveryHistoryAsync(Guid webhookId, int limit = 10)
    {
        if (!_deliveryHistory.TryGetValue(webhookId, out var history))
            return Enumerable.Empty<WebhookDelivery>();

        return await Task.FromResult(history.OrderByDescending(d => d.SentAt).Take(limit));
    }

    /// <summary>
    /// Send webhook payload to endpoint with retry logic
    /// </summary>
    private async Task SendToWebhookAsync(TenantEvent @event, WebhookSubscription webhook)
    {
        var payload = new WebhookPayload
        {
            EventId = @event.EventId,
            EventType = @event.GetType().Name,
            TenantId = @event.TenantId,
            Timestamp = @event.OccurredAt,
            Data = @event
        };

        // Generate signature if secret is configured
        if (!string.IsNullOrEmpty(webhook.Secret))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            payload.Signature = CryptographyUtility.GenerateHmacSha256(json, webhook.Secret);
        }

        var maxRetries = 3;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("webhook");
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Add webhook signature header if available
                if (!string.IsNullOrEmpty(payload.Signature))
                    content.Headers.Add("X-Webhook-Signature", payload.Signature);

                var response = await client.PostAsync(webhook.Url, content);

                var delivery = new WebhookDelivery
                {
                    WebhookId = webhook.Id,
                    EventId = @event.EventId,
                    HttpStatusCode = (int)response.StatusCode,
                    IsSuccessful = response.IsSuccessStatusCode,
                    RetryCount = retryCount
                };

                if (response.IsSuccessStatusCode)
                {
                    webhook.LastTriggeredAt = DateTime.UtcNow;
                    webhook.FailureCount = 0;
                    _logger.LogInformation("Webhook {WebhookId} delivered successfully for event {EventId}",
                        webhook.Id, @event.EventId);
                }
                else
                {
                    delivery.ErrorMessage = $"HTTP {(int)response.StatusCode}";
                    webhook.FailureCount++;

                    if (webhook.FailureCount > 5)
                    {
                        webhook.IsActive = false;
                        webhook.DisabledAt = DateTime.UtcNow;
                        _logger.LogWarning("Webhook {WebhookId} disabled due to repeated failures",
                            webhook.Id);
                    }
                }

                RecordDelivery(webhook.Id, delivery);
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Webhook {WebhookId} delivery attempt {Attempt} failed",
                    webhook.Id, retryCount);

                if (retryCount < maxRetries)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
        }

        webhook.FailureCount++;
    }

    private void RecordDelivery(Guid webhookId, WebhookDelivery delivery)
    {
        if (_deliveryHistory.TryGetValue(webhookId, out var history))
            history.Add(delivery);
    }
}
