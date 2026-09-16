#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TenantIsolation.BackgroundTasks;

/// <summary>
/// Background task definition
/// </summary>
public class BackgroundTask
{
    /// <summary>
    /// Gets or sets the unique identifier of the task.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the display name of the task.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the work item to execute for the task.
    /// </summary>
    public Func<CancellationToken, Task> WorkItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the time the task was enqueued.
    /// </summary>
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the priority of the task.
    /// </summary>
    public BackgroundTaskPriority Priority { get; set; } = BackgroundTaskPriority.Normal;

    /// <summary>
    /// Gets or sets the maximum number of retries for the task.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Task priority levels
/// </summary>
public enum BackgroundTaskPriority
{
    /// <summary>
    /// Low priority task.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority task.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority task.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority task.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Background task queue interface
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Enqueue task for background execution
    /// </summary>
    void QueueTask(BackgroundTask task);

    /// <summary>
    /// Dequeue next task
    /// </summary>
    Task<BackgroundTask?> DequeueAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get queue statistics
    /// </summary>
    QueueStatistics GetStatistics();
}

/// <summary>
/// Queue statistics
/// </summary>
public class QueueStatistics
{
    /// <summary>
    /// Gets or sets the number of tasks currently pending in the queue.
    /// </summary>
    public int PendingTasks { get; set; }

    /// <summary>
    /// Gets or sets the number of tasks that have completed successfully.
    /// </summary>
    public int CompletedTasks { get; set; }

    /// <summary>
    /// Gets or sets the number of tasks that have failed.
    /// </summary>
    public int FailedTasks { get; set; }

    /// <summary>
    /// Gets or sets the number of tasks currently running.
    /// </summary>
    public int RunningTasks { get; set; }

    /// <summary>
    /// Gets or sets the average execution time of completed tasks.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; set; }
}

/// <summary>
/// Background task queue implementation
/// Uses priority queue for task execution order
/// </summary>
public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly PriorityQueue<BackgroundTask, BackgroundTaskPriority> _queue;
    private readonly SemaphoreSlim _signal;
    private readonly ILogger<BackgroundTaskQueue> _logger;

    private int _completedTasks;
    private int _failedTasks;
    private int _runningTasks;
    private readonly List<long> _executionTimes = new();

    /// <summary>
    /// Initializes a new instance of the BackgroundTaskQueue class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public BackgroundTaskQueue(ILogger<BackgroundTaskQueue> logger)
    {
        _queue = new PriorityQueue<BackgroundTask, BackgroundTaskPriority>();
        _signal = new SemaphoreSlim(0);
        _logger = logger;
    }

    /// <summary>
    /// Enqueue task for background execution.
    /// </summary>
    /// <param name="task">The task to enqueue.</param>
    public void QueueTask(BackgroundTask task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        lock (_queue)
        {
            _queue.Enqueue(task, task.Priority);
            _logger.LogInformation("Queued task '{TaskName}' (ID: {TaskId}) with priority {Priority}",
                task.Name, task.Id, task.Priority);
        }

        _signal.Release();
    }

    /// <summary>
    /// Dequeue next task.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The dequeued task, or null if none available.</returns>
    public async Task<BackgroundTask?> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);

        lock (_queue)
        {
            return _queue.Count > 0 ? _queue.Dequeue() : null;
        }
    }

    /// <summary>
    /// Get queue statistics.
    /// </summary>
    /// <returns>The current queue statistics.</returns>
    public QueueStatistics GetStatistics()
    {
        lock (_queue)
        {
            var avgTime = _executionTimes.Count > 0
                ? TimeSpan.FromMilliseconds(_executionTimes.Average())
                : TimeSpan.Zero;

            return new QueueStatistics
            {
                PendingTasks = _queue.Count,
                CompletedTasks = _completedTasks,
                FailedTasks = _failedTasks,
                RunningTasks = _runningTasks,
                AverageExecutionTime = avgTime
            };
        }
    }

    /// <summary>
    /// Internal method to record task completion.
    /// </summary>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <param name="isSuccess">Whether the task completed successfully.</param>
    public void RecordTaskCompletion(long executionTimeMs, bool isSuccess)
    {
        lock (_queue)
        {
            _executionTimes.Add(executionTimeMs);
            if (isSuccess)
                _completedTasks++;
            else
                _failedTasks++;

            // Keep only last 100 execution times for memory efficiency
            if (_executionTimes.Count > 100)
                _executionTimes.RemoveAt(0);
        }
    }

    /// <summary>
    /// Increments the running task count.
    /// </summary>
    public void IncrementRunningCount() => Interlocked.Increment(ref _runningTasks);

    /// <summary>
    /// Decrements the running task count.
    /// </summary>
    public void DecrementRunningCount() => Interlocked.Decrement(ref _runningTasks);
}

/// <summary>
/// Hosted service for processing background tasks
/// Continuously pulls tasks from queue and executes them
/// </summary>
public class BackgroundTaskHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<BackgroundTaskHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the BackgroundTaskHostedService class.
    /// </summary>
    /// <param name="taskQueue">The background task queue.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public BackgroundTaskHostedService(
        IBackgroundTaskQueue taskQueue,
        ILogger<BackgroundTaskHostedService> logger,
        IServiceProvider serviceProvider)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Executes the background task processing logic.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns>A task representing the execution.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background task processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var task = await _taskQueue.DequeueAsync(stoppingToken);
                if (task == null)
                    continue;

                (_taskQueue as BackgroundTaskQueue)?.IncrementRunningCount();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    _logger.LogInformation("Executing background task '{TaskName}' (ID: {TaskId})",
                        task.Name, task.Id);

                    await task.WorkItem(stoppingToken);

                    stopwatch.Stop();
                    (_taskQueue as BackgroundTaskQueue)?.RecordTaskCompletion(stopwatch.ElapsedMilliseconds, true);

                    _logger.LogInformation("Completed task '{TaskName}' in {DurationMs}ms",
                        task.Name, stopwatch.ElapsedMilliseconds);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Task '{TaskName}' was cancelled", task.Name);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    (_taskQueue as BackgroundTaskQueue)?.RecordTaskCompletion(stopwatch.ElapsedMilliseconds, false);

                    _logger.LogError(ex, "Error executing task '{TaskName}'", task.Name);

                    // Re-queue failed task if retries remain
                    if (task.MaxRetries > 0)
                    {
                        task.MaxRetries--;
                        _taskQueue.QueueTask(task);
                        _logger.LogInformation("Re-queued task '{TaskName}' for retry ({RetriesRemaining} remaining)",
                            task.Name, task.MaxRetries);
                    }
                }
                finally
                {
                    (_taskQueue as BackgroundTaskQueue)?.DecrementRunningCount();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in background task processor");
            }
        }

        _logger.LogInformation("Background task processor stopped");
    }
}

/// <summary>
/// Extension methods for registering background task services
/// </summary>
public static class BackgroundTaskExtensions
{
    /// <summary>
    /// Registers the background task queue and hosted service with the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBackgroundTaskQueue(this IServiceCollection services)
    {
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddHostedService<BackgroundTaskHostedService>();
        return services;
    }
}
