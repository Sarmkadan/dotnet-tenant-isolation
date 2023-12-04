// existing content ...

## TenantKeyBenchmarks

The `TenantKeyBenchmarks` class provides a set of benchmarks for evaluating the performance of tenant-aware key generation and cache key building. It includes tests for string concatenation, string interpolation, cache key building, and subdomain extraction.

### Example Usage

```csharp
// Create a new instance of TenantKeyBenchmarks
var tenantKeyBenchmarks = new TenantKeyBenchmarks();

// Test string concatenation
var tenantAwareKeyStringConcat = tenantKeyBenchmarks.TenantAwareKey_StringConcat;

// Test string interpolation
var tenantAwareKeyInterpolation = tenantKeyBenchmarks.TenantAwareKey_Interpolation;

// Test cache key building
var cacheKeyBuilderTenantAndResource = tenantKeyBenchmarks.CacheKeyBuilder_TenantAndResource;

// Test frozen set contains reserved hit
var frozenSetContainsReservedHit = tenantKeyBenchmarks.FrozenSet_Contains_ReservedHit;

// Test frozen set contains reserved miss
var frozenSetContainsReservedMiss = tenantKeyBenchmarks.FrozenSet_Contains_ReservedMiss;

// Test subdomain extraction by index
var subdomainExtractIndexOf = tenantKeyBenchmarks.SubdomainExtract_IndexOf;

// Test subdomain extraction by split
var subdomainExtractSplit = tenantKeyBenchmarks.SubdomainExtract_Split;

// Test generating a tenant-scoped key
var generateTenantScopedKey = tenantKeyBenchmarks.GenerateTenantScopedKey;

// Test generating a tenant-scoped key with hash
var generateTenantScopedKeyWithHash = tenantKeyBenchmarks.GenerateTenantScopedKey_WithHash;
```

// existing content ...
```