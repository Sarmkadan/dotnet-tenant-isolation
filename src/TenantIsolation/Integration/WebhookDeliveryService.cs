#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TenantIsolation.Utilities;

namespace TenantIsolation.Integration;

/// <summary>
/// Webhook delivery service implementation with advanced resilience features including
/// circuit breakers, timeouts, and retry policies with Retry-After header support.
/// </summary>
public class WebhookDeliveryService : IWebhookDeliveryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryService> _logger;
    private readonly ConcurrentDictionary<string, CircuitBreaker> _circuitBreakers;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookDeliveryService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for creating clients</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="timeProvider">Time provider for testing</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClientFactory or logger is null</exception>
    public WebhookDeliveryService(
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDeliveryService> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _circuitBreakers = new ConcurrentDictionary<string, CircuitBreaker>();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async Task<WebhookDeliveryResult> DeliverAsync(
        WebhookPayload payload,
        WebhookEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(endpoint);

        var stopwatch = Stopwatch.StartNew();
        var result = new WebhookDeliveryResult
        {
            RetryCount = 0
        };

        try
        {
            // Check circuit breaker before attempting delivery
            if (endpoint.UseCircuitBreaker)
            {
                var circuitBreaker = GetCircuitBreaker(endpoint.Url);
                if (circuitBreaker.IsOpen)
                {
                    _logger.LogWarning("Circuit breaker is OPEN for endpoint {Url}. Skipping delivery.", endpoint.Url);
                    result.IsSuccess = false;
                    result.ErrorMessage = "Circuit breaker is open - endpoint temporarily unavailable";
                    return result;
                }
            }

            // Build the signed payload
            var signedPayload = BuildSignedPayload(payload, endpoint);
            var json = JsonSerializer.Serialize(signedPayload);

            // Create HTTP request with timeout
            using var cts = new CancellationTokenSource(endpoint.Timeout);
            var client = _httpClientFactory.CreateClient("webhook");
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add signature header if available
            if (!string.IsNullOrEmpty(signedPayload.Signature))
            {
                content.Headers.Add("X-Signature", signedPayload.Signature);
            }

            // Add correlation headers for tracing
            content.Headers.Add("X-Event-Id", payload.EventId);
            content.Headers.Add("X-Event-Type", payload.EventType);
            content.Headers.Add("X-Tenant-Id", payload.TenantId.ToString());

            _logger.LogInformation(
                "Delivering webhook to {Url} for event {EventId} (attempt 1)",
                endpoint.Url,
                payload.EventId);

            // Make the HTTP request
            var response = await client.PostAsync(endpoint.Url, content, cts.Token);

            result.HttpStatusCode = (int)response.StatusCode;
            result.RetryCount = 0;

            // Handle response
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Webhook delivered successfully to {Url} for event {EventId} in {Duration}ms",
                    endpoint.Url,
                    payload.EventId,
                    stopwatch.ElapsedMilliseconds);

                result.IsSuccess = true;
                result.Duration = stopwatch.Elapsed;

                // Record success in circuit breaker
                if (endpoint.UseCircuitBreaker)
                {
                    GetCircuitBreaker(endpoint.Url).RecordSuccess();
                }

                return result;
            }
            else
            {
                // Handle 5xx errors with retry logic
                if ((int)response.StatusCode >= 500 && endpoint.MaxRetries > 0)
                {
                    result = await HandleRetryableErrorAsync(
                        payload,
                        endpoint,
                        response,
                        result,
                        stopwatch);
                }
                else
                {
                    // Non-retryable error
                    var errorContent = await response.Content.ReadAsStringAsync();
                    result.IsSuccess = false;
                    result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {errorContent}";
                    result.Duration = stopwatch.Elapsed;

                    // Record failure in circuit breaker
                    if (endpoint.UseCircuitBreaker)
                    {
                        GetCircuitBreaker(endpoint.Url).RecordFailure(result.ErrorMessage);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stopwatch.Elapsed >= endpoint.Timeout)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"Request timed out after {endpoint.Timeout.TotalSeconds} seconds";
            result.Duration = stopwatch.Elapsed;

            // Record failure in circuit breaker
            if (endpoint.UseCircuitBreaker)
            {
                GetCircuitBreaker(endpoint.Url).RecordFailure(result.ErrorMessage);
            }

            _logger.LogWarning(
                "Webhook delivery to {Url} timed out after {Timeout}s: {ErrorMessage}",
                endpoint.Url,
                endpoint.Timeout.TotalSeconds,
                result.ErrorMessage);
        }
        catch (HttpRequestException ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.Duration = stopwatch.Elapsed;

            // Record failure in circuit breaker
            if (endpoint.UseCircuitBreaker)
            {
                GetCircuitBreaker(endpoint.Url).RecordFailure(result.ErrorMessage);
            }

            _logger.LogError(ex, "HTTP request failed for webhook delivery to {Url}", endpoint.Url);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.Duration = stopwatch.Elapsed;

            // Record failure in circuit breaker
            if (endpoint.UseCircuitBreaker)
            {
                GetCircuitBreaker(endpoint.Url).RecordFailure(result.ErrorMessage);
            }

            _logger.LogError(ex, "Unexpected error during webhook delivery to {Url}", endpoint.Url);
        }

        return result;
    }

    /// <inheritdoc/>
    public CircuitBreakerState GetCircuitBreakerState(string endpointUrl)
    {
        ArgumentException.ThrowIfNullOrEmpty(endpointUrl);

        var circuitBreaker = GetCircuitBreaker(endpointUrl);
        var state = circuitBreaker.GetState();

        return new CircuitBreakerState
        {
            EndpointUrl = endpointUrl,
            Status = state.Status,
            FailureCount = state.FailureCount,
            SuccessCount = state.SuccessCount,
            TotalRequests = state.TotalRequests,
            LastChanged = state.LastChanged,
            LastFailureReason = state.LastFailureReason
        };
    }

    /// <summary>
    /// Build signed webhook payload with HMAC-SHA256 signature
    /// </summary>
    private WebhookPayload BuildSignedPayload(WebhookPayload payload, WebhookEndpoint endpoint)
    {
        // Create a copy to avoid modifying the original
        var signedPayload = new WebhookPayload
        {
            EventId = payload.EventId,
            EventType = payload.EventType,
            TenantId = payload.TenantId,
            Timestamp = payload.Timestamp,
            Data = payload.Data
        };

        // Generate signature if secret is configured
        if (!string.IsNullOrEmpty(endpoint.Secret))
        {
            var json = JsonSerializer.Serialize(signedPayload, new JsonSerializerOptions { WriteIndented = false });
            signedPayload.Signature = CryptographyUtility.GenerateHmacSha256(json, endpoint.Secret);
        }

        return signedPayload;
    }

    /// <summary>
    /// Handle retryable HTTP errors (5xx) with exponential backoff and Retry-After support
    /// </summary>
    private async Task<WebhookDeliveryResult> HandleRetryableErrorAsync(
        WebhookPayload payload,
        WebhookEndpoint endpoint,
        HttpResponseMessage response,
        WebhookDeliveryResult result,
        Stopwatch stopwatch)
    {
        var retryCount = 0;
        var lastError = string.Empty;
        var errorContent = await response.Content.ReadAsStringAsync();

        while (retryCount < endpoint.MaxRetries)
        {
            retryCount++;
            result.RetryCount = retryCount;

            // Respect Retry-After header if present and configured
            if (endpoint.RespectRetryAfter && response.Headers.RetryAfter != null)
            {
                var retryDelay = response.Headers.RetryAfter.Delta ?? TimeSpan.FromSeconds(5);
                _logger.LogInformation(
                    "Respecting Retry-After header: waiting {Delay}s before retry {RetryCount}/{MaxRetries}",
                    retryDelay.TotalSeconds,
                    retryCount,
                    endpoint.MaxRetries);

                await Task.Delay(retryDelay, _timeProvider);
            }
            else
            {
                // Exponential backoff with jitter
                var delayMs = endpoint.BaseDelayMilliseconds * (int)Math.Pow(2, retryCount - 1);
                var jitter = Random.Shared.Next(0, delayMs / 2);
                var actualDelay = delayMs + jitter;

                _logger.LogInformation(
                    "Retry {RetryCount}/{MaxRetries} in {Delay}ms for event {EventId}",
                    retryCount,
                    endpoint.MaxRetries,
                    actualDelay,
                    payload.EventId);

                await Task.Delay(actualDelay);
            }

            try
            {
                // Recreate client and request for retry
                using var cts = new CancellationTokenSource(endpoint.Timeout);
                var client = _httpClientFactory.CreateClient("webhook");
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!string.IsNullOrEmpty(endpoint.Secret))
                {
                    var signedJson = CryptographyUtility.GenerateHmacSha256(json, endpoint.Secret);
                    content.Headers.Add("X-Signature", signedJson);
                }

                _logger.LogInformation(
                    "Retry {RetryCount}/{MaxRetries} delivering webhook to {Url} for event {EventId}",
                    retryCount,
                    endpoint.MaxRetries,
                    endpoint.Url,
                    payload.EventId);

                var retryResponse = await client.PostAsync(endpoint.Url, content, cts.Token);

                if (retryResponse.IsSuccessStatusCode)
                {
                    result.IsSuccess = true;
                    result.HttpStatusCode = (int)retryResponse.StatusCode;
                    result.ErrorMessage = null;
                    result.Duration = stopwatch.Elapsed;

                    // Record success in circuit breaker
                    if (endpoint.UseCircuitBreaker)
                    {
                        GetCircuitBreaker(endpoint.Url).RecordSuccess();
                    }

                    _logger.LogInformation(
                        "Webhook delivered successfully on retry {RetryCount}/{MaxRetries} to {Url} for event {EventId}",
                        retryCount,
                        endpoint.MaxRetries,
                        endpoint.Url,
                        payload.EventId);

                    return result;
                }
                else if ((int)retryResponse.StatusCode >= 500 && retryCount < endpoint.MaxRetries)
                {
                    lastError = $"HTTP {(int)retryResponse.StatusCode}: {await retryResponse.Content.ReadAsStringAsync()}";
                    errorContent = lastError;
                    response = retryResponse;
                }
                else
                {
                    // Final attempt failed
                    result.IsSuccess = false;
                    result.HttpStatusCode = (int)retryResponse.StatusCode;
                    result.ErrorMessage = $"HTTP {(int)retryResponse.StatusCode}: {await retryResponse.Content.ReadAsStringAsync()}";
                    result.Duration = stopwatch.Elapsed;

                    // Record failure in circuit breaker
                    if (endpoint.UseCircuitBreaker)
                    {
                        GetCircuitBreaker(endpoint.Url).RecordFailure(result.ErrorMessage);
                    }

                    return result;
                }
            }
            catch (Exception ex) when (retryCount < endpoint.MaxRetries)
            {
                lastError = ex.Message;
                _logger.LogWarning(ex, "Retry {RetryCount} failed for webhook to {Url}", retryCount, endpoint.Url);
            }
        }

        result.IsSuccess = false;
        result.ErrorMessage = lastError;
        result.Duration = stopwatch.Elapsed;

        // Record failure in circuit breaker
        if (endpoint.UseCircuitBreaker)
        {
            GetCircuitBreaker(endpoint.Url).RecordFailure(result.ErrorMessage);
        }

        return result;
    }

    /// <summary>
    /// Get or create circuit breaker for the specified endpoint
    /// </summary>
    private CircuitBreaker GetCircuitBreaker(string endpointUrl)
    {
        return _circuitBreakers.GetOrAdd(
            endpointUrl,
            url => new CircuitBreaker(
                endpointUrl,
                failureThreshold: 50,
                samplingPeriod: TimeSpan.FromSeconds(60),
                minimumThroughput: 10,
                durationOfOpenState: TimeSpan.FromSeconds(30),
                logger: _logger)
        );
    }

    /// <summary>
    /// Circuit breaker implementation for tracking endpoint health
    /// </summary>
    private class CircuitBreaker
    {
        private readonly string _endpointUrl;
        private readonly int _failureThreshold;
        private readonly TimeSpan _samplingPeriod;
        private readonly int _minimumThroughput;
        private readonly TimeSpan _durationOfOpenState;
        private readonly ILogger _logger;

        private int _failureCount;
        private int _successCount;
        private int _totalRequests;
        private CircuitBreakerStatus _status;
        private DateTime _lastFailureTime;
        private string? _lastFailureReason;
        private DateTime _lastStateChangeTime;

        public CircuitBreaker(
            string endpointUrl,
            int failureThreshold,
            TimeSpan samplingPeriod,
            int minimumThroughput,
            TimeSpan durationOfOpenState,
            ILogger logger)
        {
            _endpointUrl = endpointUrl;
            _failureThreshold = failureThreshold;
            _samplingPeriod = samplingPeriod;
            _minimumThroughput = minimumThroughput;
            _durationOfOpenState = durationOfOpenState;
            _logger = logger;
            _status = CircuitBreakerStatus.Closed;
            _lastStateChangeTime = DateTime.UtcNow;
        }

        public bool IsOpen => _status == CircuitBreakerStatus.Open;

        public void RecordSuccess()
        {
            Interlocked.Increment(ref _successCount);
            Interlocked.Increment(ref _totalRequests);

            // Reset failure count on success
            if (_failureCount > 0)
            {
                Interlocked.Exchange(ref _failureCount, 0);
                _lastFailureReason = null;
            }

            CheckStateTransition();
        }

        public void RecordFailure(string reason)
        {
            Interlocked.Increment(ref _failureCount);
            Interlocked.Increment(ref _totalRequests);
            _lastFailureReason = reason;
            _lastFailureTime = DateTime.UtcNow;

            CheckStateTransition();
        }

        public CircuitBreakerStateInfo GetState()
        {
            var now = DateTime.UtcNow;
            var windowStart = now - _samplingPeriod;

            // Calculate stats within sampling period
            var recentSuccess = _successCount;
            var recentFailure = _failureCount;
            var recentTotal = _totalRequests;

            return new CircuitBreakerStateInfo
            {
                Status = _status,
                FailureCount = recentFailure,
                SuccessCount = recentSuccess,
                TotalRequests = recentTotal,
                LastChanged = _lastStateChangeTime,
                LastFailureReason = _lastFailureReason
            };
        }

        private void CheckStateTransition()
        {
            var stateInfo = GetState();
            var now = DateTime.UtcNow;

            switch (_status)
            {
                case CircuitBreakerStatus.Closed:
                    // Check if we should open the circuit
                    if (stateInfo.TotalRequests >= _minimumThroughput &&
                        stateInfo.FailureCount * 100 / stateInfo.TotalRequests >= _failureThreshold)
                    {
                        _status = CircuitBreakerStatus.Open;
                        _lastStateChangeTime = now;
                        _logger.LogWarning(
                            "Circuit breaker OPENED for {EndpointUrl}: {FailureCount}/{TotalRequests} failures ({Threshold}% threshold)",
                            _endpointUrl,
                            stateInfo.FailureCount,
                            stateInfo.TotalRequests,
                            _failureThreshold);
                    }
                    break;

                case CircuitBreakerStatus.Open:
                    // Check if we should transition to half-open
                    if (now - _lastStateChangeTime >= _durationOfOpenState)
                    {
                        _status = CircuitBreakerStatus.HalfOpen;
                        _lastStateChangeTime = now;
                        _logger.LogInformation(
                            "Circuit breaker HALF-OPENED for {EndpointUrl}. Testing endpoint health.",
                            _endpointUrl);
                    }
                    break;

                case CircuitBreakerStatus.HalfOpen:
                    // In half-open state, we allow limited requests to test if endpoint is healthy
                    // For simplicity, we'll just log and not change state automatically
                    // The next successful request will close the circuit
                    break;
            }
        }
    }

    /// <summary>
    /// Internal state information for circuit breaker
    /// </summary>
    private class CircuitBreakerStateInfo
    {
        public CircuitBreakerStatus Status { get; set; }
        public int FailureCount { get; set; }
        public int SuccessCount { get; set; }
        public int TotalRequests { get; set; }
        public DateTime LastChanged { get; set; }
        public string? LastFailureReason { get; set; }
    }
}

