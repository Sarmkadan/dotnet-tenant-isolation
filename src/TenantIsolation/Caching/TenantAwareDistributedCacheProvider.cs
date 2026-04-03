#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TenantIsolation.Constants; // Assuming TenantConstants defines "TenantId" key

namespace TenantIsolation.Caching;

/// <summary>
/// A tenant-aware cache provider that wraps IDistributedCache, automatically
/// prefixing cache keys with the current tenant's identifier to ensure data isolation.
/// </summary>
public interface ITenantAwareDistributedCacheProvider : ICacheProvider { }

public class TenantAwareDistributedCacheProvider : ITenantAwareDistributedCacheProvider, IDisposable
{
    private readonly IDistributedCache _distributedCache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantAwareDistributedCacheProvider> _logger;

    public TenantAwareDistributedCacheProvider(
        IDistributedCache distributedCache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TenantAwareDistributedCacheProvider> logger)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the tenant-prefixed cache key. If no tenant is resolved, the original key is used.
    /// </summary>
    /// <param name="key">The original cache key.</param>
    /// <returns>The tenant-prefixed key or the original key if no tenant is found.</returns>
    private string GetTenantPrefixedKey(string key)
    {
        var tenantId = _httpContextAccessor.HttpContext?.Items[TenantConstants.CurrentTenantContextKey]?.ToString();

        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("No tenant ID found in HttpContext for key '{Key}'. Using original key, which might lead to cross-tenant data leakage if not intended.", key);
            return key; // Fallback to original key if tenantId is not available
        }

        // Using string.Concat to avoid intermediate string allocations.
        return string.Concat(tenantId, ":", key);
    }

    public async ValueTask<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Attempted to get a value with a null or whitespace cache key.");
            return default;
        }

        var prefixedKey = GetTenantPrefixedKey(key);
        try
        {
            var bytes = await _distributedCache.GetAsync(prefixedKey);
            if (bytes == null)
            {
                _logger.LogDebug("Cache miss for key '{PrefixedKey}' (original: '{OriginalKey}')", prefixedKey, key);
                return default;
            }

            var value = System.Text.Json.JsonSerializer.Deserialize<T>(bytes);
            _logger.LogDebug("Cache hit for key '{PrefixedKey}' (original: '{OriginalKey}')", prefixedKey, key);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value for cache key '{PrefixedKey}' (original: '{OriginalKey}') from distributed cache.", prefixedKey, key);
            return default;
        }
    }

    public async ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Attempted to set a value with a null or whitespace cache key.");
            return;
        }

        if (value == null)
        {
            _logger.LogWarning("Attempted to set a null value for cache key '{Key}'. This will result in removal.", key);
            await RemoveAsync(key);
            return;
        }

        var prefixedKey = GetTenantPrefixedKey(key);
        try
        {
            var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value);
            var options = expiration.HasValue
                ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration }
                : new DistributedCacheEntryOptions(); // No explicit expiration means it uses default/never expires

            await _distributedCache.SetAsync(prefixedKey, bytes, options);
            _logger.LogDebug("Set cache value for key '{PrefixedKey}' (original: '{OriginalKey}') with expiration {Expiration}", prefixedKey, key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for cache key '{PrefixedKey}' (original: '{OriginalKey}') to distributed cache.", prefixedKey, key);
        }
    }

    public async ValueTask RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Attempted to remove a value with a null or whitespace cache key.");
            return;
        }

        var prefixedKey = GetTenantPrefixedKey(key);
        try
        {
            await _distributedCache.RemoveAsync(prefixedKey);
            _logger.LogDebug("Removed cache entry for key '{PrefixedKey}' (original: '{OriginalKey}')", prefixedKey, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value for cache key '{PrefixedKey}' (original: '{OriginalKey}') from distributed cache.", prefixedKey, key);
        }
    }

    public ValueTask<bool> ExistsAsync(string key)
    {
        // IDistributedCache doesn't have an Exists method. A 'Get' followed by a null check is the common approach.
        // This might retrieve the entire value, which could be inefficient for large objects.
        // For simplicity, we'll use GetAsync for now.
        _logger.LogWarning("ExistsAsync for distributed cache will perform a Get operation, which might be inefficient for large objects.");
        return (GetAsync<object>(key).AsTask().Result != null)
            ? ValueTask.FromResult(true)
            : ValueTask.FromResult(false);
    }

    public async ValueTask ClearAsync()
    {
        _logger.LogWarning("ClearAsync is not supported for tenant-aware distributed cache as it would clear ALL keys regardless of tenant prefix. This operation is a NO-OP.");
        await Task.CompletedTask; // DistributedCache doesn't provide a way to clear all tenant-specific keys efficiently without knowing all prefixes.
    }

    public ValueTask<IEnumerable<string>> GetAllKeysAsync()
    {
        _logger.LogWarning("GetAllKeysAsync is not supported for distributed cache as it would require iterating over all keys, which is generally inefficient and not practical for distributed systems. Returning an empty list.");
        return ValueTask.FromResult(Enumerable.Empty<string>());
    }

    public void Dispose()
    {
        // No managed resources to dispose for IDistributedCache itself here
    }
}
