#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using TenantIsolation.Constants;

namespace TenantIsolation.Configuration;

/// <summary>
/// Configuration options for tenant resolution strategies.
/// Controls the ordered chain of resolution strategies and default tenant behavior.
/// </summary>
public class TenantResolutionOptions
{
    /// <summary>
    /// Ordered list of tenant resolution strategies to try, in priority order.
    /// First successful strategy will be used to resolve the tenant.
    /// </summary>
    public List<TenantResolutionStrategy> ResolutionStrategies { get; set; } =
    [
        TenantResolutionStrategy.Subdomain,
        TenantResolutionStrategy.Header,
        TenantResolutionStrategy.QueryString,
        TenantResolutionStrategy.Route,
        TenantResolutionStrategy.Claims,
        TenantResolutionStrategy.Default
    ];

    /// <summary>
    /// Default tenant ID to use when Default strategy is selected.
    /// If null, resolution will fail when no other strategy succeeds.
    /// </summary>
    public Guid? DefaultTenantId { get; set; }

    /// <summary>
    /// Default tenant slug to use when Default strategy is selected.
    /// If null, resolution will fail when no other strategy succeeds.
    /// </summary>
    public string? DefaultTenantSlug { get; set; }

    /// <summary>
    /// Whether to throw an exception when tenant resolution fails.
    /// When false, returns null instead of throwing TenantNotResolvedException.
    /// </summary>
    public bool ThrowOnResolutionFailure { get; set; } = true;

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if configuration is invalid</exception>
    public void Validate()
    {
        if (DefaultTenantId.HasValue && DefaultTenantSlug != null)
        {
            if (DefaultTenantSlug != DefaultTenantId.Value.ToString())
            {
                throw new InvalidOperationException(
                    "DefaultTenantSlug must match the slug representation of DefaultTenantId, or be null");
            }
        }

        if (ResolutionStrategies.Count == 0)
        {
            throw new InvalidOperationException("At least one resolution strategy must be configured");
        }

        // Validate that Default strategy is only used as last resort
        var hasDefaultStrategy = ResolutionStrategies.Contains(TenantResolutionStrategy.Default);
        var defaultStrategies = new[] { TenantResolutionStrategy.Default };

        if (hasDefaultStrategy && ResolutionStrategies.Last() != TenantResolutionStrategy.Default)
        {
            throw new InvalidOperationException(
                "TenantResolutionStrategy.Default must be the last strategy in the resolution chain");
        }
    }

    /// <summary>
    /// Creates default tenant resolution options with the recommended strategy order.
    /// </summary>
    public static TenantResolutionOptions CreateDefault()
    {
        return new TenantResolutionOptions
        {
            ResolutionStrategies = new List<TenantResolutionStrategy>
            {
                TenantResolutionStrategy.Subdomain,
                TenantResolutionStrategy.Header,
                TenantResolutionStrategy.QueryString,
                TenantResolutionStrategy.Route,
                TenantResolutionStrategy.Claims,
                TenantResolutionStrategy.Default
            },
            DefaultTenantId = null,
            DefaultTenantSlug = null,
            ThrowOnResolutionFailure = true
        };
    }
}