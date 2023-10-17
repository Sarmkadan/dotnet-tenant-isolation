# ITenantAwareDistributedCacheProvider

A provider interface and implementation for tenant-aware distributed caching, enabling isolation of cached data between tenants in multi-tenant applications. It extends `ICacheProvider` to ensure all cache operations are scoped to a specific tenant, preventing data leakage across tenant boundaries.

## API

### `TenantAwareDistributedCacheProvider`
The default implementation of `ITenantAwareDistributedCacheProvider`. Constructs with a tenant identifier and an underlying `IDistributedCache` instance.

**Parameters**
- `tenantId` (string): The tenant identifier used to scope all cache operations.
- `cache` (IDistributedCache): The underlying distributed cache system to delegate operations to.

**Remarks**
- The `tenantId` is prepended to all cache keys to ensure tenant isolation.
- Implements `IDisposable` to clean up any resources if needed by the underlying cache.

---

### `async ValueTask<T?> GetAsync<T>(string key)`
Retrieves a cached value by key, deserializing it to type `T`.

**Parameters**
- `key` (string): The cache key (without tenant prefix).

**Return Value**
- `ValueTask<T?>`: The deserialized value if found; otherwise, `null`.

**Exceptions**
- Throws `ArgumentNullException` if `key` is `null`.
- Throws `SerializationException` if deserialization fails.

**Remarks**
- The key is automatically prefixed with the tenant identifier.
- Returns `null` if the key does not exist.

---

### `async ValueTask SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null)`
Stores a value in the cache with an optional expiration.

**Parameters**
- `key` (string): The cache key (without tenant prefix).
- `value` (T): The value to cache.
- `absoluteExpirationRelativeToNow` (TimeSpan?, optional): The time span from now when the entry should expire. Defaults to `null` (no expiration).

**Exceptions**
- Throws `ArgumentNullException` if `key` is `null`.
- Throws `ArgumentNullException` if `value` is `null`.

**Remarks**
- The key is automatically prefixed with the tenant identifier.
- If `absoluteExpirationRelativeToNow` is `null`, the value is cached indefinitely.

---
### `async ValueTask RemoveAsync(string key)`
Removes a cached value by key.

**Parameters**
- `key` (string): The cache key (without tenant prefix).

**Exceptions**
- Throws `ArgumentNullException` if `key` is `null`.

**Remarks**
- The key is automatically prefixed with the tenant identifier.
- No-op if the key does not exist.

---
### `ValueTask<bool> ExistsAsync(string key)`
Checks whether a cached value exists for the given key.

**Parameters**
- `key` (string): The cache key (without tenant prefix).

**Return Value**
- `ValueTask<bool>`: `true` if the key exists; otherwise, `false`.

**Exceptions**
- Throws `ArgumentNullException` if `key` is `null`.

**Remarks**
- The key is automatically prefixed with the tenant identifier.

---
### `async ValueTask ClearAsync()`
Removes all cached values associated with the current tenant.

**Return Value**
- `ValueTask`: A task representing the asynchronous operation.

**Remarks**
- Only removes keys prefixed with the current tenant identifier.
- No-op if no keys exist for the tenant.

---
### `ValueTask<IEnumerable<string>> GetAllKeysAsync()`
Retrieves all cache keys associated with the current tenant.

**Return Value**
- `ValueTask<IEnumerable<string>>`: An enumerable of tenant-scoped keys (without tenant prefix).

**Remarks**
- The returned keys are stripped of the tenant prefix for consistency with other methods.
- The order of keys is not guaranteed.

---
### `void Dispose()`
Releases any resources used by the provider.

**Remarks**
- Calls `DisposeAsync` on the underlying `IDistributedCache` if it implements `IAsyncDisposable`.
- Safe to call multiple times.

## Usage

### Basic Usage
