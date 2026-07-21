#nullable enable

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TenantIsolation.Constants;
using TenantIsolation.Data;
using TenantIsolation.Models;
using Microsoft.Extensions.Options;
using TenantIsolation.Configuration;

namespace TenantIsolation.Services;

/// <summary>
/// A SQL-backed implementation of IDynamicTenantStore that provides and caches
/// tenant information and supports polling for updates.
/// </summary>
public class DynamicTenantStore : IDynamicTenantStore, IDisposable
{
    private readonly TenantRepository _tenantRepository;
    private readonly ILogger<DynamicTenantStore> _logger;
    private volatile ConcurrentDictionary<Guid, Tenant> _tenantCache = new();
    private Timer? _reloadTimer;
    private readonly TenantIsolationOptions _options;
    private readonly object _lock = new object();

    public event EventHandler<TenantEventArgs>? OnTenantRegistered;
    public event EventHandler<TenantEventArgs>? OnTenantRemoved;

    public DynamicTenantStore(
        TenantRepository tenantRepository,
        ILogger<DynamicTenantStore> logger,
        IOptions<TenantIsolationOptions> options)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
        _options = options.Value;

        // Initial load of tenants
        LoadTenantsAsync().Wait();
    }

    /// <summary>
    /// Starts the background timer for periodically reloading tenant data.
    /// </summary>
    public void StartReloading()
    {
        if (_options.DynamicTenantStoreReloadIntervalMinutes <= 0)
        {
            _logger.LogInformation("DynamicTenantStore reloading is disabled as interval is set to 0 or less.");
            return;
        }

        var interval = TimeSpan.FromMinutes(_options.DynamicTenantStoreReloadIntervalMinutes);
        _reloadTimer = new Timer(async _ => await ReloadTenantsAsync(), null, TimeSpan.Zero, interval);
        _logger.LogInformation("DynamicTenantStore started with a reload interval of {Interval}", interval);
    }

    /// <summary>
    /// Stops the background timer for periodically reloading tenant data.
    /// </summary>
    public void StopReloading()
    {
        _reloadTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _logger.LogInformation("DynamicTenantStore stopped.");
    }

    public Task<IEnumerable<Tenant>> GetAllActiveTenantsAsync()
    {
        return Task.FromResult<IEnumerable<Tenant>>(_tenantCache.Values.Where(t => t.Status == TenantStatus.Active));
    }

    public Task<Tenant?> GetTenantByIdAsync(Guid tenantId)
    {
        _tenantCache.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(tenant);
    }

    /// <summary>
    /// Loads all active tenants from the database into the cache.
    /// </summary>
    private async Task LoadTenantsAsync()
    {
        try
        {
            _logger.LogDebug("Loading tenants from database...");
            var tenants = await _tenantRepository.GetActiveTenantAsync();
            var newCache = new ConcurrentDictionary<Guid, Tenant>(tenants.ToDictionary(t => t.Id));
            _tenantCache = newCache;
            _logger.LogInformation("Successfully loaded {Count} tenants.", tenants.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tenants from database.");
        }
    }

    /// <summary>
    /// Reloads tenants from the database, compares with current cache, and raises events for changes.
    /// Ensures only one reload operation is active at a time.
    /// </summary>
    private async Task ReloadTenantsAsync()
    {
        // Use a lock to prevent multiple concurrent reloads if the timer fires quickly
        // or if a previous reload takes longer than the interval.
        if (!Monitor.TryEnter(_lock))
        {
            _logger.LogDebug("Skipping DynamicTenantStore reload as another reload is already in progress.");
            return;
        }

        try
        {
            _logger.LogDebug("Reloading tenants from database for dynamic store...");
            var latestTenants = await _tenantRepository.GetActiveTenantAsync();
            var latestTenantMap = latestTenants.ToDictionary(t => t.Id);

            var oldTenantIds = _tenantCache.Keys.ToHashSet();
            var newTenantIds = latestTenantMap.Keys.ToHashSet();

            // Build new cache snapshot atomically
            var removedTenants = new List<Tenant>();
            var addedOrUpdatedTenants = new List<Tenant>();

            // Find removed tenants
            foreach (var removedTenantId in oldTenantIds.Except(newTenantIds))
            {
                if (_tenantCache.TryGetValue(removedTenantId, out var removedTenant))
                {
                    removedTenants.Add(removedTenant);
                }
            }

            // Find new or updated tenants
            foreach (var latestTenant in latestTenants)
            {
                if (!_tenantCache.TryGetValue(latestTenant.Id, out var existingTenant) || !TenantsEqual(existingTenant, latestTenant))
                {
                    addedOrUpdatedTenants.Add(latestTenant);
                }
            }

            // Build new cache completely before swapping
            var newCache = new ConcurrentDictionary<Guid, Tenant>(_tenantCache);

            // Apply removals to new cache
            foreach (var removedTenant in removedTenants)
            {
                newCache.TryRemove(removedTenant.Id, out _);
            }

            // Apply additions/updates to new cache
            foreach (var tenant in addedOrUpdatedTenants)
            {
                newCache.AddOrUpdate(tenant.Id, tenant, (_, _) => tenant);
            }

            // Atomically swap the cache reference
            _tenantCache = newCache;

            // Raise events after the swap is complete
            foreach (var removedTenant in removedTenants)
            {
                _logger.LogInformation("Tenant {TenantId} ('{TenantSlug}') removed from dynamic store.", removedTenant.Id, removedTenant.Slug);
                OnTenantRemoved?.Invoke(this, new TenantEventArgs { Tenant = removedTenant });
            }

            foreach (var addedTenant in addedOrUpdatedTenants)
            {
                _logger.LogInformation("Tenant {TenantId} ('{TenantSlug}') added or updated in dynamic store.", addedTenant.Id, addedTenant.Slug);
                OnTenantRegistered?.Invoke(this, new TenantEventArgs { Tenant = addedTenant });
            }

            _logger.LogDebug("DynamicTenantStore reload completed. Current active tenants: {Count}", _tenantCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading tenants for dynamic store.");
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    /// <summary>
    /// Compares two tenant objects to check if they are effectively equal for caching purposes.
    /// </summary>
    private bool TenantsEqual(Tenant t1, Tenant t2)
    {
        // Simple comparison for essential properties.
        // Can be expanded to include more properties if their changes should trigger an update event.
        return t1.Id == t2.Id &&
               t1.Name == t2.Name &&
               t1.Slug == t2.Slug &&
               t1.Status == t2.Status &&
               t1.UpdatedAt == t2.UpdatedAt; // Check UpdateAt to detect changes
    }

    public void Dispose()
    {
        _reloadTimer?.Dispose();
    }
}
