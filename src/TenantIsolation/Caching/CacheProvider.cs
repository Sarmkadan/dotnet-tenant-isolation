// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Buffers;
using System.Collections.Concurrent;
using TenantIsolation.Utilities;

namespace TenantIsolation.Caching;

/// <summary>
/// In-memory cache provider with TTL support.
/// Implements sliding expiration and automatic cleanup.
/// Used as fallback when distributed cache is unavailable.
/// </summary>
public interface ICacheProvider
{
    ValueTask<T?> GetAsync<T>(string key);
    ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    ValueTask RemoveAsync(string key);
    ValueTask<bool> ExistsAsync(string key);
    ValueTask ClearAsync();
    ValueTask<IEnumerable<string>> GetAllKeysAsync();
}

/// <summary>
/// In-memory implementation of cache provider.
/// Thread-safe using ConcurrentDictionary; all operations complete synchronously
/// so every public method returns a pre-completed ValueTask with zero allocations.
/// </summary>
public class MemoryCacheProvider : ICacheProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly Timer _cleanupTimer;

    public MemoryCacheProvider()
    {
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _cleanupTimer = new Timer(_ => CleanupExpiredEntries(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public ValueTask<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ValueTask.FromResult<T?>(default);

        if (!_cache.TryGetValue(key, out var entry))
            return ValueTask.FromResult<T?>(default);

        if (entry.IsExpired)
        {
            _cache.TryRemove(key, out _);
            return ValueTask.FromResult<T?>(default);
        }

        entry.LastAccessTime = DateTime.UtcNow;
        return ValueTask.FromResult((T?)entry.Value);
    }

    public ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ValueTask.CompletedTask;

        // Single syscall instead of three separate DateTime.UtcNow reads.
        var now = DateTime.UtcNow;
        var entry = new CacheEntry
        {
            Key = key,
            Value = value,
            CreatedTime = now,
            LastAccessTime = now,
            ExpirationTime = expiration.HasValue ? now.Add(expiration.Value) : null
        };

        _cache.AddOrUpdate(key, entry, (_, _) => entry);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
            _cache.TryRemove(key, out _);

        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ValueTask.FromResult(false);

        if (!_cache.TryGetValue(key, out var entry))
            return ValueTask.FromResult(false);

        if (entry.IsExpired)
        {
            _cache.TryRemove(key, out _);
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(true);
    }

    public ValueTask ClearAsync()
    {
        _cache.Clear();
        return ValueTask.CompletedTask;
    }

    public ValueTask<IEnumerable<string>> GetAllKeysAsync()
    {
        var keys = _cache
            .Where(kvp => !kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        return ValueTask.FromResult<IEnumerable<string>>(keys);
    }

    private void CleanupExpiredEntries()
    {
        // ConcurrentDictionary is thread-safe; no lock needed for the scan.
        foreach (var kvp in _cache)
        {
            if (kvp.Value.IsExpired)
                _cache.TryRemove(kvp.Key, out _);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }

    private class CacheEntry
    {
        public required string Key { get; set; }
        public object? Value { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public DateTime? ExpirationTime { get; set; }

        public bool IsExpired => ExpirationTime.HasValue && DateTime.UtcNow > ExpirationTime.Value;
    }
}

/// <summary>
/// Cache key builder for consistent key generation.
/// Prevents key collisions and ensures proper tenant isolation.
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

    public CacheKeyBuilder Add(string? segment)
    {
        if (!string.IsNullOrWhiteSpace(segment))
            _segments.Add(segment);
        return this;
    }

    /// <summary>
    /// Add tenant context to key; ensures tenant isolation in cache.
    /// </summary>
    public CacheKeyBuilder WithTenant(string? tenantId)
    {
        if (!string.IsNullOrWhiteSpace(tenantId))
            _segments.Insert(0, $"tenant:{tenantId}");
        return this;
    }

    public CacheKeyBuilder WithUser(string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
            _segments.Add($"user:{userId}");
        return this;
    }

    /// <summary>
    /// Add hash of complex object to key.
    /// Useful for caching with parameter-dependent keys.
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
    /// Build final cache key. Uses ArrayPool to avoid a temporary List allocation
    /// when concatenating prefix and segments.
    /// </summary>
    public string Build()
    {
        if (_segments.Count == 0)
            return _prefix.ToLowerInvariant();

        var count = _segments.Count + 1;
        var pooled = ArrayPool<string>.Shared.Rent(count);
        try
        {
            pooled[0] = _prefix;
            _segments.CopyTo(pooled, 1);
            return string.Join(":", pooled, 0, count).ToLowerInvariant();
        }
        finally
        {
            ArrayPool<string>.Shared.Return(pooled, clearArray: false);
        }
    }
}
