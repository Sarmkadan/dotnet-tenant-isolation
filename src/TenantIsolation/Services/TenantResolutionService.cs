// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TenantIsolation.Constants;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;

namespace TenantIsolation.Services;

/// <summary>
/// Resolves current tenant from HTTP request context
/// </summary>
public class TenantResolutionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TenantService _tenantService;
    private readonly ILogger<TenantResolutionService> _logger;

    public TenantResolutionService(
        IHttpContextAccessor httpContextAccessor,
        TenantService tenantService,
        ILogger<TenantResolutionService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Resolve tenant from current HTTP request
    /// </summary>
    public async Task<Tenant> ResolveTenantAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new TenantNotResolvedException("HTTP context not available");

        // Try to get from HTTP context items (cached)
        if (httpContext.Items.TryGetValue(TenantConstants.CurrentTenantContextKey, out var cachedTenant))
            return (Tenant)cachedTenant;

        // Try multiple resolution strategies
        var tenant = await ResolveTenantFromHeaderAsync(httpContext) ??
                    await ResolveTenantFromClaimsAsync(httpContext) ??
                    await ResolveTenantFromRouteAsync(httpContext) ??
                    await ResolveTenantFromSubdomainAsync(httpContext);

        if (tenant == null)
            throw new TenantNotResolvedException("Unable to resolve tenant from request");

        if (tenant.Status != TenantStatus.Active)
            throw new TenantNotActiveException(tenant.Id, $"Tenant is {tenant.Status}");

        // Cache in context
        httpContext.Items[TenantConstants.CurrentTenantContextKey] = tenant;

        _logger.LogInformation("Resolved tenant {TenantId} from request", tenant.Id);
        return tenant;
    }

    /// <summary>
    /// Resolve tenant from HTTP header
    /// </summary>
    private async Task<Tenant?> ResolveTenantFromHeaderAsync(HttpContext httpContext)
    {
        // Try X-Tenant-Id header
        if (httpContext.Request.Headers.TryGetValue(TenantConstants.TenantIdHeader, out var tenantIdValue))
        {
            if (Guid.TryParse(tenantIdValue.ToString(), out var tenantId))
            {
                try
                {
                    return await _tenantService.GetTenantAsync(tenantId);
                }
                catch (TenantNotResolvedException)
                {
                    _logger.LogWarning("Tenant not found from header: {TenantId}", tenantId);
                }
            }
        }

        // Try X-Tenant-Slug header
        if (httpContext.Request.Headers.TryGetValue(TenantConstants.TenantSlugHeader, out var slugValue))
        {
            try
            {
                return await _tenantService.GetTenantBySlugAsync(slugValue.ToString());
            }
            catch (TenantNotResolvedException)
            {
                _logger.LogWarning("Tenant not found from slug header: {Slug}", slugValue.ToString());
            }
        }

        return null;
    }

    /// <summary>
    /// Resolve tenant from user claims
    /// </summary>
    private async Task<Tenant?> ResolveTenantFromClaimsAsync(HttpContext httpContext)
    {
        var user = httpContext.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
            return null;

        // Try tenant_id claim
        var tenantIdClaim = user.FindFirst(TenantConstants.TenantIdClaimType);
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            try
            {
                return await _tenantService.GetTenantAsync(tenantId);
            }
            catch (TenantNotResolvedException)
            {
                _logger.LogWarning("Tenant not found from claim: {TenantId}", tenantId);
            }
        }

        // Try tenant_slug claim
        var slugClaim = user.FindFirst(TenantConstants.TenantSlugClaimType);
        if (slugClaim != null)
        {
            try
            {
                return await _tenantService.GetTenantBySlugAsync(slugClaim.Value);
            }
            catch (TenantNotResolvedException)
            {
                _logger.LogWarning("Tenant not found from slug claim: {Slug}", slugClaim.Value);
            }
        }

        return null;
    }

    /// <summary>
    /// Resolve tenant from route parameter
    /// </summary>
    private async Task<Tenant?> ResolveTenantFromRouteAsync(HttpContext httpContext)
    {
        var routeData = httpContext.GetRouteData();
        if (routeData == null)
            return null;

        // Try tenantId route parameter
        if (routeData.Values.TryGetValue(TenantConstants.TenantRouteParameter, out var tenantIdValue))
        {
            if (Guid.TryParse(tenantIdValue?.ToString(), out var tenantId))
            {
                try
                {
                    return await _tenantService.GetTenantAsync(tenantId);
                }
                catch (TenantNotResolvedException)
                {
                    _logger.LogWarning("Tenant not found from route: {TenantId}", tenantId);
                }
            }
        }

        // Try slug route parameter
        if (routeData.Values.TryGetValue(TenantConstants.TenantSlugRouteParameter, out var slugValue))
        {
            try
            {
                return await _tenantService.GetTenantBySlugAsync(slugValue?.ToString() ?? "");
            }
            catch (TenantNotResolvedException)
            {
                _logger.LogWarning("Tenant not found from route slug: {Slug}", slugValue);
            }
        }

        return null;
    }

    /// <summary>
    /// Resolve tenant from subdomain
    /// </summary>
    private async Task<Tenant?> ResolveTenantFromSubdomainAsync(HttpContext httpContext)
    {
        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrEmpty(host))
            return null;

        // Extract subdomain (first part before first dot)
        var parts = host.Split('.');
        if (parts.Length < 2)
            return null; // Not a subdomain

        var subdomain = parts[0];
        if (subdomain == "www" || subdomain == "api" || subdomain == "mail")
            return null; // Skip common subdomains

        try
        {
            return await _tenantService.GetTenantBySlugAsync(subdomain);
        }
        catch (TenantNotResolvedException)
        {
            _logger.LogDebug("Tenant not found from subdomain: {Subdomain}", subdomain);
        }

        return null;
    }

    /// <summary>
    /// Get current tenant from context
    /// </summary>
    public Tenant? GetCurrentTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        if (httpContext.Items.TryGetValue(TenantConstants.CurrentTenantContextKey, out var tenant))
            return (Tenant)tenant;

        return null;
    }

    /// <summary>
    /// Get current tenant identifier
    /// </summary>
    public Guid? GetCurrentTenantId()
    {
        return GetCurrentTenant()?.Id;
    }

    /// <summary>
    /// Check if request has valid tenant
    /// </summary>
    public bool HasTenant()
    {
        return GetCurrentTenant() != null;
    }

    /// <summary>
    /// Set tenant in context (for testing/admin scenarios)
    /// </summary>
    public void SetTenant(Tenant tenant)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
            httpContext.Items[TenantConstants.CurrentTenantContextKey] = tenant;
    }
}
