# TenantStoreBackgroundReloadService

The `TenantStoreBackgroundReloadService` is a background component responsible for periodically refreshing tenant configuration data from the underlying store. It integrates with the host's lifetime management to start and stop the reload loop automatically and disposes of any held resources when the application shuts down.

## API

### TenantStoreBackgroundReloadService(ITenantStore store, ILogger<TenantStoreBackgroundReloadService> logger, TimeSpan reloadInterval, CancellationTokenSource tokenSource)

**Purpose**  
Initializes a new instance of the service with the dependencies required to perform background reloads.

**Parameters**  
- `store`: The tenant store provider used to read tenant data.  
- `logger`: Logger for emitting diagnostic information.  
- `reloadInterval`: The interval at which the store is reloaded.  
- `tokenSource`: Cancellation token source used to signal the reload loop to stop.

**Return value**  
A new `TenantStoreBackgroundReloadService` instance.

**Exceptions**  
- `ArgumentNullException` if any of the parameters is `null`.  
- `ArgumentOutOfRangeException` if `reloadInterval` is less than or equal to `TimeSpan.Zero`.

### Task StartAsync(CancellationToken cancellationToken)

**Purpose**  
Starts the background reload loop. The method returns when the loop has been initiated and is ready to process reloads.

**Parameters**  
- `cancellationToken`: Token that can be used to cancel the start operation.

**Return value**  
A `Task` that completes when the service has started; the returned task does not represent the ongoing reload work.

**Exceptions**  
- `OperationCanceledException` if `cancellationToken` is canceled before the start operation finishes.  
- `InvalidOperationException` if the service is already running.

### Task StopAsync(CancellationToken cancellationToken)

**Purpose**  
Stops the background reload loop and allows any in‑progress reload to finish gracefully.

**Parameters**  
- `cancellationToken`: Token that can be used to cancel the stop operation.

**Return value**  
A `Task` that completes when the service has stopped.

**Exceptions**  
- `OperationCanceledException` if `cancellationToken` is canceled before the stop operation finishes.  
- `InvalidOperationException` if the service is not running.

### async ValueTask DisposeAsync()

**Purpose**  
Releases all resources held by the service, ensuring that the background loop is terminated and any unmanaged handles are closed.

**Return value**  
A `ValueTask` that completes when disposal is finished.

**Exceptions**  
- `ObjectDisposedException` if `DisposeAsync` is called more than once.  
# TenantStoreBackground work that has not yet completed will be awaited before returning.

## Usage

### Registering as a hosted service

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<ITenantStore, SqlTenantStore>();
        services.AddHostedService<TenantStoreBackgroundReloadService>();
    })
    .Build();

await host.RunAsync();
```

### Manual instantiation and control

```csharp
using var cts = new CancellationTokenSource();
var store = new InMemoryTenantStore();
var logger = NullLogger<TenantStoreBackgroundReloadService>.Instance;
var service = new TenantStoreBackgroundReloadService(
    store,
    logger,
    TimeSpan.FromMinutes(5),
    cts);

// Start the background reload.
await service.StartAsync(cts.Token);

// ... application logic ...

// Signal shutdown and stop the service.
cts.Cancel();
await service.StopAsync(cts.Token);

// Dispose resources.
await service.DisposeAsync();
```

## Notes

- Calling `StartAsync` while the service is already running will result in an `InvalidOperationException`.  
- Invoking `StopAsync` before `StartAsync` has completed successfully also throws an `InvalidOperationException`.  
- The service is safe to be used concurrently by the host's lifetime management; however, external code should not invoke `StartAsync` or `StopAsync` concurrently with each other.  
- `DisposeAsync` may be called after `StopAsync` has completed; calling it while the background loop is still active will trigger a graceful cancellation of the loop before disposing resources.  
- Any unhandled exception thrown inside the reload loop is logged but does not propagate out of `StartAsync` or `StopAsync`; the loop will continue on the next interval unless the cancellation token is triggered.  
- The service does not expose any public properties or methods beyond those listed; all configuration must be supplied via the constructor.
