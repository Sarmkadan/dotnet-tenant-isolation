# TenantCleanupWorker

Background worker responsible for periodically cleaning up inactive or expired tenant resources in a multi-tenant application. It integrates with the host’s lifetime management to run cleanup tasks during application shutdown or when explicitly triggered.

## API

### `TenantCleanupWorker`

Constructor used to instantiate the worker. No parameters are required as dependencies are resolved via dependency injection.

### `AddTenantCleanupWorker(IHostBuilder hostBuilder)`

Extension method that registers the `TenantCleanupWorker` and its required services with the host builder.

- **Parameters**
  - `hostBuilder`: The `IHostBuilder` instance to configure.
- **Return Value**
  - Returns the same `IHostBuilder` instance for method chaining.
- **Throws**
  - `ArgumentNullException`: If `hostBuilder` is `null`.

### `StopAsync(CancellationToken)`

Initiates graceful shutdown of the cleanup worker, allowing ongoing operations to complete before stopping.

- **Parameters**
  - `cancellationToken`: A token to observe while waiting for the task to complete.
- **Return Value**
  - Returns a `Task` representing the asynchronous shutdown operation.
- **Throws**
  - `OperationCanceledException`: If the operation is canceled via the `cancellationToken`.

### `Dispose()`

Releases unmanaged resources and stops the background cleanup task if it is running.

- **Throws**
  - No exceptions are thrown under normal operation.

## Usage
