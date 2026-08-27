using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.BackgroundTasks;
using Xunit;

namespace TenantIsolation.Tests.BackgroundTasks;

/// <summary>
/// Tests for the <see cref="BackgroundTaskQueue"/> class.
/// </summary>
public class BackgroundTaskQueueTests
{
    private readonly Mock<ILogger<BackgroundTaskQueue>> _loggerMock;
    private readonly BackgroundTaskQueue _queue;

/// <summary>
/// Initializes a new instance of the <see cref="BackgroundTaskQueueTests"/> class.
/// Sets up a mock logger and a new <see cref="BackgroundTaskQueue"/> instance.
/// </summary>
    public BackgroundTaskQueueTests()
    {
        _loggerMock = new Mock<ILogger<BackgroundTaskQueue>>();
        _queue = new BackgroundTaskQueue(_loggerMock.Object);
    }

/// <summary>
/// Verifies that queuing a null task throws an ArgumentNullException.
/// </summary>
    [Fact]
    public void QueueTask_WithNullTask_ThrowsArgumentNullException()
    {
        // Arrange
        BackgroundTask? nullTask = null;
        _loggerMock.Object.LogInformation("Executing test: {TestName}", nameof(QueueTask_WithNullTask_ThrowsArgumentNullException));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _queue.QueueTask(nullTask!));
    }

/// <summary>
/// Verifies that queuing a valid task adds it to the queue and signals that a task is available.
/// </summary>
    [Fact]
    public void QueueTask_WithValidTask_AddsToQueueAndSignals()
    {
        // Arrange
        var task = new BackgroundTask
        {
            Name = "Test Task",
            WorkItem = _ => Task.CompletedTask
        };
        _loggerMock.Object.LogInformation("Executing test: {TestName} with task {TaskName}", nameof(QueueTask_WithValidTask_AddsToQueueAndSignals), task.Name);

        // Act
        _queue.QueueTask(task);

        // Assert
        var stats = _queue.GetStatistics();
        stats.PendingTasks.Should().Be(1);
        _loggerMock.Object.LogInformation("Test {TestName} completed successfully", nameof(QueueTask_WithValidTask_AddsToQueueAndSignals));
    }

/// <summary>
/// Verifies that DequeueAsync waits when the queue is empty until a task is available or timeout occurs.
/// </summary>
    [Fact]
    public async Task DequeueAsync_WithEmptyQueue_WaitsUntilItemAvailable()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var stopwatch = Stopwatch.StartNew();
        _loggerMock.Object.LogInformation("Executing test: {TestName} with timeout {TimeoutMs}ms", nameof(DequeueAsync_WithEmptyQueue_WaitsUntilItemAvailable), 100);

        // Act
        BackgroundTask? task = null;
        try
        {
            task = await _queue.DequeueAsync(cts.Token);
        }
        catch (OperationCanceledException ex)
        {
            // Expected when timeout occurs
            _loggerMock.Object.LogWarning(ex, "Test {TestName} caught expected {ExceptionType}: {Message}", nameof(DequeueAsync_WithEmptyQueue_WaitsUntilItemAvailable), nameof(OperationCanceledException), ex.Message);
        }

        // Assert
        stopwatch.Stop();
        task.Should().BeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200); // Should not wait long
        _loggerMock.Object.LogInformation("Test {TestName} completed in {ElapsedMs}ms", nameof(DequeueAsync_WithEmptyQueue_WaitsUntilItemAvailable), stopwatch.ElapsedMilliseconds);
    }

