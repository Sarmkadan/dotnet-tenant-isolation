#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using TenantIsolation.Caching;
using TenantIsolation.Configuration;
using TenantIsolation.Constants;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;
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
    private readonly ICacheProvider _cacheProvider;
    private readonly IMemoryCache _memoryCache;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger,
        IOptions<TenantIsolationOptions> tenantIsolationOptions,
        ICacheProvider cacheProvider,
        IMemoryCache memoryCache)
    {
        _next = next;
        _logger = logger;
        _tenantIsolationOptions = tenantIsolationOptions.Value;
        _cacheProvider = cacheProvider;
        _memoryCache = memoryCache;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantResolutionService tenantResolutionService)
    {
        // Check if the current request path is in the excluded paths
        if (_tenantIsolationOptions.ExcludedPaths != null &&
            _tenantIsolationOptions.ExcludedPaths.Any(path => context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug("Bypassing tenant resolution for excluded path: {RequestPath}", context.Request.Path);
            await _next(context);
            return;
        }

        // Try to get tenant from cache first (by host or header key)
        var cacheKey = GetCacheKey(context);
        if (!string.IsNullOrEmpty(cacheKey))
        {
            var cachedTenant = await _cacheProvider.GetAsync<Tenant>(cacheKey);
            if (cachedTenant != null)
            {
                _logger.LogDebug("Tenant {TenantId} resolved from cache for request {RequestPath}", cachedTenant.Id, context.Request.Path);
                context.Items[TenantConstants.CurrentTenantContextKey] = cachedTenant;
                context.Items["TenantId"] = cachedTenant.Id;
                context.Response.Headers["X-Tenant-Id"] = cachedTenant.Id.ToString();
                context.Response.Headers["X-Tenant-Slug"] = cachedTenant.Slug;
                await _next(context);
                return;
            }
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

            // Cache the resolved tenant with short TTL
            if (!string.IsNullOrEmpty(cacheKey))
            {
                var cacheDuration = TimeSpan.FromMinutes(_tenantIsolationOptions.CacheExpirationMinutes > 0
                    ? _tenantIsolationOptions.CacheExpirationMinutes
                    : 5);
                await _cacheProvider.SetAsync(cacheKey, tenant, cacheDuration);
                _logger.LogDebug("Tenant {TenantId} cached with key {CacheKey} for {DurationMinutes} minutes",
                    tenant.Id, cacheKey, cacheDuration.TotalMinutes);
            }

            context.Items[TenantConstants.CurrentTenantContextKey] = tenant;
            context.Items["TenantId"] = tenant.Id;

            _logger.LogInformation("Tenant {TenantId} resolved for request {RequestPath}",
                tenant.Id, context.Request.Path);

            // Add tenant info to response headers
            context.Response.Headers["X-Tenant-Id"] = tenant.Id.ToString();
            context.Response.Headers["X-Tenant-Slug"] = tenant.Slug;

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

    /// <summary>
    /// Generates a cache key based on the request's host or header information.
    /// This ensures tenant resolution can be cached per host/header combination with short TTL.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A cache key string, or null if caching should be skipped</returns>
    private string? GetCacheKey(HttpContext context)
    {
        // Skip caching if caching is disabled in options
        if (!_tenantIsolationOptions.EnableCaching)
        {
            _logger.LogDebug("Caching is disabled in options, skipping cache key generation");
            return null;
        }

        // Try to get tenant ID from header first (most reliable for caching)
        if (context.Request.Headers.TryGetValue(TenantConstants.TenantIdHeader, out var tenantIdHeader))
        {
            if (Guid.TryParse(tenantIdHeader.ToString(), out var tenantId))
            {
                return new CacheKeyBuilder("tenant:resolution")
                    .WithTenant(tenantId.ToString())
                    .Build();
            }
        }

        // Fall back to host-based key
        var host = context.Request.Host.Host;
        if (!string.IsNullOrEmpty(host))
        {
            return new CacheKeyBuilder("tenant:resolution")
                .WithTenant(host)
                .Build();
        }

        return null;
    }
}

