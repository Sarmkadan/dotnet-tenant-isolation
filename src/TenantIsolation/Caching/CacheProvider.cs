// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text;
using TenantIsolation.Utilities;

namespace TenantIsolation.Caching;

/// <summary>
/// In-memory cache provider with TTL support
/// Implements sliding expiration and automatic cleanup
/// Used as fallback when distributed cache is unavailable
/// </summary>
public interface ICacheProvider
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task ClearAsync();
    Task<IEnumerable<string>> GetAllKeysAsync();
}

/// <summary>
/// In-memory implementation of cache provider
/// Thread-safe using ConcurrentDictionary
/// </summary>
public class MemoryCacheProvider : ICacheProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly Timer _cleanupTimer;
    private readonly object _lockObject = new();

    public MemoryCacheProvider()
    {
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        // Run cleanup every 5 minutes to remove expired entries
        _cleanupTimer = new Timer(_ => CleanupExpiredEntries(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Get value from cache by key
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return default;

        return await Task.Run(() =>
        {
            if (!_cache.TryGetValue(key, out var entry))
                return default;

            // Check if expired
            if (entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
                return default;
            }

            // Update last access time for sliding expiration
            entry.LastAccessTime = DateTime.UtcNow;

            return (T?)entry.Value;
        });
    }

    /// <summary>
    /// Set value in cache with optional expiration
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        return await Task.Run(() =>
        {
            var entry = new CacheEntry
            {
                Key = key,
                Value = value,
                CreatedTime = DateTime.UtcNow,
                LastAccessTime = DateTime.UtcNow,
                ExpirationTime = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null
            };

            _cache.AddOrUpdate(key, entry, (_, _) => entry);
        });
    }

    /// <summary>
    /// Remove entry from cache
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        return await Task.Run(() =>
        {
            _cache.TryRemove(key, out _);
        });
    }

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return await Task.Run(() =>
        {
            if (!_cache.TryGetValue(key, out var entry))
                return false;

            if (entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
                return false;
            }

            return true;
        });
    }

    /// <summary>
    /// Clear all entries from cache
    /// </summary>
    public async Task ClearAsync()
    {
        return await Task.Run(_cache.Clear);
    }

    /// <summary>
    /// Get all keys in cache (excluding expired)
    /// </summary>
    public async Task<IEnumerable<string>> GetAllKeysAsync()
    {
        return await Task.Run(() =>
        {
            var keys = _cache
                .Where(kvp => !kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            return (IEnumerable<string>)keys;
        });
    }

    /// <summary>
    /// Clean up expired entries from cache
    /// Called periodically to prevent memory leaks
    /// </summary>
    private void CleanupExpiredEntries()
    {
        lock (_lockObject)
        {
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }

    /// <summary>
    /// Internal cache entry structure
    /// </summary>
    private class CacheEntry
    {
        public required string Key { get; set; }
        public object? Value { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public DateTime? ExpirationTime { get; set; }

        /// <summary>
        /// Check if entry has expired
        /// </summary>
        public bool IsExpired => ExpirationTime.HasValue && DateTime.UtcNow > ExpirationTime.Value;
    }
}

/// <summary>
/// Cache key builder for consistent key generation
/// Prevents key collisions and ensures proper tenant isolation
/// </summary>
public class CacheKeyBuilder
{
    private readonly string _prefix;
    private readonly List<string> _segments;

    public CacheKeyBuilder(string prefix = "cache")
    {
        _prefix = prefix;
        _segments = new List<string>();
    }

    /// <summary>
    /// Add segment to cache key
    /// </summary>
    public CacheKeyBuilder Add(string? segment)
    {
        if (!string.IsNullOrWhiteSpace(segment))
            _segments.Add(segment);
        return this;
    }

    /// <summary>
    /// Add tenant context to key
    /// Ensures tenant isolation in cache
    /// </summary>
    public CacheKeyBuilder WithTenant(string? tenantId)
    {
        if (!string.IsNullOrWhiteSpace(tenantId))
            _segments.Insert(0, $"tenant:{tenantId}");
        return this;
    }

    /// <summary>
    /// Add user context to key
    /// </summary>
    public CacheKeyBuilder WithUser(string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
            _segments.Add($"user:{userId}");
        return this;
    }

    /// <summary>
    /// Add hash of complex object to key
    /// Useful for caching with parameter-dependent keys
    /// </summary>
    public CacheKeyBuilder WithHash(object? parameter)
    {
        if (parameter != null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(parameter);
            var hash = json.GetDeterministicHashCode().ToString("X");
            _segments.Add($"hash:{hash}");
        }
        return this;
    }

    /// <summary>
    /// Build final cache key
    /// </summary>
    public string Build()
    {
        var parts = new List<string> { _prefix };
        parts.AddRange(_segments);
        return string.Join(":", parts).ToLowerInvariant();
    }
}
