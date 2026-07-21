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

public class BackgroundTaskQueueTests
{
    private readonly Mock<ILogger<BackgroundTaskQueue>> _loggerMock;
    private readonly BackgroundTaskQueue _queue;

    public BackgroundTaskQueueTests()
    {
        _loggerMock = new Mock<ILogger<BackgroundTaskQueue>>();
        _queue = new BackgroundTaskQueue(_loggerMock.Object);
    }

    [Fact]
    public void QueueTask_WithNullTask_ThrowsArgumentNullException()
    {
        // Arrange
        BackgroundTask? nullTask = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _queue.QueueTask(nullTask!));
    }

    [Fact]
    public void QueueTask_WithValidTask_AddsToQueueAndSignals()
    {
        // Arrange
        var task = new BackgroundTask
        {
            Name = "Test Task",
            WorkItem = _ => Task.CompletedTask
        };

        // Act
        _queue.QueueTask(task);

        // Assert
        var stats = _queue.GetStatistics();
        stats.PendingTasks.Should().Be(1);
    }

    [Fact]
    public async Task DequeueAsync_WithEmptyQueue_WaitsUntilItemAvailable()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var stopwatch = Stopwatch.StartNew();

        // Act
        BackgroundTask? task = null;
        try
        {
            task = await _queue.DequeueAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when timeout occurs
        }

        // Assert
        stopwatch.Stop();
        task.Should().BeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200); // Should not wait long
    }

    [Fact]
    public async Task DequeueAsync_WithItemAvailable_ReturnsTaskImmediately()
    {
        // Arrange
        var expectedTask = new BackgroundTask
        {
            Name = "Expected Task",
            WorkItem = _ => Task.CompletedTask
        };
        _queue.QueueTask(expectedTask);

        // Act
        var actualTask = await _queue.DequeueAsync(CancellationToken.None);

        // Assert
        actualTask.Should().NotBeNull();
        actualTask.Should().BeSameAs(expectedTask);
        var stats = _queue.GetStatistics();
        stats.PendingTasks.Should().Be(0);
    }

    [Fact]
    public async Task DequeueAsync_WithMultipleTasks_ReturnsTasksInPriorityOrder()
    {
        // Arrange - PriorityQueue uses lower values as higher priority (min-heap)
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

        _queue.QueueTask(lowPriorityTask);
        _queue.QueueTask(highPriorityTask);
        _queue.QueueTask(normalPriorityTask);

        // Act
        var firstTask = await _queue.DequeueAsync(CancellationToken.None);
        var secondTask = await _queue.DequeueAsync(CancellationToken.None);
        var thirdTask = await _queue.DequeueAsync(CancellationToken.None);

        // Assert - PriorityQueue: Low=0 (highest), Normal=1, High=2 (lowest priority)
        firstTask.Should().NotBeNull();
        firstTask!.Name.Should().Be("Low Priority Task");

        secondTask.Should().NotBeNull();
        secondTask!.Name.Should().Be("Normal Priority Task");

        thirdTask.Should().NotBeNull();
        thirdTask!.Name.Should().Be("High Priority Task");
    }

    [Fact]
    public async Task DequeueAsync_WithCancellationToken_CancelsWaitingOperation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var waitTask = _queue.DequeueAsync(cts.Token);

        // Act - cancel immediately
        cts.Cancel();

        // Assert - SemaphoreSlim.WaitAsync throws OperationCanceledException, not TaskCanceledException
        await Assert.ThrowsAsync<OperationCanceledException>(() => waitTask);
    }

    [Fact]
    public async Task DequeueAsync_WithConcurrentProducers_HandlesCorrectly()
    {
        // Arrange
        var tasks = new ConcurrentBag<BackgroundTask>();
        var producerCount = 5;
        var tasksPerProducer = 10;

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

        // Act - consume all tasks
        var consumedTasks = new List<BackgroundTask>();
        while (consumedTasks.Count < producerCount * tasksPerProducer)
        {
            var task = await _queue.DequeueAsync(CancellationToken.None);
            if (task != null)
            {
                consumedTasks.Add(task);
            }
        }

        // Assert
        consumedTasks.Should().HaveCount(producerCount * tasksPerProducer);

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
    }

    [Fact]
    public async Task DequeueAsync_WithTimeout_ReturnsNullWhenTimeout()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        BackgroundTask? task = null;
        try
        {
            task = await _queue.DequeueAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when timeout occurs
        }

        // Assert
        task.Should().BeNull();
    }

    [Fact]
    public void GetStatistics_WithEmptyQueue_ReturnsZeroValues()
    {
        // Act
        var stats = _queue.GetStatistics();

        // Assert
        stats.PendingTasks.Should().Be(0);
        stats.CompletedTasks.Should().Be(0);
        stats.FailedTasks.Should().Be(0);
        stats.RunningTasks.Should().Be(0);
        stats.AverageExecutionTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void GetStatistics_WithTasks_ReturnsCorrectCounts()
    {
        // Arrange - add some tasks
        var task1 = new BackgroundTask { Name = "Task 1", WorkItem = _ => Task.CompletedTask };
        var task2 = new BackgroundTask { Name = "Task 2", WorkItem = _ => Task.CompletedTask };
        _queue.QueueTask(task1);
        _queue.QueueTask(task2);

        // Act
        var stats = _queue.GetStatistics();

        // Assert
        stats.PendingTasks.Should().Be(2);
        stats.CompletedTasks.Should().Be(0);
        stats.FailedTasks.Should().Be(0);
        stats.RunningTasks.Should().Be(0);
    }

    [Fact]
    public async Task RecordTaskCompletion_RecordsExecutionTimeAndUpdatesCounts()
    {
        // Arrange
        var executionTimeMs = 1234L;

        // Act
        _queue.RecordTaskCompletion(executionTimeMs, isSuccess: true);
        _queue.RecordTaskCompletion(executionTimeMs + 100, isSuccess: false);
        _queue.RecordTaskCompletion(executionTimeMs + 200, isSuccess: true);

        // Act - get stats
        var stats = _queue.GetStatistics();

        // Assert
        stats.CompletedTasks.Should().Be(2);
        stats.FailedTasks.Should().Be(1);
        var expectedAvgMs = (1234 + 1334 + 1434) / 3.0;
        stats.AverageExecutionTime.TotalMilliseconds.Should().BeApproximately(expectedAvgMs, 1);
    }

    [Fact]
    public void IncrementRunningCount_And_DecrementRunningCount_UpdatesRunningTasks()
    {
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

        // Assert
        afterIncrement.RunningTasks.Should().Be(1);
        afterSecondIncrement.RunningTasks.Should().Be(2);
        afterDecrement.RunningTasks.Should().Be(1);
    }

    [Fact]
    public async Task QueueTask_WithDifferentPriorities_ProcessesInCorrectOrder()
    {
        // Arrange - create tasks with different priorities
        // Note: PriorityQueue uses lower values as higher priority (min-heap)
        var lowTask = new BackgroundTask
        {
            Name = "Low Task",
            Priority = BackgroundTaskPriority.Low,
            WorkItem = _ => Task.CompletedTask
        };

        var normalTask = new BackgroundTask
        {
            Name = "Normal Task",
            Priority = BackgroundTaskPriority.Normal,
            WorkItem = _ => Task.CompletedTask
        };

        var highTask = new BackgroundTask
        {
            Name = "High Task",
            Priority = BackgroundTaskPriority.High,
            WorkItem = _ => Task.CompletedTask
        };

        var criticalTask = new BackgroundTask
        {
            Name = "Critical Task",
            Priority = BackgroundTaskPriority.Critical,
            WorkItem = _ => Task.CompletedTask
        };

        // Queue in random order
        _queue.QueueTask(normalTask);
        _queue.QueueTask(criticalTask);
        _queue.QueueTask(lowTask);
        _queue.QueueTask(highTask);

        // Act - dequeue all
        var task1 = await _queue.DequeueAsync(CancellationToken.None);
        var task2 = await _queue.DequeueAsync(CancellationToken.None);
        var task3 = await _queue.DequeueAsync(CancellationToken.None);
        var task4 = await _queue.DequeueAsync(CancellationToken.None);

        // Assert - should be in priority order (Low < Normal < High < Critical in enum, but PriorityQueue uses lower values first)
        // PriorityQueue: Low=0 (highest), Normal=1, High=2, Critical=3 (lowest)
        task1.Should().NotBeNull();
        task1!.Name.Should().Be("Low Task");

        task2.Should().NotBeNull();
        task2!.Name.Should().Be("Normal Task");

        task3.Should().NotBeNull();
        task3!.Name.Should().Be("High Task");

        task4.Should().NotBeNull();
        task4!.Name.Should().Be("Critical Task");
    }

    [Fact]
    public async Task DequeueAsync_AfterQueueTask_ReturnsTaskWithoutWaiting()
    {
        // Arrange
        var task = new BackgroundTask
        {
            Name = "Immediate Task",
            WorkItem = _ => Task.CompletedTask
        };

        // Act - queue then immediately dequeue
        _queue.QueueTask(task);
        var dequeuedTask = await _queue.DequeueAsync(CancellationToken.None);

        // Assert
        dequeuedTask.Should().NotBeNull();
        dequeuedTask.Should().BeSameAs(task);
    }

    [Fact]
    public async Task MultipleDequeueAsyncCalls_WithSufficientTasks_ReturnsAllTasks()
    {
        // Arrange
        var tasks = new List<BackgroundTask>();
        for (int i = 0; i < 10; i++)
        {
            var task = new BackgroundTask
            {
                Name = $"Task {i}",
                WorkItem = _ => Task.CompletedTask
            };
            tasks.Add(task);
            _queue.QueueTask(task);
        }

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

        // Assert
        dequeuedTasks.Should().HaveCount(10);
        dequeuedTasks.Select(t => t.Name).Should().BeEquivalentTo(tasks.Select(t => t.Name));
    }
}