#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TenantIsolation.Constants;
using TenantIsolation.Data;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;

namespace TenantIsolation.Services;

/// <summary>
/// Service for managing tenant feature toggles
/// </summary>
public class TenantFeatureService
{
    private readonly TenantDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TenantFeatureService> _logger;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

    public TenantFeatureService(TenantDbContext context, IMemoryCache cache, ILogger<TenantFeatureService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Check if feature is enabled for tenant
    /// </summary>
    public async Task<bool> IsFeatureEnabledAsync(Guid tenantId, string featureKey)
    {
        var feature = await GetFeatureAsync(tenantId, featureKey);
        if (feature == null)
            return false;

        return feature.IsAvailable();
    }

    /// <summary>
    /// Get feature details
    /// </summary>
    public async Task<TenantFeature?> GetFeatureAsync(Guid tenantId, string featureKey)
    {
        var cacheKey = $"feature_{tenantId}_{featureKey}";

        if (_cache.TryGetValue(cacheKey, out TenantFeature? cached))
            return cached;

        var feature = await _context.TenantFeatures
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.FeatureKey == featureKey);

        if (feature != null)
            _cache.Set(cacheKey, feature, _cacheExpiry);

        return feature;
    }

    /// <summary>
    /// Enable feature for tenant
    /// </summary>
    public async Task<TenantFeature> EnableFeatureAsync(
        Guid tenantId,
        string featureKey,
        int? rolloutPercentage = null)
    {
        var feature = await GetFeatureAsync(tenantId, featureKey);

        if (feature == null)
        {
            feature = new TenantFeature
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FeatureKey = featureKey,
                IsEnabled = true,
                RolloutPercentage = rolloutPercentage ?? 100
            };

            await _context.TenantFeatures.AddAsync(feature);
        }
        else
        {
            feature.IsEnabled = true;
            if (rolloutPercentage.HasValue)
                feature.RolloutPercentage = rolloutPercentage.Value;

            _context.TenantFeatures.Update(feature);
        }

        feature.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        InvalidateCache(tenantId, featureKey);

        _logger.LogInformation("Feature {FeatureKey} enabled for tenant {TenantId}",
            featureKey, tenantId);

