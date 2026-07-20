#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Frozen;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantIsolation.Constants;
using TenantIsolation.Configuration;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;

namespace TenantIsolation.Services;

/// <summary>
/// Resolves current tenant from HTTP request context using configurable strategy chain.
/// </summary>
public class TenantResolutionService : ITenantResolutionService
{
    // FrozenSet gives O(1) lookups with no per-call allocation; far faster than
    // repeated string equality chains when the reserved list grows.
    private static readonly FrozenSet<string> ReservedSubdomains = FrozenSet.Create(StringComparer.OrdinalIgnoreCase,
        "www", "api", "mail", "smtp", "ftp", "admin", "app",
        "static", "cdn", "assets", "dev", "staging", "prod", "auth", "login");

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDynamicTenantStore _dynamicTenantStore;
    private readonly ILogger<TenantResolutionService> _logger;
    private readonly TenantResolutionOptions _options;

    // Track the strategy used for the current resolution
    private TenantResolutionStrategy? _resolvedStrategy;

    public TenantResolutionService(
        IHttpContextAccessor httpContextAccessor,
        IDynamicTenantStore dynamicTenantStore,
        ILogger<TenantResolutionService> logger,
        IOptions<TenantResolutionOptions> options)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _dynamicTenantStore = dynamicTenantStore ?? throw new ArgumentNullException(nameof(dynamicTenantStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();

        _logger.LogDebug("TenantResolutionService initialized with {Count} strategies: {Strategies}",
            _options.ResolutionStrategies.Count,
            string.Join(", ", _options.ResolutionStrategies));
    }

    /// <summary>
    /// Resolve tenant from current HTTP request using configured strategy chain.
    /// </summary>
    public async Task<Tenant> ResolveTenantAsync()
    {
        var result = await ResolveTenantWithStrategyAsync();
        if (!result.Success)
        {
            throw new TenantNotResolvedException("Unable to resolve tenant from request");
        }

        return result.Tenant!;
    }

    /// <summary>
    /// Resolve tenant from current HTTP request with strategy information.
    /// </summary>
    public async Task<TenantResolutionResult> ResolveTenantWithStrategyAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogError("HTTP context not available");
            return new TenantResolutionResult();
        }

        // Try to get from HTTP context items (cached)
        if (httpContext.Items.TryGetValue(TenantConstants.CurrentTenantContextKey, out var cachedTenant))
        {
            var tenant = (Tenant)cachedTenant;
            TenantResolutionStrategy? cachedStrategy = null;
            if (httpContext.Items.TryGetValue(TenantConstants.ResolvedStrategyContextKey, out var cachedStrategyValue))
            {
                cachedStrategy = (TenantResolutionStrategy)cachedStrategyValue!;
            }

            _logger.LogDebug("Tenant {TenantId} resolved from HttpContext cache using strategy: {Strategy}",
                tenant.Id, cachedStrategy);
            if (cachedStrategy.HasValue)
            {
                return new TenantResolutionResult(tenant, cachedStrategy.Value);
            }
            return new TenantResolutionResult(tenant, TenantResolutionStrategy.Default);
        }

