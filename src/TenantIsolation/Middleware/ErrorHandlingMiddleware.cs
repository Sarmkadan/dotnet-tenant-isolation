#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TenantIsolation.Exceptions;

namespace TenantIsolation.Middleware;

/// <summary>
/// Middleware for centralized error handling and exception conversion to HTTP responses
/// Ensures consistent error formatting across all endpoints
/// Logs exceptions for diagnostics while returning safe error messages to clients
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invoke middleware to handle exceptions in request pipeline
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in request pipeline: {ExceptionType}", ex.GetType().Name);
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Convert exception to appropriate HTTP response
    /// Maps domain exceptions to specific HTTP status codes with descriptive error details
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case TenantNotActiveException activeEx:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response = new ErrorResponse
                {
                    Code = "TENANT_NOT_ACTIVE",
                    Message = activeEx.Message,
                    StatusCode = 403,
                    TraceId = context.TraceIdentifier
                };
                break;

            case TenantNotResolvedException tenantEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ErrorResponse
                {
                    Code = "TENANT_NOT_FOUND",
                    Message = tenantEx.Message,
                    StatusCode = 400,
                    TraceId = context.TraceIdentifier
                };
                break;

            case TenantIsolationException isolationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ErrorResponse
                {
                    Code = "TENANT_ISOLATION_ERROR",
                    Message = isolationEx.Message,
                    StatusCode = 400,
                    TraceId = context.TraceIdentifier
                };
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = new ErrorResponse
                {
                    Code = "UNAUTHORIZED",
                    Message = "Authentication required to access this resource",
                    StatusCode = 401,
                    TraceId = context.TraceIdentifier
                };
                break;

            case ArgumentNullException argNullEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ErrorResponse
                {
                    Code = "INVALID_ARGUMENT",
                    Message = $"Required argument '{argNullEx.ParamName}' was not provided",
                    StatusCode = 400,
                    TraceId = context.TraceIdentifier
                };
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ErrorResponse
                {
                    Code = "INVALID_ARGUMENT",
                    Message = argEx.Message,
                    StatusCode = 400,
                    TraceId = context.TraceIdentifier
                };
                break;

            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ErrorResponse
                {
                    Code = "INVALID_OPERATION",
                    Message = exception.Message,
                    StatusCode = 400,
                    TraceId = context.TraceIdentifier
                };
                break;

            case TimeoutException:
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response = new ErrorResponse
                {
                    Code = "REQUEST_TIMEOUT",
                    Message = "The request took too long to complete. Please try again.",
                    StatusCode = 408,
                    TraceId = context.TraceIdentifier
                };
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new ErrorResponse
                {
                    Code = "INTERNAL_SERVER_ERROR",
                    Message = "An unexpected error occurred. Please contact support.",
                    StatusCode = 500,
                    TraceId = context.TraceIdentifier,
                    Details = exception.GetType().Name
                };
                break;
        }

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Standard error response format for all API errors
    /// </summary>
    private class ErrorResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("traceId")]
        public string? TraceId { get; set; }

        [JsonPropertyName("details")]
        public string? Details { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
