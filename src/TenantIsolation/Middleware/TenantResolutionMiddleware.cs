#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TenantIsolation.Exceptions;
using TenantIsolation.Services;

namespace TenantIsolation.Middleware;

/// <summary>
/// ASP.NET Core middleware for tenant resolution and context setup
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        TenantResolutionService tenantResolutionService,
        TenantService tenantService)
    {
        try
        {
            // Skip tenant resolution for health check endpoints
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/.well-known"))
            {
                await _next(context);
                return;
            }

            // Attempt to resolve tenant
            var tenant = await tenantResolutionService.ResolveTenantAsync();

            // Set current tenant in services
            tenantService.SetCurrentTenant(tenant.Id);

            _logger.LogInformation("Tenant {TenantId} resolved for request {RequestPath}",
                tenant.Id, context.Request.Path);

            // Add tenant info to response headers
            context.Response.Headers.Add("X-Tenant-Id", tenant.Id.ToString());
            context.Response.Headers.Add("X-Tenant-Slug", tenant.Slug);

            await _next(context);
        }
        catch (TenantNotResolvedException ex)
        {
            _logger.LogWarning(ex, "Failed to resolve tenant for request {RequestPath}",
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Tenant not found",
                message = ex.Message,
                code = ex.ErrorCode
            };

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (TenantNotActiveException ex)
        {
            _logger.LogWarning(ex, "Tenant {TenantId} is not active", ex.TenantId);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Tenant not active",
                message = ex.Message,
                code = ex.ErrorCode
            };

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (TenantIsolationException ex)
        {
            _logger.LogError(ex, "Tenant isolation error for request {RequestPath}",
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Tenant isolation error",
                message = ex.Message,
                code = ex.ErrorCode
            };

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in tenant resolution middleware");
            throw;
        }
    }
}
