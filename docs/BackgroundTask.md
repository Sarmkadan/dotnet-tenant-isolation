# BackgroundTask

`BackgroundTask` represents a unit of deferred work within the dotnet-tenant-isolation background processing pipeline. It encapsulates a named, prioritized, retryable asynchronous operation together with its scheduling metadata and exposes members for enqueueing, dequeuing, and tracking execution statistics across a bounded in-process queue managed by `BackgroundTaskHostedService`.

## API

### Properties

#### `Id` : `string`
Globally unique identifier for the task instance. Assigned at creation and immutable thereafter.

#### `Name` : `string`
Human-readable label describing the task’s purpose. Used in logs and queue statistics.

#### `WorkItem` : `Func<CancellationToken, Task>`
The asynchronous delegate that performs the actual work. Receives a `CancellationToken` that signals graceful shutdown. Must be set before the task is enqueued; otherwise the task will fail immediately upon dequeuing.

#### `EnqueuedAt` : `DateTime`
UTC timestamp recorded when `QueueTask` is called. Used to calculate queue latency and for diagnostics.

#### `Priority` : `BackgroundTaskPriority`
Enumeration value that determines dequeuing order. Higher-priority tasks are dequeued before lower-priority tasks when multiple items are waiting.

#### `MaxRetries` : `int`
Maximum number of automatic retry attempts after a failure. The hosted service decrements an internal retry counter on each attempt and discards the task when the counter reaches zero.

#### `PendingTasks` : `int`
Snapshot of the number of tasks currently waiting in the queue. Updated by the queue implementation.

#### `CompletedTasks` : `int`
Cumulative count of tasks that have finished successfully across the lifetime of the queue.

#### `FailedTasks` : `int`
Cumulative count of tasks that exhausted all retries or threw non-retryable exceptions.

#### `RunningTasks` : `int`
Number of tasks currently executing inside the hosted service’s worker loop.

#### `AverageExecutionTime` : `TimeSpan`
Rolling average wall-clock duration of completed task executions. Recalculated by `RecordTaskCompletion`.

#### `BackgroundTaskQueue` : `BackgroundTaskQueue`
The underlying queue channel that stores pending `BackgroundTask` instances. Exposed for advanced scenarios such as inspecting queue depth or draining.

### Methods

#### `void QueueTask(BackgroundTask task)`
Enqueues a `BackgroundTask` for eventual execution by the hosted service.
- **Parameters**: `task` — the fully configured `BackgroundTask` to enqueue. Must not be null.
- **Throws**: `ArgumentNullException` when `task` is null; `InvalidOperationException` when the queue channel has been marked as completed (shutdown).

#### `async Task<BackgroundTask?> DequeueAsync(CancellationToken cancellationToken)`
Asynchronously waits for and removes the next available task from the queue, respecting priority order.
- **Parameters**: `cancellationToken` — propagated to the underlying channel read; cancelling it aborts the wait.
- **Returns**: The next `BackgroundTask` ready for execution, or `null` if the channel is closed and empty.
- **Throws**: `OperationCanceledException` when the token is cancelled before a task arrives.

#### `QueueStatistics GetStatistics()`
Returns a snapshot of current queue metrics.
- **Returns**: A `QueueStatistics` object populated with the latest values of `PendingTasks`, `CompletedTasks`, `FailedTasks`, `RunningTasks`, and `AverageExecutionTime`.

#### `void RecordTaskCompletion(TimeSpan executionTime)`
Updates the rolling average execution time and increments the `CompletedTasks` counter.
- **Parameters**: `executionTime` — the measured duration of a successfully completed task.
- **Remarks**: Must be called exactly once per successful execution; calling it for failed tasks skews statistics.

#### `void IncrementRunningCount()`
Atomically increases `RunningTasks` by one. Called by the hosted service immediately before invoking `WorkItem`.

#### `void DecrementRunningCount()`
Atomically decreases `RunningTasks` by one. Called by the hosted service in a `finally` block after `WorkItem` completes or throws.

### Hosted Service

#### `BackgroundTaskHostedService` : `BackgroundTaskHostedService`
The `IHostedService` implementation that continuously dequeues and executes tasks on a background thread. It honours `Priority`, handles retry logic based on `MaxRetries`, and respects cancellation tokens during application shutdown.

### Dependency Injection Extension

#### `static IServiceCollection AddBackgroundTaskQueue(this IServiceCollection services)`
Registers `BackgroundTaskQueue` as a singleton and `BackgroundTaskHostedService` as a hosted service in the DI container.
- **Parameters**: `services` — the service collection to configure.
- **Returns**: The same `IServiceCollection` for chaining.
- **Throws**: `ArgumentNullException` when `services` is null.

## Usage

### Example 1: Enqueueing a tenant-specific cleanup task

```csharp
// Assume services obtained via DI
var queue = serviceProvider.GetRequiredService<BackgroundTaskQueue>();

var cleanupTask = new BackgroundTask
{
    Id = Guid.NewGuid().ToString(),
    Name = $"TenantCleanup-{tenantId}",
    Priority = BackgroundTaskPriority.Low,
    MaxRetries = 2,
    WorkItem = async (cancellationToken) =>
    {
        await tenantDataStore.PurgeExpiredSessionsAsync(tenantId, cancellationToken);
    }
};

queue.QueueTask(cleanupTask);
```

### Example 2: Monitoring queue health from a health-check endpoint

```csharp
app.MapGet("/health/queue", (BackgroundTaskQueue queue) =>
{
    var stats = queue.GetStatistics();
    bool healthy = stats.PendingTasks < 50 && stats.FailedTasks < 10;

    return new
    {
        status = healthy ? "Healthy" : "Degraded",
        stats.PendingTasks,
        stats.RunningTasks,
        stats.CompletedTasks,
        stats.FailedTasks,
        averageExecutionMs = stats.AverageExecutionTime.TotalMilliseconds
    };
});
```

## Notes

- **Thread safety**: `IncrementRunningCount`, `DecrementRunningCount`, and the counters backing `GetStatistics` use atomic operations or locking internally. External callers may safely read statistics from any thread while the hosted service is running.
- **Queue completion**: Once the application host initiates shutdown, the queue channel is marked complete. Subsequent calls to `QueueTask` throw `InvalidOperationException`. `DequeueAsync` drains remaining tasks and then returns `null`.
- **Retry behaviour**: The hosted service catches exceptions from `WorkItem` and re-enqueues the task (preserving original `EnqueuedAt`) until the retry budget is exhausted. Tasks that repeatedly fail may delay processing of other items; set `MaxRetries` conservatively.
- **Statistics accuracy**: `RecordTaskCompletion` must only be invoked for successful executions. Calling it for failed attempts causes `AverageExecutionTime` and `CompletedTasks` to drift from reality.
- **Null `WorkItem`**: A task enqueued without a valid `WorkItem` delegate will throw when dequeued, immediately consuming one retry attempt. Always validate `WorkItem` before calling `QueueTask`.
- **Priority ordering**: Dequeuing order is best-effort and depends on the underlying channel implementation. Under high load, lower-priority tasks may still execute before higher-priority ones if the higher-priority tasks were enqueued after the dequeue loop already selected a candidate.
