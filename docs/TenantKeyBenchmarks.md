# TenantKeyBenchmarks

Benchmarks for tenant key generation and manipulation strategies in multi-tenant applications. Measures performance characteristics of string concatenation, interpolation, cache key composition, frozen set lookups, subdomain extraction, and scoped key generation with optional hashing.

## API

### `public string TenantAwareKey_StringConcat()`

Generates a tenant-aware key using string concatenation. Returns the composed key string. Does not throw.

### `public string TenantAwareKey_Interpolation()`

Generates a tenant-aware key using string interpolation. Returns the composed key string. Does not throw.

### `public string CacheKeyBuilder_TenantAndResource()`

Builds a cache key combining tenant identifier and resource identifier. Returns the formatted cache key. Does not throw.

### `public bool FrozenSet_Contains_ReservedHit()`

Checks for a reserved key that exists in a frozen set. Returns `true` when the key is found. Does not throw.

### `public bool FrozenSet_Contains_ReservedMiss()`

Checks for a reserved key that does not exist in a frozen set. Returns `false` when the key is not found. Does not throw.

### `public string SubdomainExtract_IndexOf()`

Extracts a tenant subdomain from a hostname using `IndexOf` and substring operations. Returns the extracted subdomain. Does not throw.

### `public string SubdomainExtract_Split()`

Extracts a tenant subdomain from a hostname using `Split`. Returns the extracted subdomain. Does not throw.

### `public string GenerateTenantScopedKey()`

Generates a tenant-scoped key without cryptographic hashing. Returns the scoped key string. Does not throw.

### `public string GenerateTenantScopedKey_WithHash()`

Generates a tenant-scoped key incorporating a hash of the input. Returns the hashed scoped key string. Does not throw.

## Usage

```csharp
var benchmarks = new TenantKeyBenchmarks();

// Compare string concatenation vs interpolation for tenant key generation
string concatKey = benchmarks.TenantAwareKey_StringConcat();
string interpKey = benchmarks.TenantAwareKey_Interpolation();

// Evaluate cache key composition and scoped key generation
string cacheKey = benchmarks.CacheKeyBuilder_TenantAndResource();
string scopedKey = benchmarks.GenerateTenantScopedKey();
string hashedKey = benchmarks.GenerateTenantScopedKey_WithHash();
```

```csharp
var benchmarks = new TenantKeyBenchmarks();

// Benchmark subdomain extraction approaches
string indexOfResult = benchmarks.SubdomainExtract_IndexOf();
string splitResult = benchmarks.SubdomainExtract_Split();

// Measure frozen set lookup performance for hit and miss scenarios
bool hit = benchmarks.FrozenSet_Contains_ReservedHit();
bool miss = benchmarks.FrozenSet_Contains_ReservedMiss();
```

## Notes

- All members are parameterless and stateless; they operate on fixed test data defined within the benchmark class.
- No synchronization is required; instances can be used concurrently across threads without shared mutable state.
- Results are deterministic for a given runtime and input dataset; variance comes from JIT compilation, GC, and OS scheduling.
- `FrozenSet_Contains_ReservedHit` and `FrozenSet_Contains_ReservedMiss` exercise the same `FrozenSet<string>` instance with known-present and known-absent keys respectively.
- `SubdomainExtract_IndexOf` and `SubdomainExtract_Split` process identical hostname inputs; differences reflect algorithmic overhead, not input variance.
- `GenerateTenantScopedKey_WithHash` incurs additional allocation from hashing; compare against `GenerateTenantScopedKey` to quantify the cost.
