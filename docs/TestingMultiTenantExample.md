# TestingMultiTenantExample

Overview  
The `TestingMultiTenantExample` type contains a set of static asynchronous helper methods used to exercise the multi‑tenant isolation features of the `dotnet-tenant-isolation` library in an automated test scenario. It configures an in‑memory database, creates tenant‑scoped services, and validates that tenant data remains isolated.

## API

### `SetupInMemoryTestDatabaseAsync`

```csharp
public static async Task<ServiceProvider> SetupInMemoryTestDatabaseAsync()
```

**Purpose**  
Creates an Entity Framework Core in‑memory database, registers the tenant‑aware DbContext and related services, and builds a `ServiceProvider` that can be used to resolve tenant‑scoped components.

**Parameters**  
None.

**Return value**  
A `ServiceProvider` configured with the in‑memory database and tenant services. The caller is responsible for disposing the provider when testing is complete.

**Exceptions**  
- Throws `InvalidOperationException` if the DbContext cannot be initialized with the in‑memory provider.  
- Propagates any exception thrown during service registration or provider building.

### `TestTenantCreationAsync`

```csharp
public static async Task TestTenantCreationAsync()
```

**Purpose**  
Verifies that a new tenant can be created successfully using the services obtained from the provider returned by `SetupInMemoryTestDatabaseAsync`. It creates a tenant record, persists it, and asserts that the record can be read back.

**Parameters**  
None. The method internally obtains a fresh `ServiceProvider` by invoking `SetupInMemoryTestDatabaseAsync`.

**Return value**  
Completes when the tenant creation and retrieval assertions have succeeded.

**Exceptions**  
- Throws `InvalidOperationException` if the tenant‑scoped DbContext cannot be resolved.  
- Throws `AssertionException` (or the testing framework’s equivalent) if the created tenant cannot be retrieved or if any expected property does not match the input.  
- Propagates any underlying database exception.

### `TestMultipleTenantIsolationAsync`

```csharp
public static async Task TestMultipleTenantIsolationAsync()
```

**Purpose**  
Confirms that data inserted for one tenant is not visible to another tenant. The method creates two distinct tenants, inserts tenant‑specific data for each, and then queries each tenant’s context to ensure isolation.

**Parameters**  
None. Internally calls `SetupInMemoryTestDatabaseAsync` to obtain a provider and resolves two separate tenant scopes.

**Return value**  
Completes when all isolation assertions have passed.

**Exceptions**  
- Throws `InvalidOperationException` if tenant scopes cannot be created.  
- Throws an assertion‑based exception if any data from one tenant is observed in the other's context.  
- Propagates any database‑related exceptions that occur during insert or query operations.

### `RunAsync`

```csharp
public static async Task RunAsync()
```

**Purpose**  
Convenience orchestrator that executes the full test sequence: database setup, tenant creation verification, and multi‑tenant isolation verification. Intended for use as a single entry point in test suites.

**Parameters**  
None.

**Return value**  
Completes when all constituent operations have succeeded.

**Exceptions**  
- Propagates the first exception thrown by any of the invoked methods (`SetupInMemoryTestDatabaseAsync`, `TestTenantCreationAsync`, `TestMultipleTenantIsolationAsync`).  
- If an exception occurs, previously created resources may not be disposed; callers should handle cleanup as needed.

## Usage

### Example 1: Individual step execution

```csharp
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

// Obtain a provider with an in‑memory tenant‑aware database.
var provider = await TestingMultiTenantExample.SetupInMemoryTestDatabaseAsync();
try
{
    // Resolve a tenant‑scoped service and create a tenant.
    await TestingMultiTenantExample.TestTenantCreationAsync();

    // Verify that tenants cannot see each other's data.
    await TestingMultiTenantExample.TestMultipleTenantIsolationAsync();
}
finally
{
    // Dispose of the provider to release the in‑memory database.
    if (provider is IAsyncDisposable asyncDisposable)
        await asyncDisposable.DisposeAsync();
    else
        provider.Dispose();
}
```

### Example 2: Using the convenience orchestrator

```csharp
using System.Threading.Tasks;

// Runs the complete validation flow in a single call.
await TestingMultiTenantExample.RunAsync();
// No manual disposal required; RunAsync internally disposes the provider.
```

## Notes

- The methods are **static** and rely on internal state (the in‑memory database) that is recreated on each call to `SetupInMemoryTestDatabaseAsync`. Repeatedly invoking the setup methods without disposing the previous provider can lead to multiple overlapping in‑memory databases, increasing memory consumption.
- The class is **not thread‑safe** for concurrent invocations because the static methods share the same underlying service registration logic. If parallel execution is required, each thread should obtain its own provider by calling `SetupInMemoryTestDatabaseAsync` and avoid sharing the returned `ServiceProvider` across threads.
- Exceptions are not swallowed; they propagate to the caller so that test frameworks can mark the test as failed. Callers should ensure that any acquired `ServiceProvider` is disposed even when an exception occurs (see the `finally` block in Example 1).
- The in‑memory database provider does not persist data beyond the lifetime of the `ServiceProvider`. Therefore, any attempt to use resolved services after disposing the provider will result in `ObjectDisposedException`.  
- These helpers are intended solely for testing scenarios; they should not be used in production code.
