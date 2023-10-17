# ICacheProvider

A provider for managing tenant-isolated, user-scoped, and hash-augmented in-memory cache operations with automatic key generation and expiration tracking. Designed for multi-tenant applications requiring fine-grained cache isolation and lifecycle control.

## API

### `MemoryCacheProvider`

A concrete implementation of `ICacheProvider` that uses `System.Runtime.Caching.MemoryCache` as the underlying cache store. Instances are thread-safe and disposable.

### `ValueTask<T?> GetAsync<T>()`

Retrieves a cached value of type `T` associated with the current cache key.

- **Parameters**: None.
- **Return value**: A `ValueTask<T?>` that resolves to the cached value if present, or `null` if not found or expired.
- **Exceptions**: Throws `ObjectDisposedException` if the provider has been disposed.

### `ValueTask SetAsync<T>(T value, TimeSpan? slidingExpiration = null)`

Stores a value in the cache with an optional sliding expiration policy.

- **Parameters**:
  - `value`: The value to cache.
  - `slidingExpiration`: Optional duration after which the item is removed if not accessed. If `null`, the item does not expire automatically.
- **Return value**: A `ValueTask` representing the asynchronous operation.
- **Exceptions**:
  - Throws `ArgumentNullException` if `value` is `null`.
  - Throws `ObjectDisposedException` if the provider has been disposed.

### `ValueTask RemoveAsync()`

Removes the currently scoped cache entry from the cache.

- **Parameters**: None.
- **Return value**: A `ValueTask` representing the asynchronous operation.
- **Exceptions**: Throws `ObjectDisposedException` if the provider has been disposed.

### `ValueTask<bool> ExistsAsync()`

Checks whether a cache entry exists for the current key and has not expired.

- **Parameters**: None.
- **Return value**: A `ValueTask<bool>` that resolves to `true` if the entry exists and is valid, otherwise `false`.
- **Exceptions**: Throws `ObjectDisposedException` if the provider has been disposed.

### `ValueTask ClearAsync()`

Removes all cache entries that match the current tenant and user scope.

- **Parameters**: None.
- **Return value**: A `ValueTask` representing the asynchronous operation.
- **Exceptions**: Throws `ObjectDisposedException` if the provider has been disposed.

### `ValueTask<IEnumerable<string>> GetAllKeysAsync()`

Retrieves all cache keys that match the current tenant and user scope.

- **Parameters**: None.
- **Return value**: A `ValueTask<IEnumerable<string>>` containing all matching cache keys.
- **Exceptions**: Throws `ObjectDisposedException` if the provider has been disposed.

### `void Dispose()`

Releases all resources used by the cache provider, including unregistering from global cleanup mechanisms.

- **Parameters**: None.
- **Return value**: None.
- **Exceptions**: None.

### `required string Key`

Gets the fully constructed cache key for the current context. Read-only.

- **Type**: `string`
- **Access**: Public get-only property.
- **Exceptions**: None.

### `object? Value`

Gets or sets the cached value associated with the current key. Read-write.

- **Type**: `object?`
- **Access**: Public property.
- **Exceptions**:
  - Throws `InvalidOperationException` when setting if the provider has been disposed.
  - Throws `ObjectDisposedException` when getting if the provider has been disposed.

### `DateTime CreatedTime`

Gets the UTC timestamp when the current cache entry was created. Read-only.

- **Type**: `DateTime`
- **Access**: Public get-only property.
- **Exceptions**: None.

### `DateTime LastAccessTime`

Gets the UTC timestamp of the last access to the current cache entry. Read-only.

- **Type**: `DateTime`
- **Access**: Public get-only property.
- **Exceptions**: None.

### `DateTime? ExpirationTime`

Gets the UTC timestamp when the current cache entry will expire, or `null` if it does not expire. Read-only.

- **Type**: `DateTime?`
- **Access**: Public get-only property.
- **Exceptions**: None.

### `CacheKeyBuilder`

A builder for constructing hierarchical, tenant-aware cache keys with support for user, hash, and tenant scopes.

### `CacheKeyBuilder Add(string segment)`

Appends a custom segment to the cache key.

- **Parameters**:
  - `segment`: The string segment to append.
- **Return value**: The updated `CacheKeyBuilder` for chaining.
- **Exceptions**: Throws `ArgumentNullException` if `segment` is `null`.

### `CacheKeyBuilder WithTenant(string tenantId)`

Sets the tenant identifier for the cache key.

- **Parameters**:
  - `tenantId`: The tenant identifier.
- **Return value**: The updated `CacheKeyBuilder` for chaining.
- **Exceptions**: Throws `ArgumentNullException` if `tenantId` is `null`.

### `CacheKeyBuilder WithUser(string userId)`

Sets the user identifier for the cache key.

- **Parameters**:
  - `userId`: The user identifier.
- **Return value**: The updated `CacheKeyBuilder` for chaining.
- **Exceptions**: Throws `ArgumentNullException` if `userId` is `null`.

### `CacheKeyBuilder WithHash(string hash)`

Appends a hash segment to the cache key for additional isolation or versioning.

- **Parameters**:
  - `hash`: The hash value to include.
- **Return value**: The updated `CacheKeyBuilder` for chaining.
- **Exceptions**: Throws `ArgumentNullException` if `hash` is `null`.

### `string Build()`

Finalizes and returns the constructed cache key.

- **Parameters**: None.
- **Return value**: The fully constructed cache key string.
- **Exceptions**: None.

## Usage

### Example 1: Basic tenant-scoped cache