/// <summary>
/// Verifies that DequeueAsync returns a task immediately when one is available in the queue.
/// </summary>
    [Fact]
    public async Task DequeueAsync_WithItemAvailable_ReturnsTaskImmediately()
    {
        // Arrange
        var expectedTask = new BackgroundTask
        {
            Name = "Expected Task",
            WorkItem = _ => Task.CompletedTask
        };
        _loggerMock.Object.LogInformation("Executing test: {TestName} arranging test with {TaskName}", nameof(DequeueAsync_WithItemAvailable_ReturnsTaskImmediately), expectedTask.Name);
        _queue.QueueTask(expectedTask);

        // Act
        _loggerMock.Object.LogInformation("Test {TestName} attempting to dequeue", nameof(DequeueAsync_WithItemAvailable_ReturnsTaskImmediately));
        var actualTask = await _queue.DequeueAsync(CancellationToken.None);

        // Assert
        _loggerMock.Object.LogInformation("Test {TestName} asserting on result", nameof(DequeueAsync_WithItemAvailable_ReturnsTaskImmediately));
        actualTask.Should().NotBeNull();
        actualTask.Should().BeSameAs(expectedTask);
        var stats = _queue.GetStatistics();
        stats.PendingTasks.Should().Be(0);
        _loggerMock.Object.LogInformation("Test {TestName} completed successfully", nameof(DequeueAsync_WithItemAvailable_ReturnsTaskImmediately));
    }

/// <summary>
/// Verifies that DequeueAsync returns tasks in the correct priority order (lowest priority value first).
/// </summary>
    [Fact]
    public async Task DequeueAsync_WithMultipleTasks_ReturnsTasksInPriorityOrder()
    {
        // Arrange - PriorityQueue uses lower values as higher priority (min-heap)
        _loggerMock.Object.LogInformation("Executing test: {TestName}", nameof(DequeueAsync_WithMultipleTasks_ReturnsTasksInPriorityOrder));
        var lowPriorityTask = new BackgroundTask
        {
            Name = "Low Priority Task",
            Priority = BackgroundTaskPriority.Low,
            WorkItem = _ => Task.CompletedTask
        };

        var highPriorityTask = new BackgroundTask
        {
            Name = "High Priority Task",
            Priority = BackgroundTaskPriority.High,
            WorkItem = _ => Task.CompletedTask
        };

        var normalPriorityTask = new BackgroundTask
        {
            Name = "Normal Priority Task",
            Priority = BackgroundTaskPriority.Normal,
            WorkItem = _ => Task.CompletedTask
        };

        _loggerMock.Object.LogInformation("Test {TestName}: Queuing tasks", nameof(DequeueAsync_WithMultipleTasks_ReturnsTasksInPriorityOrder));
        _queue.QueueTask(lowPriorityTask);
        _queue.QueueTask(highPriorityTask);
        _queue.QueueTask(normalPriorityTask);

        // Act
        _loggerMock.Object.LogInformation("Test {TestName}: Starting dequeue operations", nameof(DequeueAsync_WithMultipleTasks_ReturnsTasksInPriorityOrder));
        var firstTask = await _queue.DequeueAsync(CancellationToken.None);
        var secondTask = await _queue.DequeueAsync(CancellationToken.None);
        var thirdTask = await _queue.DequeueAsync(CancellationToken.None);

        // Assert - PriorityQueue: Low=0 (highest), Normal=1, High=2 (lowest priority)
        _loggerMock.Object.LogInformation("Test {TestName}: Starting assertions", nameof(DequeueAsync_WithMultipleTasks_ReturnsTasksInPriorityOrder));
        firstTask.Should().NotBeNull();
        firstTask!.Name.Should().Be("Low Priority Task");

        secondTask.Should().NotBeNull();
        secondTask!.Name.Should().Be("Normal Priority Task");

        thirdTask.Should().NotBeNull();
        thirdTask!.Name.Should().Be("High Priority Task");
        _loggerMock.Object.LogInformation("Test {TestName}: All assertions passed", nameof(DequeueAsync_WithMultipleTasks_ReturnsTasksInPriorityOrder));
    }

