#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Threading.Tasks;

namespace TenantIsolation.Integration;

/// <summary>
/// Webhook delivery service interface for sending webhook payloads to external endpoints
/// with advanced resilience features including circuit breakers, timeouts, and retry policies.
/// </summary>
public interface IWebhookDeliveryService
{
    /// <summary>
    /// Deliver webhook payload to the specified endpoint with resilience features
    /// </summary>
    /// <param name="payload">The webhook payload to deliver</param>
    /// <param name="endpoint">The webhook endpoint configuration</param>
    /// <returns>Delivery result with status and metadata</returns>
    /// <exception cref="ArgumentNullException">Thrown when payload or endpoint is null</exception>
    Task<WebhookDeliveryResult> DeliverAsync(WebhookPayload payload, WebhookEndpoint endpoint);

    /// <summary>
    /// Get the current circuit breaker state for monitoring
    /// </summary>
    /// <param name="endpointUrl">The endpoint URL to check</param>
    /// <returns>Circuit breaker state information</returns>
    CircuitBreakerState GetCircuitBreakerState(string endpointUrl);
}

/// <summary>
/// Webhook endpoint configuration with delivery settings
/// </summary>
public class WebhookEndpoint
{
    /// <summary>
    /// The webhook endpoint URL
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Optional secret key for HMAC-SHA256 payload signing
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Timeout for the HTTP request (default: 10 seconds)
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Maximum number of retry attempts (default: 3)
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay between retries in milliseconds (default: 1000ms)
    /// </summary>
    public int BaseDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Whether to respect Retry-After header (default: true)
    /// </summary>
    public bool RespectRetryAfter { get; set; } = true;

    /// <summary>
    /// Whether to use circuit breaker pattern (default: true)
    /// </summary>
    public bool UseCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Circuit breaker failure threshold percentage (0-100, default: 50)
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 50;

    /// <summary>
    /// Circuit breaker sampling period in seconds (default: 60)
    /// </summary>
    public int CircuitBreakerSamplingPeriodSeconds { get; set; } = 60;

    /// <summary>
    /// Circuit breaker minimum through requests (default: 10)
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Circuit breaker duration of open state in seconds (default: 30)
    /// </summary>
    public int CircuitBreakerDurationOfOpenStateSeconds { get; set; } = 30;
}

/// <summary>
/// Result of a webhook delivery attempt
/// </summary>
public class WebhookDeliveryResult
{
    /// <summary>
    /// Whether the delivery was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// HTTP status code if applicable
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Error message if delivery failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Total duration of the delivery attempt
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Whether the request was retried
    /// </summary>
    public bool WasRetried => RetryCount > 0;
}

/// <summary>
/// Circuit breaker state information for monitoring
/// </summary>
public class CircuitBreakerState
{
    /// <summary>
    /// The endpoint URL
    /// </summary>
    public required string EndpointUrl { get; set; }

    /// <summary>
    /// Current state of the circuit breaker
    /// </summary>
    public CircuitBreakerStatus Status { get; set; }

    /// <summary>
    /// Number of failures in the current sampling window
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Number of successful requests in the current sampling window
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Total number of requests in the current sampling window
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Timestamp when the circuit breaker was last changed
    /// </summary>
    public DateTime LastChanged { get; set; }

    /// <summary>
    /// Exception that caused the last failure (if any)
    /// </summary>
    public string? LastFailureReason { get; set; }
}

/// <summary>
/// Circuit breaker operational status
/// </summary>
public enum CircuitBreakerStatus
{
    /// <summary>Circuit is closed - requests are allowed</summary>
    Closed,

    /// <summary>Circuit is open - requests are blocked</summary>
    Open,

    /// <summary>Circuit is half-open - limited requests are allowed for testing</summary>
    HalfOpen
}