        // Try strategies in configured order
        foreach (var strategy in _options.ResolutionStrategies)
        {
            Tenant? tenant = null;

            try
            {
                switch (strategy)
                {
                    case TenantResolutionStrategy.Header:
                        tenant = await ResolveTenantFromHeaderAsync(httpContext);
                        break;
                    case TenantResolutionStrategy.Subdomain:
                        tenant = await ResolveTenantFromSubdomainAsync(httpContext);
                        break;
                    case TenantResolutionStrategy.QueryString:
                        tenant = await ResolveTenantFromQueryStringAsync(httpContext);
                        break;
                    case TenantResolutionStrategy.Route:
                        tenant = await ResolveTenantFromRouteAsync(httpContext);
                        break;
                    case TenantResolutionStrategy.Claims:
                        tenant = await ResolveTenantFromClaimsAsync(httpContext);
                        break;
                    case TenantResolutionStrategy.Default:
                        tenant = await ResolveTenantFromDefaultAsync(httpContext);
                        break;
                    default:
                        _logger.LogWarning("Unknown tenant resolution strategy: {Strategy}", strategy);
                        continue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving tenant using strategy {Strategy}", strategy);
                continue;
            }

            if (tenant != null)
            {
                // Validate tenant is active
                if (tenant.Status != TenantStatus.Active)
                {
                    _logger.LogWarning("Tenant {TenantId} is not active (status: {Status})", tenant.Id, tenant.Status);
                    if (_options.ThrowOnResolutionFailure)
                    {
                        throw new TenantNotActiveException(tenant.Id, $"Tenant is {tenant.Status}");
                    }
                    return new TenantResolutionResult();
                }

                // Cache in context for subsequent access within the same request
                httpContext.Items[TenantConstants.CurrentTenantContextKey] = tenant;
                httpContext.Items[TenantConstants.ResolvedStrategyContextKey] = strategy;
                _resolvedStrategy = strategy;

                _logger.LogInformation("Resolved tenant {TenantId} from request using strategy: {Strategy}",
                    tenant.Id, strategy);
                return new TenantResolutionResult(tenant, strategy);
            }
        }

        _logger.LogWarning(
            "All tenant resolution strategies exhausted for {Method} {Path} from {RemoteIp}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.Connection.RemoteIpAddress);

        if (_options.ThrowOnResolutionFailure)
        {
            throw new TenantNotResolvedException("Unable to resolve tenant from request");
        }

        return new TenantResolutionResult();
    }

    /// <summary>
    /// Get the strategy that was used to resolve the current tenant.
    /// Returns null if no tenant has been resolved yet.
    /// </summary>
    public TenantResolutionStrategy? GetResolvedStrategy()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        // Check if already resolved
        if (httpContext.Items.TryGetValue(TenantConstants.ResolvedStrategyContextKey, out var strategyValue))
        {
            return (TenantResolutionStrategy)strategyValue!;
        }

        return null;
    }

    /// <summary>
    /// Get current tenant from context. This method reads from HttpContext.Items directly.
    /// </summary>
    public Tenant? GetCurrentTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        if (httpContext.Items.TryGetValue(TenantConstants.CurrentTenantContextKey, out var tenant))
        {
            return (Tenant)tenant;
        }