/// <summary>
/// Verifies that DequeueAsync can be cancelled by a cancellation token while waiting for a task.
/// </summary>
    [Fact]
    public async Task DequeueAsync_WithCancellationToken_CancelsWaitingOperation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var waitTask = _queue.DequeueAsync(cts.Token);
        _loggerMock.Object.LogInformation("Executing test: {TestName} arranging test", nameof(DequeueAsync_WithCancellationToken_CancelsWaitingOperation));

        // Act - cancel immediately
        _loggerMock.Object.LogInformation("Test {TestName} cancelling {Operation}", nameof(DequeueAsync_WithCancellationToken_CancelsWaitingOperation), nameof(waitTask));
        cts.Cancel();

        // Assert - SemaphoreSlim.WaitAsync throws OperationCanceledException, not TaskCanceledException
        _loggerMock.Object.LogInformation("Test {TestName} asserting on result", nameof(DequeueAsync_WithCancellationToken_CancelsWaitingOperation));
        await Assert.ThrowsAsync<OperationCanceledException>(() => waitTask);
        _loggerMock.Object.LogInformation("Test {TestName} completed successfully", nameof(DequeueAsync_WithCancellationToken_CancelsWaitingOperation));
    }

/// <summary>
/// Verifies that the queue handles concurrent producers correctly, ensuring all tasks are eventually dequeued.
/// </summary>
    [Fact]
    public async Task DequeueAsync_WithConcurrentProducers_HandlesCorrectly()
    {
        // Arrange
        var tasks = new ConcurrentBag<BackgroundTask>();
        var producerCount = 5;
        var tasksPerProducer = 10;
        _loggerMock.Object.LogInformation("Executing test: {TestName} with {ProducerCount} producers and {TasksPerProducer} tasks per producer", nameof(DequeueAsync_WithConcurrentProducers_HandlesCorrectly), producerCount, tasksPerProducer);

        // Create producer tasks
        var producerTasks = new Task[producerCount];
        for (int i = 0; i < producerCount; i++)
        {
            var producerId = i;
            producerTasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < tasksPerProducer; j++)
                {
                    var task = new BackgroundTask
                    {
                        Name = $"Producer{producerId}-Task{j}",
                        WorkItem = _ => Task.CompletedTask
                    };
                    _queue.QueueTask(task);
                    tasks.Add(task);
                }
            });
        }

        // Wait for all producers to finish
        await Task.WhenAll(producerTasks);
        _loggerMock.Object.LogInformation("Test {TestName}: All producers have finished", nameof(DequeueAsync_WithConcurrentProducers_HandlesCorrectly));

        // Act - consume all tasks
        _loggerMock.Object.LogInformation("Test {TestName}: Starting to consume tasks", nameof(DequeueAsync_WithConcurrentProducers_HandlesCorrectly));
        var consumedTasks = new List<BackgroundTask>();
        while (consumedTasks.Count < producerCount * tasksPerProducer)
        {
            var task = await _queue.DequeueAsync(CancellationToken.None);
            if (task != null)
            {
                consumedTasks.Add(task);
            }
        }
        _loggerMock.Object.LogInformation("Test {TestName}: Consumed {ConsumedCount} tasks", nameof(DequeueAsync_WithConcurrentProducers_HandlesCorrectly), consumedTasks.Count);

        // Assert
        consumedTasks.Should().HaveCount(producerCount * tasksPerProducer);
        _loggerMock.Object.LogInformation("Test {TestName}: Assertion on count passed", nameof(DequeueAsync_WithConcurrentProducers_HandlesCorrectly));

        // Verify all tasks were consumed (order doesn't matter for concurrent producers)
        var taskNames = consumedTasks.Select(t => t.Name).ToHashSet();
        for (int i = 0; i < producerCount; i++)
        {
            for (int j = 0; j < tasksPerProducer; j++)
            {
                var expectedName = $"Producer{i}-Task{j}";
                taskNames.Should().Contain(expectedName);
            }
        }
        _loggerMock.Object.LogInformation("Test {TestName}: All task names verified", nameof(DequeueAsync_WithConcurrentProducers_HandlesCorrectly));
    }

