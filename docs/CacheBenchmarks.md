# CacheBenchmarks

A benchmarking utility for evaluating cache performance under tenant-isolated scenarios, measuring operations like cache hits, misses, upserts, and key generation strategies across different tenant configurations.

## API

### `Setup`
Initializes the benchmark environment, including cache providers and tenant contexts. Must be called before any benchmark operations. Throws if initialization fails (e.g., cache provider unavailable).

### `GetAsync_CacheHit`
Measures cache read performance when the requested key exists. Returns the cached value. Throws if the key does not exist after setup (unexpected cache miss).

### `GetAsync_CacheMiss`
Measures cache read performance when the requested key does not exist. Returns `null`. Throws if the key unexpectedly exists (unexpected cache hit).

### `SetAsync_Upsert`
Measures cache write performance during an upsert operation. Returns the result of the operation (e.g., success status). Throws if the upsert fails (e.g., network issues).

### `CacheKeyBuilder_Simple`
A benchmark-specific key generator producing a static key for baseline comparisons. Returns a non-tenant-specific key string.

### `CacheKeyBuilder_WithTenant`
A tenant-aware key generator producing a key scoped to a tenant. Returns a string combining the tenant ID with the base key.

### `CacheKeyBuilder_WithTenantUserAndHash`
A tenant-aware key generator producing a key scoped to a tenant, user, and a hashed component. Returns a string combining tenant ID, user ID, and a stable hash of the payload.

### `ExistsAsync_CacheHit`
Measures existence-check performance when the key exists. Returns `true`. Throws if the key does not exist (unexpected cache miss).

### `ExistsAsync_CacheMiss`
Measures existence-check performance when the key does not exist. Returns `false`. Throws if the key unexpectedly exists (unexpected cache hit).

### `RemoveAsync`
Measures cache removal performance. Returns a boolean indicating success. Throws if the removal fails (e.g., key already absent).

### `Cleanup`
Resets the benchmark state, clearing any test data from the cache. Throws if cleanup fails (e.g., partial failure).

### `Dispose`
Releases all resources held by the benchmark, including cache connections. Must be called to avoid leaks. Throws if disposal encounters errors (e.g., connection leaks).

## Usage
