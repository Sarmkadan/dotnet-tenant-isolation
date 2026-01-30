// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TenantIsolation.Middleware;

/// <summary>
/// Middleware for comprehensive request and response logging
/// Captures request details, execution time, response status, and tenant context
/// Essential for auditing, debugging, and performance monitoring
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Log incoming request and outgoing response with timing
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            // Log incoming request
            LogIncomingRequest(context);

            // Capture response body for logging
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await _next(context);

            stopwatch.Stop();

            // Log outgoing response
            await LogOutgoingResponse(context, memoryStream, stopwatch.ElapsedMilliseconds);

            // Copy response to original stream
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Exception occurred during request processing for {Path}. Duration: {DurationMs}ms",
                context.Request.Path, stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// Log details of incoming HTTP request
    /// Captures method, path, query string, headers, and tenant context
    /// </summary>
    private void LogIncomingRequest(HttpContext context)
    {
        var request = context.Request;
        var tenantId = context.Items.ContainsKey("TenantId") ? context.Items["TenantId"]?.ToString() : "unknown";

        var logMessage = $"[REQUEST] {request.Method} {request.Path}" +
            (string.IsNullOrEmpty(request.QueryString.Value) ? "" : $"{request.QueryString}") +
            $" | TenantId: {tenantId}" +
            $" | RemoteIP: {context.Connection.RemoteIpAddress}" +
            $" | UserAgent: {request.Headers.UserAgent}";

        _logger.LogInformation(logMessage);

        // Log request headers for sensitive operations
        if (request.Method == "POST" || request.Method == "PUT" || request.Method == "DELETE")
        {
            var contentType = request.ContentType ?? "unknown";
            var contentLength = request.ContentLength ?? 0;
            _logger.LogDebug("Request Content-Type: {ContentType}, Content-Length: {ContentLength}",
                contentType, contentLength);
        }
    }

    /// <summary>
    /// Log details of outgoing HTTP response
    /// Captures status code, content length, and execution time
    /// </summary>
    private async Task LogOutgoingResponse(HttpContext context, MemoryStream responseBody, long durationMs)
    {
        var response = context.Response;
        var tenantId = context.Items.ContainsKey("TenantId") ? context.Items["TenantId"]?.ToString() : "unknown";

        var statusCodeCategory = response.StatusCode switch
        {
            >= 200 and < 300 => "SUCCESS",
            >= 300 and < 400 => "REDIRECT",
            >= 400 and < 500 => "CLIENT_ERROR",
            >= 500 => "SERVER_ERROR",
            _ => "UNKNOWN"
        };

        var logMessage = $"[RESPONSE] {response.StatusCode} ({statusCodeCategory}) {context.Request.Method} {context.Request.Path}" +
            $" | TenantId: {tenantId}" +
            $" | Duration: {durationMs}ms" +
            $" | ResponseSize: {response.ContentLength ?? 0}";

        // Log slow requests as warnings
        if (durationMs > 5000)
        {
            _logger.LogWarning("SLOW_REQUEST: {Message}", logMessage);
        }
        else if (response.StatusCode >= 500)
        {
            _logger.LogError(logMessage);
        }
        else if (response.StatusCode >= 400)
        {
            _logger.LogWarning(logMessage);
        }
        else
        {
            _logger.LogInformation(logMessage);
        }

        // Log response body for errors
        if (response.StatusCode >= 400)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseBody);
            var responseContent = await reader.ReadToEndAsync();
            _logger.LogDebug("Error Response Body: {ResponseBody}",
                responseContent.Substring(0, Math.Min(responseContent.Length, 500)));
        }
    }
}

/// <summary>
/// Extension method to register request logging middleware
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