/// <summary>
/// Verifies that DequeueAsync returns null when a timeout occurs while waiting for a task.
/// </summary>
    [Fact]
    public async Task DequeueAsync_WithTimeout_ReturnsNullWhenTimeout()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        _loggerMock.Object.LogInformation("Executing test: {TestName} with timeout {TimeoutMs}ms", nameof(DequeueAsync_WithTimeout_ReturnsNullWhenTimeout), 50);

        // Act
        BackgroundTask? task = null;
        try
        {
            task = await _queue.DequeueAsync(cts.Token);
        }
        catch (OperationCanceledException ex)
        {
            // Expected when timeout occurs
            _loggerMock.Object.LogWarning(ex, "Test {TestName} caught expected {ExceptionType}: {Message}", nameof(DequeueAsync_WithTimeout_ReturnsNullWhenTimeout), nameof(OperationCanceledException), ex.Message);
        }

        // Assert
        task.Should().BeNull();
        _loggerMock.Object.LogInformation("Test {TestName} completed successfully", nameof(DequeueAsync_WithTimeout_ReturnsNullWhenTimeout));
    }

/// <summary>
/// Verifies that GetStatistics returns zero values for all counts when the queue is empty.
/// </summary>
    [Fact]
    public void GetStatistics_WithEmptyQueue_ReturnsZeroValues()
    {
        _loggerMock.Object.LogInformation("Executing test: {TestName}", nameof(GetStatistics_WithEmptyQueue_ReturnsZeroValues));
        // Act
        var stats = _queue.GetStatistics();
        _loggerMock.Object.LogInformation("Test {TestName}: Got statistics", nameof(GetStatistics_WithEmptyQueue_ReturnsZeroValues));

        // Assert
        stats.PendingTasks.Should().Be(0);
        stats.CompletedTasks.Should().Be(0);
        stats.FailedTasks.Should().Be(0);
        stats.RunningTasks.Should().Be(0);
        stats.AverageExecutionTime.Should().Be(TimeSpan.Zero);
        _loggerMock.Object.LogInformation("Test {TestName}: Assertion passed", nameof(GetStatistics_WithEmptyQueue_ReturnsZeroValues));
    }

/// <summary>
/// Verifies that GetStatistics returns correct counts for pending tasks when tasks are queued.
/// </summary>
    [Fact]
    public void GetStatistics_WithTasks_ReturnsCorrectCounts()
    {
        _loggerMock.Object.LogInformation("Executing test: {TestName}", nameof(GetStatistics_WithTasks_ReturnsCorrectCounts));
        // Arrange - add some tasks
        var task1 = new BackgroundTask { Name = "Task 1", WorkItem = _ => Task.CompletedTask };
        var task2 = new BackgroundTask { Name = "Task 2", WorkItem = _ => Task.CompletedTask };
        _queue.QueueTask(task1);
        _queue.QueueTask(task2);

        // Act
        var stats = _queue.GetStatistics();
        _loggerMock.Object.LogInformation("Test {TestName}: Got statistics", nameof(GetStatistics_WithTasks_ReturnsCorrectCounts));

        // Assert
        stats.PendingTasks.Should().Be(2);
        stats.CompletedTasks.Should().Be(0);
        stats.FailedTasks.Should().Be(0);
        stats.RunningTasks.Should().Be(0);
        _loggerMock.Object.LogInformation("Test {TestName}: Assertion passed", nameof(GetStatistics_WithTasks_ReturnsCorrectCounts));
    }

