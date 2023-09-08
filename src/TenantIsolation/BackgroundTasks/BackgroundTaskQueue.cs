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
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public Func<CancellationToken, Task> WorkItem { get; set; } = null!;
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
    public BackgroundTaskPriority Priority { get; set; } = BackgroundTaskPriority.Normal;
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Task priority levels
/// </summary>
public enum BackgroundTaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
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
    public int PendingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public int RunningTasks { get; set; }
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

    public BackgroundTaskQueue(ILogger<BackgroundTaskQueue> logger)
    {
        _queue = new PriorityQueue<BackgroundTask, BackgroundTaskPriority>();
        _signal = new SemaphoreSlim(0);
        _logger = logger;
    }

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

    public async Task<BackgroundTask?> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);

        lock (_queue)
        {
            return _queue.Count > 0 ? _queue.Dequeue() : null;
        }
    }

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
    /// Internal method to record task completion
    /// </summary>
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

    public void IncrementRunningCount() => Interlocked.Increment(ref _runningTasks);
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

    public BackgroundTaskHostedService(
        IBackgroundTaskQueue taskQueue,
        ILogger<BackgroundTaskHostedService> logger,
        IServiceProvider serviceProvider)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

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
    public static IServiceCollection AddBackgroundTaskQueue(this IServiceCollection services)
    {
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddHostedService<BackgroundTaskHostedService>();
        return services;
    }
}
