#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TenantIsolation.Middleware;

/// <summary>
/// Extension methods for RequestContextMiddleware providing additional functionality
/// for request context management and diagnostics
/// </summary>
public static class RequestContextMiddlewareExtensions
{
    /// <summary>
    /// Registers request context middleware with custom logger configuration.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <param name="configureLogger">Optional logger configuration action.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IApplicationBuilder UseRequestContext(
        this IApplicationBuilder builder,
        Action<ILoggingBuilder>? configureLogger = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Configure logging if provided
        if (configureLogger != null)
        {
            ArgumentNullException.ThrowIfNull(builder.ApplicationServices);
            var loggingBuilder = builder.ApplicationServices.GetRequiredService<ILoggingBuilder>();
            configureLogger(loggingBuilder);
        }

        return builder.UseMiddleware<RequestContextMiddleware>();
    }

    /// <summary>
    /// Registers request context middleware with tenant ID extraction strategy override.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <param name="tenantIdExtractor">Custom tenant ID extraction function.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> or <paramref name="tenantIdExtractor"/> is <see langword="null"/>.
    /// </exception>
    public static IApplicationBuilder UseRequestContext(
        this IApplicationBuilder builder,
        Func<HttpContext, string?> tenantIdExtractor)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(tenantIdExtractor);

        return builder.Use(async (context, next) =>
        {
            // Set or use existing correlation ID for distributed tracing
            var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationHeader)
                ? correlationHeader.ToString()
                : Guid.NewGuid().ToString("N");

            context.Items["CorrelationId"] = correlationId;
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            // Use custom tenant ID extractor
            var tenantId = tenantIdExtractor(context);
            if (!string.IsNullOrEmpty(tenantId))
            {
                context.Items["TenantId"] = tenantId;
                context.Response.Headers["X-Tenant-ID"] = tenantId;
            }

            // Extract user ID if authenticated
            var userId = context.User?.FindFirst("sub")?.Value ?? context.User?.FindFirst("nameid")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                context.Items["UserId"] = userId;
                context.Response.Headers["X-User-ID"] = userId;
            }

            // Set request timestamp
            context.Items["RequestStartTime"] = DateTime.UtcNow;
            context.Items["RequestPath"] = context.Request.Path;
            context.Items["RequestMethod"] = context.Request.Method;

            await next();
        });
    }

    /// <summary>
    /// Registers request context middleware with request timeout configuration.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <param name="timeout">Request timeout duration.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is not positive.</exception>
    public static IApplicationBuilder UseRequestContext(
        this IApplicationBuilder builder,
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive");
        }

        return builder.Use(async (context, next) =>
        {
            // Set request timeout in context
            context.Items["RequestTimeout"] = timeout;

            // Delegate to the actual middleware
            var middleware = new RequestContextMiddleware(
                next,
                context.RequestServices.GetRequiredService<ILogger<RequestContextMiddleware>>());
            await middleware.InvokeAsync(context);
        });
    }

    /// <summary>
    /// Gets the current request context from HttpContext.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> instance.</param>
    /// <returns>Request context or <see langword="null"/> if not available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    public static IRequestContext? GetRequestContext(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items["RequestContext"] as IRequestContext
            ?? new RequestContextFromHttpContext(context);
    }

    /// <summary>
    /// Gets the current request context from IServiceProvider.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <returns>Request context or <see langword="null"/> if not available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IRequestContext? GetRequestContext(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var httpContextAccessor = services.GetService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor?.HttpContext;

        return httpContext?.GetRequestContext();
    }

    /// <summary>
    /// Gets the correlation ID from the current request context.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> instance.</param>
    /// <returns>Correlation ID.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when request context is not available.</exception>
    public static string GetCorrelationId(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetRequestContext()?.CorrelationId
            ?? throw new InvalidOperationException("Request context is not available");
    }

    /// <summary>
    /// Gets the tenant ID from the current request context.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> instance.</param>
    /// <returns>Tenant ID or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when request context is not available.</exception>
    public static string? GetTenantId(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetRequestContext()?.TenantId;
    }

    /// <summary>
    /// Gets the user ID from the current request context.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> instance.</param>
    /// <returns>User ID or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    public static string? GetUserId(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetRequestContext()?.UserId;
    }

    /// <summary>
    /// Gets the request start time from the current request context.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> instance.</param>
    /// <returns>Request start time.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    public static DateTime GetRequestStartTime(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetRequestContext()?.RequestStartTime ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the request duration from the current request context.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> instance.</param>
    /// <returns><see cref="TimeSpan"/> representing request duration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    public static TimeSpan GetRequestDuration(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var startTime = context.GetRequestStartTime();
        return DateTime.UtcNow - startTime;
    }

    /// <summary>
    /// Checks if the current request is associated with a specific tenant.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> instance.</param>
    /// <param name="tenantId">Tenant ID to check.</param>
    /// <returns><see langword="true"/> if request is for the specified tenant.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public static bool IsForTenant(this HttpContext context, string tenantId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        return string.Equals(
            context.GetRequestContext()?.TenantId,
            tenantId,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Private implementation of <see cref="IRequestContext"/> that wraps <see cref="HttpContext"/> directly.
    /// Provides request-scoped context data extracted from the HTTP request pipeline.
    /// </summary>
    private sealed class RequestContextFromHttpContext : IRequestContext
    {
        private readonly HttpContext _context;

        public RequestContextFromHttpContext(HttpContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public string CorrelationId => _context.Items["CorrelationId"]?.ToString() ?? string.Empty;
        public string? TenantId => _context.Items["TenantId"]?.ToString();
        public string? UserId => _context.Items["UserId"]?.ToString();
        public DateTime RequestStartTime => _context.Items["RequestStartTime"] as DateTime? ?? DateTime.UtcNow;
        public string Path => _context.Items["RequestPath"]?.ToString() ?? string.Empty;
        public string Method => _context.Items["RequestMethod"]?.ToString() ?? string.Empty;
    }
}