/// <summary>
/// Verifies that RecordTaskCompletion correctly records execution time and updates completed/failed task counts.
/// </summary>
    [Fact]
    public async Task RecordTaskCompletion_RecordsExecutionTimeAndUpdatesCounts()
    {
        _loggerMock.Object.LogInformation("Executing test: {TestName}", nameof(RecordTaskCompletion_RecordsExecutionTimeAndUpdatesCounts));
        // Arrange
        var executionTimeMs = 1234L;

        // Act
        _queue.RecordTaskCompletion(executionTimeMs, isSuccess: true);
        _queue.RecordTaskCompletion(executionTimeMs + 100, isSuccess: false);
        _queue.RecordTaskCompletion(executionTimeMs + 200, isSuccess: true);

        // Act - get stats
        var stats = _queue.GetStatistics();
        _loggerMock.Object.LogInformation("Test {TestName}: Recorded completions and got stats", nameof(RecordTaskCompletion_RecordsExecutionTimeAndUpdatesCounts));

        // Assert
        stats.CompletedTasks.Should().Be(2);
        stats.FailedTasks.Should().Be(1);
        var expectedAvgMs = (1234 + 1334 + 1434) / 3.0;
        stats.AverageExecutionTime.TotalMilliseconds.Should().BeApproximately(expectedAvgMs, 1);
        _loggerMock.Object.LogInformation("Test {TestName}: Assertion passed", nameof(RecordTaskCompletion_RecordsExecutionTimeAndUpdatesCounts));
    }

/// <summary>
/// Verifies that IncrementRunningCount and DecrementRunningCount correctly update the running task count.
/// </summary>
    [Fact]
    public void IncrementRunningCount_And_DecrementRunningCount_UpdatesRunningTasks()
    {
        _loggerMock.Object.LogInformation("Executing test: {TestName}", nameof(IncrementRunningCount_And_DecrementRunningCount_UpdatesRunningTasks));
        // Arrange
        var initialStats = _queue.GetStatistics();
        initialStats.RunningTasks.Should().Be(0);

        // Act
        _queue.IncrementRunningCount();
        var afterIncrement = _queue.GetStatistics();

        _queue.IncrementRunningCount();
        var afterSecondIncrement = _queue.GetStatistics();

        _queue.DecrementRunningCount();
        var afterDecrement = _queue.GetStatistics();
        _loggerMock.Object.LogInformation("Test {TestName}: Incremented and decremented counts", nameof(IncrementRunningCount_And_DecrementRunningCount_UpdatesRunningTasks));

        // Assert
        afterIncrement.RunningTasks.Should().Be(1);
        afterSecondIncrement.RunningTasks.Should().Be(2);
        afterDecrement.RunningTasks.Should().Be(1);
        _loggerMock.Object.LogInformation("Test {TestName}: Assertion passed", nameof(IncrementRunningCount_And_DecrementRunningCount_UpdatesRunningTasks));
    }

/// <summary>
/// Verifies that tasks are queued and processed in the correct priority order (lowest priority value first).
/// </summary>
    [Fact]
    public async Task QueueTask_WithDifferentPriorities_ProcessesInCorrectOrder()
    {
        _loggerMock.Object.LogInformation("Executing test: {TestName}", nameof(QueueTask_WithDifferentPriorities_ProcessesInCorrectOrder));
        // Arrange - create tasks with different priorities
        var lowTask = new BackgroundTask { Name = "Low Task", Priority = BackgroundTaskPriority.Low, WorkItem = _ => Task.CompletedTask };
        var normalTask = new BackgroundTask { Name = "Normal Task", Priority = BackgroundTaskPriority.Normal, WorkItem = _ => Task.CompletedTask };
        var highTask = new BackgroundTask { Name = "High Task", Priority = BackgroundTaskPriority.High, WorkItem = _ => Task.CompletedTask };
        var criticalTask = new BackgroundTask { Name = "Critical Task", Priority = BackgroundTaskPriority.Critical, WorkItem = _ => Task.CompletedTask };

        // Queue in random order
        _queue.QueueTask(normalTask);
        _queue.QueueTask(criticalTask);
        _queue.QueueTask(lowTask);
        _queue.QueueTask(highTask);
        _loggerMock.Object.LogInformation("Test {TestName}: Queued tasks", nameof(QueueTask_WithDifferentPriorities_ProcessesInCorrectOrder));

        // Act - dequeue all
        var task1 = await _queue.DequeueAsync(CancellationToken.None);
        var task2 = await _queue.DequeueAsync(CancellationToken.None);
        var task3 = await _queue.DequeueAsync(CancellationToken.None);
        var task4 = await _queue.DequeueAsync(CancellationToken.None);
        _loggerMock.Object.LogInformation("Test {TestName}: Dequeued tasks", nameof(QueueTask_WithDifferentPriorities_ProcessesInCorrectOrder));

        // Assert
        task1.Should().NotBeNull();
        task1!.Name.Should().Be("Low Task");
        task2.Should().NotBeNull();
        task2!.Name.Should().Be("Normal Task");
        task3.Should().NotBeNull();
        task3!.Name.Should().Be("High Task");
        task4.Should().NotBeNull();
        task4!.Name.Should().Be("Critical Task");
        _loggerMock.Object.LogInformation("Test {TestName}: Assertion passed", nameof(QueueTask_WithDifferentPriorities_ProcessesInCorrectOrder));
    }

