#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;

namespace TenantIsolation.Middleware;

/// <summary>
/// Middleware for rate limiting to prevent abuse and excessive resource consumption
/// Implements sliding window rate limiting per tenant and IP address
/// Protects against DoS attacks and ensures fair resource distribution
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, RateLimitOptions? options = null)
    {
        _next = next;
        _logger = logger;
        _options = options ?? new RateLimitOptions();
        _buckets = new ConcurrentDictionary<string, RateLimitBucket>();
    }

    /// <summary>
    /// Check rate limits and enforce them
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health check endpoints
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var tenantId = context.Items.ContainsKey("TenantId") ? context.Items["TenantId"]?.ToString() : "anonymous";
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{tenantId}:{remoteIp}";

        var bucket = _buckets.GetOrAdd(key, _ => new RateLimitBucket(_options.RequestsPerMinute));

        // Check if rate limit exceeded
        if (!bucket.TryConsumeToken())
        {
            _logger.LogWarning("Rate limit exceeded for {Key}. Requests: {Requests}, Limit: {Limit}",
                key, bucket.RequestCount, _options.RequestsPerMinute);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Add("Retry-After", _options.RetryAfterSeconds.ToString());
            context.Response.ContentType = "application/json";

            var errorMessage = new { code = "RATE_LIMIT_EXCEEDED", message = "Too many requests. Please try again later." };
            var json = System.Text.Json.JsonSerializer.Serialize(errorMessage);
            await context.Response.WriteAsync(json);
            return;
        }

        // Add rate limit headers to response
        context.Response.Headers.Add("X-RateLimit-Limit", _options.RequestsPerMinute.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", bucket.RemainingTokens.ToString());
        context.Response.Headers.Add("X-RateLimit-Reset", bucket.ResetTime.ToString("O"));

        await _next(context);
    }

    /// <summary>
    /// Rate limit bucket using sliding window algorithm
    /// Tracks requests within a time window
    /// </summary>
    private class RateLimitBucket
    {
        private readonly int _maxRequests;
        private readonly Queue<DateTime> _requestTimestamps;
        private readonly object _lockObject = new();

        public int RequestCount => _requestTimestamps.Count;
        public DateTime ResetTime { get; private set; }
        public int RemainingTokens => Math.Max(0, _maxRequests - RequestCount);

        public RateLimitBucket(int maxRequests)
        {
            _maxRequests = maxRequests;
            _requestTimestamps = new Queue<DateTime>();
            ResetTime = DateTime.UtcNow.AddMinutes(1);
        }

        /// <summary>
        /// Try to consume a token from the bucket
        /// Uses sliding window: removes requests older than 1 minute
        /// </summary>
        public bool TryConsumeToken()
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                var windowStart = now.AddMinutes(-1);

                // Remove requests outside the window
                while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < windowStart)
                    _requestTimestamps.Dequeue();

                // Check if under limit
                if (_requestTimestamps.Count < _maxRequests)
                {
                    _requestTimestamps.Enqueue(now);
                    ResetTime = now.AddMinutes(1);
                    return true;
                }

                return false;
            }
        }
    }
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Maximum requests per minute per tenant/IP
    /// </summary>
    public int RequestsPerMinute { get; set; } = 60;

    /// <summary>
    /// Seconds to wait before retry (sent in Retry-After header)
    /// </summary>
    public int RetryAfterSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to enable rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Extension method to register rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, RateLimitOptions? options = null)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>(options ?? new RateLimitOptions());
    }
}