        return null;
    }

    public Guid? GetCurrentTenantId() => GetCurrentTenant()?.Id;

    public bool HasTenant() => GetCurrentTenant() != null;

    /// <summary>
    /// Resolve tenant from HTTP header.
    /// </summary>
    private async Task<Tenant?> ResolveTenantFromHeaderAsync(HttpContext httpContext)
    {
        _logger.LogDebug("Attempting to resolve tenant from header using strategies: X-Tenant-Id, X-Tenant-Slug");

        if (httpContext.Request.Headers.TryGetValue(TenantConstants.TenantIdHeader, out var tenantIdValue))
        {
            if (Guid.TryParse(tenantIdValue.ToString(), out var tenantId))
            {
                var tenant = await _dynamicTenantStore.GetTenantByIdAsync(tenantId);
                if (tenant != null)
                {
                    _logger.LogDebug("Tenant found from header ID: {TenantId}", tenantId);
                    return tenant;
                }
                _logger.LogWarning("Tenant not found from header ID: {TenantId}", tenantId);
            }
        }

        if (httpContext.Request.Headers.TryGetValue(TenantConstants.TenantSlugHeader, out var slugValue))
        {
            var tenants = await _dynamicTenantStore.GetAllActiveTenantsAsync();
            var tenant = tenants.FirstOrDefault(t => t.Slug.Equals(slugValue.ToString(), StringComparison.OrdinalIgnoreCase));
            if (tenant != null)
            {
                _logger.LogDebug("Tenant found from header slug: {Slug}", slugValue.ToString());
                return tenant;
            }
            _logger.LogWarning("Tenant not found from header slug: {Slug}", slugValue.ToString());
        }

        return null;
    }

    /// <summary>
    /// Resolve tenant from query string (?tenantId=... or ?tenantSlug=...)
    /// </summary>
    private async Task<Tenant?> ResolveTenantFromQueryStringAsync(HttpContext httpContext)
    {
        _logger.LogDebug("Attempting to resolve tenant from query string using parameters: tenantId, tenantSlug");

        if (httpContext.Request.Query.TryGetValue("tenantId", out var tenantIdValue))
        {
            if (Guid.TryParse(tenantIdValue.ToString(), out var tenantId))
            {
                var tenant = await _dynamicTenantStore.GetTenantByIdAsync(tenantId);
                if (tenant != null)
                {
                    _logger.LogDebug("Tenant found from query string ID: {TenantId}", tenantId);
                    return tenant;
                }
                _logger.LogWarning("Tenant not found from query string ID: {TenantId}", tenantId);
            }
        }

        if (httpContext.Request.Query.TryGetValue("tenantSlug", out var slugValue))
        {
            var tenants = await _dynamicTenantStore.GetAllActiveTenantsAsync();
            var tenant = tenants.FirstOrDefault(t => t.Slug.Equals(slugValue.ToString(), StringComparison.OrdinalIgnoreCase));
            if (tenant != null)
            {
                _logger.LogDebug("Tenant found from query string slug: {Slug}", slugValue.ToString());
                return tenant;
            }
            _logger.LogWarning("Tenant not found from query string slug: {Slug}", slugValue.ToString());
        }

        return null;
    }

    /// <summary>
    /// Resolve tenant from route parameter.
    /// </summary>
    private async Task<Tenant?> ResolveTenantFromRouteAsync(HttpContext httpContext)
    {
        _logger.LogDebug("Attempting to resolve tenant from route parameters: {TenantRouteParameter}, {TenantSlugRouteParameter}",
            TenantConstants.TenantRouteParameter, TenantConstants.TenantSlugRouteParameter);

        var routeData = httpContext.GetRouteData();
        if (routeData == null)
        {
            return null;
        }

        if (routeData.Values.TryGetValue(TenantConstants.TenantRouteParameter, out var tenantIdValue))
        {
            if (Guid.TryParse(tenantIdValue?.ToString(), out var tenantId))
            {
                var tenant = await _dynamicTenantStore.GetTenantByIdAsync(tenantId);
                if (tenant != null)
                {
                    _logger.LogDebug("Tenant found from route ID: {TenantId}", tenantId);
                    return tenant;
                }
                _logger.LogWarning("Tenant not found from route ID: {TenantId}", tenantId);
            }
        }

        if (routeData.Values.TryGetValue(TenantConstants.TenantSlugRouteParameter, out var slugValue))
        {
            var tenants = await _dynamicTenantStore.GetAllActiveTenantsAsync();
            var tenant = tenants.FirstOrDefault(t => t.Slug.Equals(slugValue?.ToString() ?? "", StringComparison.OrdinalIgnoreCase));
            if (tenant != null)
            {
                _logger.LogDebug("Tenant found from route slug: {Slug}", slugValue);
                return tenant;
            }
            _logger.LogWarning("Tenant not found from route slug: {Slug}", slugValue);
        }

        return null;
    }

    /// <summary>
    /// Resolve tenant from user claims.
    /// </summary>
    private async Task<Tenant?> ResolveTenantFromClaimsAsync(HttpContext httpContext)
    {
        _logger.LogDebug("Attempting to resolve tenant from claims using claim types: {TenantIdClaimType}, {TenantSlugClaimType}",
            TenantConstants.TenantIdClaimType, TenantConstants.TenantSlugClaimType);

        var user = httpContext.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            _logger.LogDebug("User not authenticated, cannot resolve tenant from claims");
            return null;
        }

        var tenantIdClaim = user.FindFirst(TenantConstants.TenantIdClaimType);
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            var tenant = await _dynamicTenantStore.GetTenantByIdAsync(tenantId);
            if (tenant != null)
            {
                _logger.LogDebug("Tenant found from claim ID: {TenantId}", tenantId);
                return tenant;
            }
            _logger.LogWarning("Tenant not found from claim ID: {TenantId}", tenantId);
        }

        var slugClaim = user.FindFirst(TenantConstants.TenantSlugClaimType);
        if (slugClaim != null)
        {
            var tenants = await _dynamicTenantStore.GetAllActiveTenantsAsync();
            var tenant = tenants.FirstOrDefault(t => t.Slug.Equals(slugClaim.Value, StringComparison.OrdinalIgnoreCase));
            if (tenant != null)
            {
                _logger.LogDebug("Tenant found from claim slug: {Slug}", slugClaim.Value);
                return tenant;
            }
            _logger.LogWarning("Tenant not found from claim slug: {Slug}", slugClaim.Value);
        }

        return null;
    }

    /// <summary>
    /// Resolve tenant from subdomain.
    /// Uses IndexOf instead of Split to avoid allocating a string array per request.
    /// </summary>
    private async Task<Tenant?> ResolveTenantFromSubdomainAsync(HttpContext httpContext)
    {
        _logger.LogDebug("Attempting to resolve tenant from subdomain");

        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrEmpty(host))
        {
            _logger.LogDebug("No host available in request");
            return null;
        }

        // Extract subdomain without allocating a string[] via Split.
        var dotIndex = host.IndexOf('.');
        if (dotIndex <= 0 || dotIndex == host.Length - 1)
        {
            _logger.LogDebug("No valid subdomain found in host: {Host}", host);
            return null;
        }

        var subdomain = host[..dotIndex];
        if (ReservedSubdomains.Contains(subdomain))
        {
            _logger.LogDebug("Subdomain is reserved: {Subdomain}", subdomain);
            return null;
        }

        var tenants = await _dynamicTenantStore.GetAllActiveTenantsAsync();
        var tenant = tenants.FirstOrDefault(t => t.Slug.Equals(subdomain, StringComparison.OrdinalIgnoreCase));
        if (tenant != null)
        {
            _logger.LogDebug("Tenant found from subdomain: {Subdomain}", subdomain);
            return tenant;
        }

        _logger.LogWarning("Tenant not found from subdomain: {Subdomain}", subdomain);
        return null;
    }

    /// <summary>
    /// Resolve tenant from default configuration.
    /// </summary>
    private async Task<Tenant?> ResolveTenantFromDefaultAsync(HttpContext httpContext)
    {
        _logger.LogDebug("Attempting to resolve tenant from default configuration");

        if (_options.DefaultTenantId.HasValue)
        {
            var tenant = await _dynamicTenantStore.GetTenantByIdAsync(_options.DefaultTenantId.Value);
            if (tenant != null)
            {
                _logger.LogDebug("Tenant found from default ID: {TenantId}", _options.DefaultTenantId.Value);
                return tenant;
            }
            _logger.LogWarning("Default tenant not found with ID: {TenantId}", _options.DefaultTenantId.Value);
        }

        if (!string.IsNullOrEmpty(_options.DefaultTenantSlug))
        {
            var tenants = await _dynamicTenantStore.GetAllActiveTenantsAsync();
            var tenant = tenants.FirstOrDefault(t => t.Slug.Equals(_options.DefaultTenantSlug, StringComparison.OrdinalIgnoreCase));
            if (tenant != null)
            {
                _logger.LogDebug("Tenant found from default slug: {Slug}", _options.DefaultTenantSlug);
                return tenant;
            }
            _logger.LogWarning("Default tenant not found with slug: {Slug}", _options.DefaultTenantSlug);
        }

        _logger.LogWarning("No default tenant configured");
        return null;
    }
}