/// <summary>
/// Extension methods for IServiceCollection to register webhook delivery service
/// </summary>
public static class WebhookDeliveryServiceExtensions
{
    /// <summary>
    /// Add webhook delivery service with default configuration
    /// </summary>
    public static IServiceCollection AddWebhookDeliveryService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient("webhook");
        services.AddSingleton<IWebhookDeliveryService, WebhookDeliveryService>();

        return services;
    }

    /// <summary>
    /// Add webhook delivery service with custom configuration
    /// </summary>
    public static IServiceCollection AddWebhookDeliveryService(
        this IServiceCollection services,
        Action<WebhookDeliveryOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddHttpClient("webhook");
        services.AddSingleton<IWebhookDeliveryService, WebhookDeliveryService>();

        // Apply default options
        var options = new WebhookDeliveryOptions();
        configure(options);

        return services;
    }
}

/// <summary>
/// Configuration options for webhook delivery service
/// </summary>
public class WebhookDeliveryOptions
{
    /// <summary>
    /// Default timeout for webhook requests
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Default maximum number of retries
    /// </summary>
    public int DefaultMaxRetries { get; set; } = 3;

    /// <summary>
    /// Default base delay between retries in milliseconds
    /// </summary>
    public int DefaultBaseDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Whether to respect Retry-After header by default
    /// </summary>
    public bool DefaultRespectRetryAfter { get; set; } = true;

    /// <summary>
    /// Whether to use circuit breaker by default
    /// </summary>
    public bool DefaultUseCircuitBreaker { get; set; } = true;
}