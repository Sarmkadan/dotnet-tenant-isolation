# SubscriptionExpirationWorker

A background service that monitors and enforces subscription expiration policies for multi-tenant applications. This worker periodically checks tenant subscriptions against their configured expiration dates, triggering appropriate actions (e.g., soft deletion, notifications) when expirations are detected. It integrates with the host's dependency injection system and supports graceful shutdown.

## API

### `SubscriptionExpirationWorker`
**Purpose:**
The primary constructor for the worker. Typically invoked implicitly via dependency injection when registering the service with `AddSubscriptionExpirationWorker`.

**Parameters:**
None (relies on DI-resolved dependencies).

---

### `public override async Task StopAsync(CancellationToken cancellationToken)`
**Purpose:**
Gracefully shuts down the worker, completing any in-progress expiration checks before halting. Called by the host during application shutdown.

**Parameters:**
- `cancellationToken`: A token to monitor for cancellation requests. The worker respects this token during shutdown.

**Return Value:**
A `Task` representing the asynchronous shutdown operation.

**Throws:**
- `OperationCanceledException`: If the shutdown is interrupted by cancellation.

---

### `public override void Dispose()`
**Purpose:**
Releases unmanaged resources and disposes of managed dependencies (e.g., timers, database connections). Called by the host when the worker is no longer needed.

**Parameters:**
None.

**Return Value:**
Void.

**Throws:**
None (implements `IDisposable` contract).

---

### `public static IHostBuilder AddSubscriptionExpirationWorker(this IHostBuilder hostBuilder)`
**Purpose:**
Registers the `SubscriptionExpirationWorker` with the host's service collection and configures required dependencies (e.g., expiration check interval, tenant repository). Intended for use during application startup.

**Parameters:**
- `hostBuilder`: The `IHostBuilder` instance to extend.

**Return Value:**
The modified `IHostBuilder` for method chaining.

**Throws:**
- `ArgumentNullException`: If `hostBuilder` is `null`.

## Usage

### Example 1: Basic Registration
