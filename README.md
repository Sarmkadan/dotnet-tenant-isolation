// existing content ...

## CacheBenchmarks

The `CacheBenchmarks` class provides a set of benchmarks for evaluating the performance of the cache implementation. It includes tests for cache hits, misses, upserts, and removals, as well as tests for cache key builders with different parameters.

### Example Usage

```csharp
// Create a new instance of CacheBenchmarks
var cacheBenchmarks = new CacheBenchmarks();

// Setup the cache benchmarks
cacheBenchmarks.Setup();

// Run the cache hit benchmark
var result = await cacheBenchmarks.GetAsync_CacheHit();

// Run the cache miss benchmark
var resultMiss = await cacheBenchmarks.GetAsync_CacheMiss();

// Run the upsert benchmark
await cacheBenchmarks.SetAsync_Upsert();

// Get the cache key builder with simple parameters
var cacheKeyBuilderSimple = cacheBenchmarks.CacheKeyBuilder_Simple;

// Get the cache key builder with tenant parameters
var cacheKeyBuilderTenant = cacheBenchmarks.CacheKeyBuilder_WithTenant;

// Get the cache key builder with tenant user and hash parameters
var cacheKeyBuilderTenantUserAndHash = cacheBenchmarks.CacheKeyBuilder_WithTenantUserAndHash;

// Check if the cache exists with a cache hit
var existsCacheHit = await cacheBenchmarks.ExistsAsync_CacheHit();

// Check if the cache exists with a cache miss
var existsCacheMiss = await cacheBenchmarks.ExistsAsync_CacheMiss();

// Remove the cache
await cacheBenchmarks.RemoveAsync();

// Cleanup the cache benchmarks
cacheBenchmarks.Cleanup();

// Dispose of the cache benchmarks
cacheBenchmarks.Dispose();
```

// existing content ...
