#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // Add this for IOptions
using TenantIsolation.Configuration; // Add this for TenantIsolationOptions
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
    private readonly TenantIsolationOptions _tenantIsolationOptions;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger,
        IOptions<TenantIsolationOptions> tenantIsolationOptions) // Inject IOptions
    {
        _next = next;
        _logger = logger;
        _tenantIsolationOptions = tenantIsolationOptions.Value; // Access the options
    }

    public async Task InvokeAsync(
        HttpContext context,
        TenantResolutionService tenantResolutionService,
        TenantService tenantService)
    {
        // Check if the current request path is in the excluded paths
        if (_tenantIsolationOptions.ExcludedPaths != null &&
            _tenantIsolationOptions.ExcludedPaths.Any(path => context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug("Bypassing tenant resolution for excluded path: {RequestPath}", context.Request.Path);
            await _next(context);
            return;
        }

        try
        {
            // Attempt to resolve tenant
            var tenant = await tenantResolutionService.ResolveTenantAsync();

            // Null check for tenant before accessing its properties
            if (tenant == null)
            {
                _logger.LogWarning("Tenant not resolved for request {RequestPath} and path is not excluded. Proceeding without tenant context.",
                    context.Request.Path);
                // Optionally throw an exception if a tenant is mandatory for all non-excluded paths
                // throw new TenantNotResolvedException("Tenant could not be resolved for this request.");
                await _next(context);
                return;
            }

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

