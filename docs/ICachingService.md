# ICachingService

The `ICachingService` interface defines a tenant-aware caching abstraction for .NET applications, enabling isolated caching per tenant while providing standard cache operations. It supports both synchronous and asynchronous operations, with metrics tracking for cache hits, misses, and total entries.

## API

### `int TotalEntries`
Gets the total number of entries currently stored in the cache across all tenants.

### `int CacheHits`
Gets the total number of cache hits recorded since the service was initialized.

### `int CacheMisses`
Gets the total number of cache misses recorded since the service was initialized.

### `CachingService`
Gets the underlying non-tenant-aware caching service instance.

### `TenantAwareCachingService`
Gets the underlying tenant-aware caching service instance.

### `Task<T?> GetOrFetchAsync<T>(Func<Task<T>> fetchFunc, string key, TimeSpan? slidingExpiration = null)`
Retrieves a value from the cache or fetches it using the provided function if the key is missing.

- **fetchFunc**: Function to invoke when the key is not found in the cache.
- **key**: Cache key to retrieve or store the value under.
- **slidingExpiration**: Optional sliding expiration time for the cached value.
- **Returns**: The cached value if present; otherwise, the result of `fetchFunc`.
- **Throws**: `ArgumentNullException` if `fetchFunc` or `key` is `null`.

### `ValueTask<T?> GetAsync<T>(string key)`
Retrieves a value from the cache asynchronously.

- **key**: Cache key to retrieve the value for.
- **Returns**: The cached value if present; otherwise, `null`.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `ValueTask SetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null)`
Stores a value in the cache asynchronously.

- **key**: Cache key to store the value under.
- **value**: Value to cache.
- **slidingExpiration**: Optional sliding expiration time for the cached value.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `ValueTask RemoveAsync(string key)`
Removes a value from the cache asynchronously.

- **key**: Cache key to remove.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `ValueTask ClearAsync()`
Removes all entries from the cache asynchronously.

### `ValueTask<CacheStatistics> GetStatisticsAsync()`
Retrieves current cache statistics including hits, misses, and total entries.

- **Returns**: A `CacheStatistics` object with the current metrics.

### `static IServiceCollection AddCachingService(IServiceCollection services)`
Registers the caching service and its dependencies with the dependency injection container.

- **services**: The `IServiceCollection` to configure.
- **Returns**: The configured `IServiceCollection` for method chaining.
- **Throws**: `ArgumentNullException` if `services` is `null`.

## Usage

### Basic Usage with Dependency Injection