        return feature;
    }

    /// <summary>
    /// Disable feature for tenant
    /// </summary>
    public async Task<bool> DisableFeatureAsync(Guid tenantId, string featureKey)
    {
        var feature = await GetFeatureAsync(tenantId, featureKey);
        if (feature == null)
            return false;

        feature.IsEnabled = false;
        feature.UpdatedAt = DateTime.UtcNow;
        _context.TenantFeatures.Update(feature);
        await _context.SaveChangesAsync();

        InvalidateCache(tenantId, featureKey);

        _logger.LogInformation("Feature {FeatureKey} disabled for tenant {TenantId}",
            featureKey, tenantId);

        return true;
    }

    /// <summary>
    /// Set feature rollout percentage
    /// </summary>
    public async Task<bool> SetRolloutPercentageAsync(Guid tenantId, string featureKey, int percentage)
    {
        if (percentage < 0 || percentage > 100)
            throw new TenantIsolationException("Rollout percentage must be between 0 and 100");

        var feature = await GetFeatureAsync(tenantId, featureKey);
        if (feature == null)
            return false;

        feature.RolloutPercentage = percentage;
        feature.UpdatedAt = DateTime.UtcNow;
        _context.TenantFeatures.Update(feature);
        await _context.SaveChangesAsync();

        InvalidateCache(tenantId, featureKey);

        return true;
    }

    /// <summary>
    /// Get all enabled features for tenant
    /// </summary>
    public async Task<List<TenantFeature>> GetEnabledFeaturesAsync(Guid tenantId)
    {
        var cacheKey = $"features_enabled_{tenantId}";

        if (_cache.TryGetValue(cacheKey, out List<TenantFeature>? cached))
            return cached;

        var features = await _context.TenantFeatures
            .Where(f => f.TenantId == tenantId && f.IsEnabled)
            .OrderBy(f => f.FeatureKey)
            .ToListAsync();

        _cache.Set(cacheKey, features, _cacheExpiry);

        return features;
    }

    /// <summary>
    /// Get all features for tenant
    /// </summary>
    public async Task<List<TenantFeature>> GetAllFeaturesAsync(Guid tenantId)
    {
        var cacheKey = $"features_all_{tenantId}";

        if (_cache.TryGetValue(cacheKey, out List<TenantFeature>? cached))
            return cached;

        var features = await _context.TenantFeatures
            .Where(f => f.TenantId == tenantId)
            .OrderBy(f => f.FeatureKey)
            .ToListAsync();

        _cache.Set(cacheKey, features, _cacheExpiry);

        return features;
    }

    /// <summary>
    /// Get features by category
    /// </summary>
    public async Task<List<TenantFeature>> GetFeaturesByCategoryAsync(Guid tenantId, string category)
    {
        return await _context.TenantFeatures
            .Where(f => f.TenantId == tenantId && f.Category == category)
            .OrderBy(f => f.FeatureKey)
            .ToListAsync();
    }

    /// <summary>
    /// Record feature usage
    /// </summary>
    public async Task<bool> RecordFeatureUsageAsync(Guid tenantId, string featureKey, long amount = 1)
    {
        var feature = await GetFeatureAsync(tenantId, featureKey);
        if (feature == null)
            return false;

        if (feature.IsUsageLimitExceeded())
            throw new TenantIsolationException(
                $"Usage limit of {feature.UsageLimit} exceeded for feature {featureKey}");

        feature.RecordUsage(amount);
        _context.TenantFeatures.Update(feature);
        await _context.SaveChangesAsync();

        InvalidateCache(tenantId, featureKey);

        return true;
    }

    /// <summary>
    /// Reset feature usage
    /// </summary>
    public async Task<bool> ResetFeatureUsageAsync(Guid tenantId, string featureKey)
    {
        var feature = await GetFeatureAsync(tenantId, featureKey);
        if (feature == null)
            return false;

        feature.ResetUsage();
        _context.TenantFeatures.Update(feature);
        await _context.SaveChangesAsync();

        InvalidateCache(tenantId, featureKey);

        return true;
    }

    /// <summary>
    /// Check usage limit
    /// </summary>
    public async Task<bool> CheckUsageLimitAsync(Guid tenantId, string featureKey)
    {
        var feature = await GetFeatureAsync(tenantId, featureKey);
        if (feature == null)
            return false;

        return !feature.IsUsageLimitExceeded();
    }

    /// <summary>
    /// Initialize default features for tenant
    /// </summary>
    public async Task InitializeDefaultFeaturesAsync(Guid tenantId)
    {
        var defaultFeatures = new[]
        {
            TenantFeatureFlags.MultiTenancy,
            TenantFeatureFlags.AdvancedSecurity,
            TenantFeatureFlags.CustomBranding,
            TenantFeatureFlags.DataRetention,
            TenantFeatureFlags.ApiAccess
        };

        foreach (var featureKey in defaultFeatures)
        {
            var existing = await GetFeatureAsync(tenantId, featureKey);
            if (existing == null)
            {
                var feature = new TenantFeature
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FeatureKey = featureKey,
                    IsEnabled = true,
                    RolloutPercentage = 100
                };

                await _context.TenantFeatures.AddAsync(feature);
            }
        }

        await _context.SaveChangesAsync();
        InvalidateCache(tenantId);

        _logger.LogInformation("Initialized default features for tenant {TenantId}", tenantId);
    }

    /// <summary>
    /// Get feature statistics
    /// </summary>
    public async Task<object> GetStatisticsAsync(Guid tenantId)
    {
        var features = await GetAllFeaturesAsync(tenantId);

        return new
        {
            TotalFeatures = features.Count,
            EnabledFeatures = features.Count(f => f.IsEnabled),
            DisabledFeatures = features.Count(f => !f.IsEnabled),
            FeaturesWithUsageLimits = features.Count(f => f.UsageLimit.HasValue),
            BetaFeatures = features.Count(f => f.AvailabilityLevel == "Beta"),
            DeprecatedFeatures = features.Count(f => f.DeprecatedAt.HasValue),
            AverageRollout = features.Where(f => f.IsEnabled).Average(f => f.RolloutPercentage)
        };
    }

    /// <summary>
    /// Invalidate cache
    /// </summary>
    private void InvalidateCache(Guid tenantId, string? featureKey = null)
    {
        if (featureKey != null)
            _cache.Remove($"feature_{tenantId}_{featureKey}");

        _cache.Remove($"features_enabled_{tenantId}");
        _cache.Remove($"features_all_{tenantId}");
    }

    /// <summary>
    /// Check if feature can be accessed
    /// </summary>
    public async Task<(bool canUse, string? errorMessage)> CanUseFeatureAsync(Guid tenantId, string featureKey)
    {
        var feature = await GetFeatureAsync(tenantId, featureKey);
        if (feature == null)
            return (false, "Feature not found");

        if (!feature.CanUseFeature(out var error))
            return (false, error);

        return (true, null);
    }

    /// <summary>
    /// Set feature rollout percentage for multiple tenants
    /// </summary>
    public async Task<Dictionary<Guid, bool>> SetBulkRolloutPercentageAsync(
        IEnumerable<Guid> tenantIds,
        string featureKey,
        int percentage,
        bool enableFeature = true)
    {
        if (percentage < 0 || percentage > 100)
            throw new TenantIsolationException("Rollout percentage must be between 0 and 100");

        var results = new Dictionary<Guid, bool>();

        foreach (var tenantId in tenantIds)
        {
            try
            {
                var feature = await GetFeatureAsync(tenantId, featureKey);

                if (feature == null)
                {
                    if (enableFeature)
                    {
                        feature = new TenantFeature
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            FeatureKey = featureKey,
                            IsEnabled = true,
                            RolloutPercentage = percentage
                        };

                        await _context.TenantFeatures.AddAsync(feature);
                        results[tenantId] = true;
                    }
                    else
                    {
                        results[tenantId] = false;
                    }
                }
                else
                {
                    feature.IsEnabled = enableFeature;
                    feature.RolloutPercentage = percentage;
                    feature.UpdatedAt = DateTime.UtcNow;
                    _context.TenantFeatures.Update(feature);
                    results[tenantId] = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting bulk rollout for tenant {TenantId} feature {FeatureKey}", tenantId, featureKey);
                results[tenantId] = false;
            }
        }

        if (results.Values.Any(r => r))
            await _context.SaveChangesAsync();

        InvalidateCacheBulk(tenantIds, featureKey);

        _logger.LogInformation("Bulk rollout set for feature {FeatureKey} across {Count} tenants", featureKey, tenantIds.Count());

        return results;
    }

    /// <summary>
    /// Enable/disable feature for multiple tenants
    /// </summary>
    public async Task<Dictionary<Guid, bool>> SetBulkFeatureStateAsync(
        IEnumerable<Guid> tenantIds,
        string featureKey,
        bool isEnabled)
    {
        var results = new Dictionary<Guid, bool>();

        foreach (var tenantId in tenantIds)
        {
            try
            {
                var feature = await GetFeatureAsync(tenantId, featureKey);

                if (feature == null)
                {
                    if (isEnabled)
                    {
                        feature = new TenantFeature
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            FeatureKey = featureKey,
                            IsEnabled = true,
                            RolloutPercentage = 100
                        };

                        await _context.TenantFeatures.AddAsync(feature);
                        results[tenantId] = true;
                    }
                    else
                    {
                        results[tenantId] = false;
                    }
                }
                else
                {
                    feature.IsEnabled = isEnabled;
                    feature.UpdatedAt = DateTime.UtcNow;
                    _context.TenantFeatures.Update(feature);
                    results[tenantId] = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting bulk feature state for tenant {TenantId} feature {FeatureKey}", tenantId, featureKey);
                results[tenantId] = false;
            }
        }

        if (results.Values.Any(r => r))
            await _context.SaveChangesAsync();

        InvalidateCacheBulk(tenantIds, featureKey);

        _logger.LogInformation("Bulk feature state set to {IsEnabled} for feature {FeatureKey} across {Count} tenants", isEnabled, featureKey, tenantIds.Count());

        return results;
    }

    /// <summary>
    /// Get feature status for multiple tenants
    /// </summary>
    public async Task<Dictionary<Guid, bool>> GetBulkFeatureStatusAsync(
        IEnumerable<Guid> tenantIds,
        string featureKey)
    {
        var results = new Dictionary<Guid, bool>();

        foreach (var tenantId in tenantIds)
        {
            try
            {
                var isEnabled = await IsFeatureEnabledAsync(tenantId, featureKey);
                results[tenantId] = isEnabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bulk feature status for tenant {TenantId} feature {FeatureKey}", tenantId, featureKey);
                results[tenantId] = false;
            }
        }

        return results;
    }

    /// <summary>
    /// Get features for multiple tenants
    /// </summary>
    public async Task<Dictionary<Guid, TenantFeature?>> GetBulkFeaturesAsync(
        IEnumerable<Guid> tenantIds,
        string featureKey)
    {
        var results = new Dictionary<Guid, TenantFeature?>();

        foreach (var tenantId in tenantIds)
        {
            try
            {
                var feature = await GetFeatureAsync(tenantId, featureKey);
                results[tenantId] = feature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bulk features for tenant {TenantId} feature {FeatureKey}", tenantId, featureKey);
                results[tenantId] = null;
            }
        }

        return results;
    }

    /// <summary>
    /// Invalidate cache for multiple tenants
    /// </summary>
    private void InvalidateCacheBulk(IEnumerable<Guid> tenantIds, string featureKey)
    {
        foreach (var tenantId in tenantIds)
        {
            _cache.Remove($"feature_{tenantId}_{featureKey}");
        }

        foreach (var tenantId in tenantIds)
        {
            _cache.Remove($"features_enabled_{tenantId}");
            _cache.Remove($"features_all_{tenantId}");
        }
    }
}
