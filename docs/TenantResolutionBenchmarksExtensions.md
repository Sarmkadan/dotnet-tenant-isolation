# TenantResolutionBenchmarksExtensions

`TenantResolutionBenchmarksExtensions` is a static class containing benchmark extension methods designed to evaluate the performance characteristics of tenant resolution strategies in the `dotnet-tenant-isolation` library. These methods facilitate measuring execution time and resource consumption for various tenant resolution scenarios, including sequential processing, invalid identifier handling, context switching, and current tenant retrieval.

## API

### ResolveMultipleTenants_Sequential

**Purpose:** Measures the performance of resolving multiple tenants sequentially using the configured tenant resolver.

**Parameters:**
- `ITenantResolver resolver`: The tenant resolver instance to use for resolution.
- `IEnumerable<string> tenantIds`: A collection of tenant identifiers to resolve in sequence.

**Return Value:** `ValueTask` - Represents the asynchronous operation without returning a result.

**Exceptions:**
- `ArgumentNullException`: Thrown when `resolver` or `tenantIds` is null.
- `InvalidOperationException`: Thrown when resolution fails for any tenant ID in the sequence.

---

### ResolveTenant_InvalidId

**Purpose:** Evaluates the performance and behavior of the tenant resolver when provided with an invalid tenant identifier.

**Parameters:**
- `ITenantResolver resolver`: The tenant resolver instance to test.
- `string invalidTenantId`: An invalid tenant identifier expected to fail resolution.

**Return Value:** `ValueTask` - Represents the asynchronous operation without returning a result.

**Exceptions:**
- `ArgumentNullException`: Thrown when `resolver` or `invalidTenantId` is null.
- `TenantNotFoundException`: Thrown when the resolver fails to locate a tenant for the given identifier.

---

### ResolveTenant_SwitchContext

**Purpose:** Assesses the overhead of resolving a tenant while dynamically switching the execution context.

**Parameters:**
- `ITenantResolver resolver`: The tenant resolver instance to use.
- `string tenantId`: The tenant identifier to resolve.
- `Action<HttpContext> contextSwitcher`: A delegate that modifies the HTTP context before resolution.

**Return Value:** `ValueTask` - Represents the asynchronous operation without returning a result.

**Exceptions:**
- `ArgumentNullException`: Thrown when any parameter is null.
- `TenantNotFoundException`: Thrown if the tenant cannot be resolved under the switched context.

---

### GetCurrentTenant_Performance

**Purpose:** Benchmarks the performance of retrieving the currently active tenant from the ambient context.

**Parameters:**
- `ITenantResolver resolver`: The tenant resolver instance to query.

**Return Value:** `ValueTask` - Represents the asynchronous operation without returning a result.

**Exceptions:**
- `ArgumentNullException`: Thrown when `resolver` is null.
- `InvalidOperationException`: Thrown when no current tenant is available in the context.

---

## Usage

### Example 1: Sequential Tenant Resolution Benchmark

```csharp
[MemoryDiagnoser]
public class TenantResolutionBenchmarks
{
    private readonly ITenantResolver _resolver = new DefaultTenantResolver();
    private readonly string[] _tenantIds = { "tenant1", "tenant2", "tenant3" };

    [Benchmark]
    public async ValueTask ResolveMultipleTenants()
    {
        await TenantResolutionBenchmarksExtensions.ResolveMultipleTenants_Sequential(
            _resolver, 
            _tenantIds
        );
    }
}
```

### Example 2: Invalid Tenant ID Handling

```csharp
[Benchmark]
public async ValueTask HandleInvalidTenant()
{
    try
    {
        await TenantResolutionBenchmarksExtensions.ResolveTenant_InvalidId(
            _resolver, 
            "nonexistent-tenant"
        );
    }
    catch (TenantNotFoundException)
    {
        // Expected exception for invalid ID
    }
}
```

---

## Notes

- All methods are designed for benchmarking purposes and do not return meaningful results. They should be used within a performance testing framework like BenchmarkDotNet.
- Thread-safety depends on the underlying `ITenantResolver` implementation. If the resolver maintains state or relies on ambient context (e.g., `HttpContext`), concurrent execution may lead to race conditions or inconsistent results.
- Edge cases such as empty `tenantIds` collections or null `contextSwitcher` delegates are explicitly checked and will throw `ArgumentNullException`.
- The `ResolveTenant_SwitchContext` method assumes the presence of an `HttpContext` and may behave unpredictably in non-HTTP environments unless properly mocked.
