#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace TenantIsolation.Middleware;

/// <summary>
/// Middleware for establishing request context with correlation and user tracking
/// Sets up trace IDs and tenant context that flows through the entire request pipeline
/// Enables distributed tracing and audit trails across services
/// </summary>
public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestContextMiddleware> _logger;

    public RequestContextMiddleware(RequestDelegate next, ILogger<RequestContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Initialize request context with correlation IDs and metadata
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Set or use existing correlation ID for distributed tracing
        var correlationId = context.Request.Headers.ContainsKey("X-Correlation-ID")
            ? context.Request.Headers["X-Correlation-ID"].ToString()
            : Guid.NewGuid().ToString("N");

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Add("X-Correlation-ID", correlationId);

        // Extract tenant ID from header or route
        var tenantId = ExtractTenantId(context);
        if (!string.IsNullOrEmpty(tenantId))
        {
            context.Items["TenantId"] = tenantId;
            context.Response.Headers.Add("X-Tenant-ID", tenantId);
        }

        // Extract user ID if authenticated
        var userId = context.User?.FindFirst("sub")?.Value ?? context.User?.FindFirst("nameid")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            context.Items["UserId"] = userId;
            context.Response.Headers.Add("X-User-ID", userId);
        }

        // Set request timestamp
        context.Items["RequestStartTime"] = DateTime.UtcNow;
        context.Items["RequestPath"] = context.Request.Path;
        context.Items["RequestMethod"] = context.Request.Method;

        // Add request context to logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "TenantId", tenantId ?? "anonymous" },
            { "UserId", userId ?? "anonymous" },
            { "Path", context.Request.Path },
            { "Method", context.Request.Method }
        }))
        {
            await _next(context);
        }
    }

    /// <summary>
    /// Extract tenant ID from various sources
    /// Priority: header > route parameter > subdomain
    /// </summary>
    private string? ExtractTenantId(HttpContext context)
    {
        // Check X-Tenant-Id header
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue))
            return headerValue.ToString();

        // Check route parameter
        if (context.GetRouteData().Values.TryGetValue("tenantId", out var routeValue))
            return routeValue?.ToString();

        // Check subdomain (e.g., tenant.example.com)
        var host = context.Request.Host.Host;
        if (!host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) &&
            !host.StartsWith("api.", StringComparison.OrdinalIgnoreCase) &&
            host.Contains("."))
        {
            var subdomain = host.Split('.')[0];
            if (!IsReservedSubdomain(subdomain))
                return subdomain;
        }

        return null;
    }

    /// <summary>
    /// Check if subdomain is reserved (not a tenant identifier)
    /// </summary>
    private static bool IsReservedSubdomain(string subdomain)
    {
        var reserved = new[] { "localhost", "www", "api", "admin", "mail", "ftp", "cdn", "static" };
        return reserved.Contains(subdomain, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Utility to access context from dependency injection
/// Provides scoped access to current request context
/// </summary>
public interface IRequestContext
{
    string CorrelationId { get; }
    string? TenantId { get; }
    string? UserId { get; }
    DateTime RequestStartTime { get; }
    string Path { get; }
    string Method { get; }
}

/// <summary>
/// Implementation of request context provider
/// </summary>
public class RequestContext : IRequestContext
{
    private readonly HttpContext? _httpContext;

    public RequestContext(IHttpContextAccessor contextAccessor)
    {
        _httpContext = contextAccessor.HttpContext;
    }

    public string CorrelationId => _httpContext?.Items["CorrelationId"]?.ToString() ?? string.Empty;
    public string? TenantId => _httpContext?.Items["TenantId"]?.ToString();
    public string? UserId => _httpContext?.Items["UserId"]?.ToString();
    public DateTime RequestStartTime => _httpContext?.Items["RequestStartTime"] as DateTime? ?? DateTime.UtcNow;
    public string Path => _httpContext?.Items["RequestPath"]?.ToString() ?? string.Empty;
    public string Method => _httpContext?.Items["RequestMethod"]?.ToString() ?? string.Empty;
}