/// <summary>
/// Verifies that queuing a task and then immediately dequeuing it returns the task without waiting.
/// </summary>
    [Fact]
    public async Task DequeueAsync_AfterQueueTask_ReturnsTaskWithoutWaiting()
    {
        _loggerMock.Object.LogInformation("Executing test: {TestName}", nameof(DequeueAsync_AfterQueueTask_ReturnsTaskWithoutWaiting));
        // Arrange
        var task = new BackgroundTask { Name = "Immediate Task", WorkItem = _ => Task.CompletedTask };

        // Act - queue then immediately dequeue
        _queue.QueueTask(task);
        var dequeuedTask = await _queue.DequeueAsync(CancellationToken.None);
        _loggerMock.Object.LogInformation("Test {TestName}: Queued and dequeued task", nameof(DequeueAsync_AfterQueueTask_ReturnsTaskWithoutWaiting));

        // Assert
        dequeuedTask.Should().NotBeNull();
        dequeuedTask.Should().BeSameAs(task);
        _loggerMock.Object.LogInformation("Test {TestName}: Assertion passed", nameof(DequeueAsync_AfterQueueTask_ReturnsTaskWithoutWaiting));
    }

/// <summary>
/// Verifies that multiple calls to DequeueAsync return all queued tasks when sufficient tasks are available.
/// </summary>
    [Fact]
    public async Task MultipleDequeueAsyncCalls_WithSufficientTasks_ReturnsAllTasks()
    {
        _loggerMock.Object.LogInformation("Executing test: {TestName}", nameof(MultipleDequeueAsyncCalls_WithSufficientTasks_ReturnsAllTasks));
        // Arrange
        var tasks = new List<BackgroundTask>();
        for (int i = 0; i < 10; i++)
        {
            var task = new BackgroundTask { Name = $"Task {i}", WorkItem = _ => Task.CompletedTask };
            tasks.Add(task);
            _queue.QueueTask(task);
        }
        _loggerMock.Object.LogInformation("Test {TestName}: Queued 10 tasks", nameof(MultipleDequeueAsyncCalls_WithSufficientTasks_ReturnsAllTasks));

        // Act
        var dequeuedTasks = new List<BackgroundTask>();
        for (int i = 0; i < 10; i++)
        {
            var task = await _queue.DequeueAsync(CancellationToken.None);
            if (task != null)
            {
                dequeuedTasks.Add(task);
            }
        }
        _loggerMock.Object.LogInformation("Test {TestName}: Dequeued 10 tasks", nameof(MultipleDequeueAsyncCalls_WithSufficientTasks_ReturnsAllTasks));

        // Assert
        dequeuedTasks.Should().HaveCount(10);
        dequeuedTasks.Select(t => t.Name).Should().BeEquivalentTo(tasks.Select(t => t.Name));
        _loggerMock.Object.LogInformation("Test {TestName}: Assertion passed", nameof(MultipleDequeueAsyncCalls_WithSufficientTasks_ReturnsAllTasks));
    }
}
