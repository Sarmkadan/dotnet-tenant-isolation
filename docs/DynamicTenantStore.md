# DynamicTenantStore

A tenant store implementation that periodically reloads its tenant data from an underlying source on a background schedule. It provides thread-safe, non-blocking access to the current set of active tenants and supports graceful start/stop of the reload cycle. Designed for scenarios where tenant definitions change at runtime and must be reflected without restarting the application.

## API

### `DynamicTenantStore`

The constructor. Initializes the store, sets up the reload schedule, and loads the initial set of tenants. The specific constructor parameters (e.g., reload interval, tenant source delegate, logger factory) are defined by the implementation; consult the source for the exact signature.

### `void StartReloading()`

Begins the periodic background reload cycle. If the store was previously stopped via `StopReloading`, calling this method resumes the timer-based refresh. If already running, subsequent calls have no effect. Does not perform an immediate reload on this call—the first scheduled reload occurs after the configured interval elapses.

- **Throws**: `ObjectDisposedException` if the instance has been disposed.

### `void StopReloading()`

Halts the periodic background reload cycle. The current in-memory tenant data remains available for queries. Use this to pause refreshes during maintenance windows or before disposing the store.

- **Throws**: `ObjectDisposedException` if the instance has been disposed.

### `Task<IEnumerable<Tenant>> GetAllActiveTenantsAsync()`

Returns a snapshot of all currently active tenants held in memory. The returned collection is a materialized copy, so subsequent background reloads do not mutate it. The method completes synchronously in most implementations (no I/O is performed on each call) but returns a `Task` for consistency with async patterns.

- **Returns**: A task that resolves to an enumerable of `Tenant` objects representing the active tenants at the moment of the call.
- **Throws**: `ObjectDisposedException` if the instance has been disposed.

### `Task<Tenant?> GetTenantByIdAsync(string id)`

Looks up a single tenant by its identifier from the current in-memory snapshot. Returns `null` if no active tenant with the given `id` exists at the time of the call.

- **Parameters**:
  - `id` (`string`): The tenant identifier to search for. Null or empty values are handled by the underlying lookup and will result in a `null` return.
- **Returns**: A task that resolves to the matching `Tenant` instance, or `null` if not found.
- **Throws**: `ObjectDisposedException` if the instance has been disposed.

### `void Dispose()`

Stops the background reload cycle (if running) and releases all resources held by the store, including the internal timer and any synchronization primitives. After disposal, all member methods throw `ObjectDisposedException`. Calling `Dispose` multiple times is safe; subsequent calls have no effect.

## Usage

### Example 1: Basic lifetime with periodic access

```csharp
var store = new DynamicTenantStore(
    reloadInterval: TimeSpan.FromMinutes(5),
    tenantSource: async ct => await remoteService.FetchTenantsAsync(ct),
    loggerFactory: loggerFactory);

store.StartReloading();

// Later, in a request handler:
IEnumerable<Tenant> activeTenants = await store.GetAllActiveTenantsAsync();
foreach (var tenant in activeTenants)
{
    Console.WriteLine($"Active: {tenant.Id}");
}

// On application shutdown:
store.StopReloading();
store.Dispose();
```

### Example 2: Lookup with fallback during controlled refresh pause

```csharp
var store = new DynamicTenantStore(
    reloadInterval: TimeSpan.FromSeconds(30),
    tenantSource: async ct => await database.LoadTenantsAsync(ct),
    loggerFactory: loggerFactory);

store.StartReloading();

// Pause refreshes during a deployment window
store.StopReloading();

// Incoming requests still resolve against the last-known snapshot
Tenant? tenant = await store.GetTenantByIdAsync("tenant-abc");
if (tenant is null)
{
    // Handle unknown tenant — log, reject, or fall back to default
    throw new TenantNotFoundException("tenant-abc");
}

// Resume after deployment completes
store.StartReloading();
```

## Notes

- **Thread safety**: All public query methods (`GetAllActiveTenantsAsync`, `GetTenantByIdAsync`) read from a snapshot that is atomically swapped by the background reload cycle. Concurrent reads and a single background write do not block each other. `StartReloading`, `StopReloading`, and `Dispose` use internal synchronization to prevent race conditions when starting or stopping the timer.
- **Disposed state**: Once `Dispose` is called, every public method throws `ObjectDisposedException`. There is no recovery; create a new instance if tenant access is needed after disposal.
- **Empty source results**: If the tenant source delegate returns an empty collection during a reload, the store updates its snapshot to an empty set. Queries after such a reload will return no tenants. Implement retry or stale-data fallback in the source delegate if this behavior is undesirable.
- **Reload overlap**: The implementation ensures that a slow reload does not overlap with the next scheduled tick. If a reload is still in progress when the interval elapses, that tick is skipped.
- **Start/Stop idempotency**: Calling `StartReloading` when already running, or `StopReloading` when already stopped, is a no-op and does not throw.
- **Snapshot materialization**: The enumerable returned by `GetAllActiveTenantsAsync` is a materialized copy (e.g., a list). Callers may enumerate it safely without holding locks and without risk of modification by a concurrent reload.
