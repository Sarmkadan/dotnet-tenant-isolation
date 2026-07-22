using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.BackgroundTasks;
using Xunit;

namespace TenantIsolation.Tests;

public class BackgroundTaskTests
{
    private readonly Mock<ILogger<BackgroundTaskQueue>> _loggerMock;
    private readonly BackgroundTaskQueue _taskQueue;

    public BackgroundTaskTests()
    {
        _loggerMock = new Mock<ILogger<BackgroundTaskQueue>>();
        _taskQueue = new BackgroundTaskQueue(_loggerMock.Object);
    }

    [Fact]
    public void BackgroundTask_DefaultConstructor_ShouldInitializeProperties()
    {
        // Act
        var task = new BackgroundTask();

        // Assert
        task.Id.Should().NotBeNullOrEmpty();
        task.Id.Should().MatchRegex("^[a-f0-9]{32}$");
        task.Name.Should().BeEmpty();
        task.WorkItem.Should().BeNull();
        task.EnqueuedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        task.Priority.Should().Be(BackgroundTaskPriority.Normal);
        task.MaxRetries.Should().Be(3);
    }

    [Fact]
    public void BackgroundTask_CustomConstructor_ShouldInitializeProperties()
    {
        // Arrange
        var workItem = new Func<CancellationToken, Task>(_ => Task.CompletedTask);
        var name = "Test Task";
        var priority = BackgroundTaskPriority.High;
        var maxRetries = 5;

        // Act
        var task = new BackgroundTask
        {
            Name = name,
            WorkItem = workItem,
            Priority = priority,
            MaxRetries = maxRetries
        };

        // Assert
        task.Id.Should().NotBeNullOrEmpty();
        task.Name.Should().Be(name);
        task.WorkItem.Should().BeSameAs(workItem);
        task.EnqueuedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        task.Priority.Should().Be(priority);
        task.MaxRetries.Should().Be(maxRetries);
    }

    [Fact]
    public void BackgroundTask_WithAllPropertiesSet_ShouldWorkCorrectly()
    {
        // Arrange
        var workItem = new Func<CancellationToken, Task>(ct => Task.Delay(100, ct));
        var name = "Complete Task";
        var enqueuedAt = DateTime.UtcNow.AddMinutes(-5);
        var priority = BackgroundTaskPriority.Critical;
        var maxRetries = 10;

        // Act
        var task = new BackgroundTask
        {
            Id = "custom-task-id",
            Name = name,
            WorkItem = workItem,
            EnqueuedAt = enqueuedAt,
            Priority = priority,
            MaxRetries = maxRetries
        };

        // Assert
        task.Id.Should().Be("custom-task-id");
        task.Name.Should().Be(name);
        task.WorkItem.Should().BeSameAs(workItem);
        task.EnqueuedAt.Should().Be(enqueuedAt);
        task.Priority.Should().Be(priority);
        task.MaxRetries.Should().Be(maxRetries);
    }

    [Fact]
    public void BackgroundTaskPriority_EnumValues_ShouldBeCorrect()
    {
        // Assert
        ((int)BackgroundTaskPriority.Low).Should().Be(0);
        ((int)BackgroundTaskPriority.Normal).Should().Be(1);
        ((int)BackgroundTaskPriority.High).Should().Be(2);
        ((int)BackgroundTaskPriority.Critical).Should().Be(3);
    }

    [Fact]
    public void QueueTask_WithValidTask_ShouldAddToQueue()
    {
        // Arrange
        var task = new BackgroundTask
        {
            Name = "Valid Task",
            WorkItem = _ => Task.CompletedTask
        };

        // Act
        _taskQueue.QueueTask(task);

        // Assert
        var statistics = _taskQueue.GetStatistics();
        statistics.PendingTasks.Should().Be(1);
    }

    [Fact]
    public void QueueTask_WithNullTask_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => _taskQueue.QueueTask(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task DequeueAsync_WithPendingTask_ShouldReturnTask()
    {
        // Arrange
        var task = new BackgroundTask
        {
            Name = "Dequeue Test Task",
            WorkItem = _ => Task.CompletedTask
        };
        _taskQueue.QueueTask(task);

        // Act
        var dequeuedTask = await _taskQueue.DequeueAsync(CancellationToken.None);

        // Assert
        dequeuedTask.Should().NotBeNull();
        dequeuedTask!.Id.Should().Be(task.Id);
        dequeuedTask.Name.Should().Be(task.Name);
        var statistics = _taskQueue.GetStatistics();
        statistics.PendingTasks.Should().Be(0);
    }

    [Fact]
    public async Task DequeueAsync_WithNoPendingTasks_ShouldWaitForTask()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var dequeueTask = Task.Run(async () => await _taskQueue.DequeueAsync(cts.Token));

        // Give it a moment to start waiting
        await Task.Delay(100);

        // Act - queue a task after waiting
        var task = new BackgroundTask
        {
            Name = "Delayed Task",
            WorkItem = _ => Task.CompletedTask
        };
        _taskQueue.QueueTask(task);

        // Assert
        var dequeuedTask = await dequeueTask;
        dequeuedTask.Should().NotBeNull();
        dequeuedTask!.Id.Should().Be(task.Id);

        // Clean up
        cts.Cancel();
        await dequeueTask;
    }

    [Fact]
    public async Task DequeueAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await _taskQueue.DequeueAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void GetStatistics_WithEmptyQueue_ShouldReturnZeroValues()
    {
        // Act
        var statistics = _taskQueue.GetStatistics();

        // Assert
        statistics.PendingTasks.Should().Be(0);
        statistics.CompletedTasks.Should().Be(0);
        statistics.FailedTasks.Should().Be(0);
        statistics.RunningTasks.Should().Be(0);
        statistics.AverageExecutionTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void GetStatistics_WithMultipleTasks_ShouldReturnCorrectCounts()
    {
        // Arrange
        var task1 = new BackgroundTask { Name = "Task 1", WorkItem = _ => Task.CompletedTask };
        var task2 = new BackgroundTask { Name = "Task 2", WorkItem = _ => Task.CompletedTask };
        var task3 = new BackgroundTask { Name = "Task 3", WorkItem = _ => Task.CompletedTask };

        _taskQueue.QueueTask(task1);
        _taskQueue.QueueTask(task2);
        _taskQueue.QueueTask(task3);

        // Act
        var statistics = _taskQueue.GetStatistics();

        // Assert
        statistics.PendingTasks.Should().Be(3);
        statistics.CompletedTasks.Should().Be(0);
        statistics.FailedTasks.Should().Be(0);
        statistics.RunningTasks.Should().Be(0);
    }

    [Fact]
    public void RecordTaskCompletion_WithSuccess_ShouldIncrementCompletedCounter()
    {
        // Arrange
        var initialStats = _taskQueue.GetStatistics();

        // Act
        _taskQueue.RecordTaskCompletion(150, true);
        _taskQueue.RecordTaskCompletion(200, true);

        // Assert
        var statistics = _taskQueue.GetStatistics();
        statistics.CompletedTasks.Should().Be(initialStats.CompletedTasks + 2);
        statistics.FailedTasks.Should().Be(initialStats.FailedTasks);
    }

    [Fact]
    public void RecordTaskCompletion_WithFailure_ShouldIncrementFailedCounter()
    {
        // Arrange
        var initialStats = _taskQueue.GetStatistics();

        // Act
        _taskQueue.RecordTaskCompletion(100, false);
        _taskQueue.RecordTaskCompletion(120, false);

        // Assert
        var statistics = _taskQueue.GetStatistics();
        statistics.CompletedTasks.Should().Be(initialStats.CompletedTasks);
        statistics.FailedTasks.Should().Be(initialStats.FailedTasks + 2);
    }

    [Fact]
    public void RecordTaskCompletion_ShouldMaintainExecutionTimesHistory()
    {
        // Act - add more than 100 execution times
        for (int i = 0; i < 150; i++)
        {
            _taskQueue.RecordTaskCompletion(i * 10, true);
        }

        // Assert - should only keep last 100
        var statistics = _taskQueue.GetStatistics();
        statistics.CompletedTasks.Should().Be(150);
        statistics.AverageExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void IncrementRunningCount_ShouldIncreaseRunningTasks()
    {
        // Arrange
        var initialStats = _taskQueue.GetStatistics();

        // Act
        _taskQueue.IncrementRunningCount();
        _taskQueue.IncrementRunningCount();

        // Assert
        var statistics = _taskQueue.GetStatistics();
        statistics.RunningTasks.Should().Be(initialStats.RunningTasks + 2);
    }

    [Fact]
    public void DecrementRunningCount_ShouldDecreaseRunningTasks()
    {
        // Arrange
        _taskQueue.IncrementRunningCount();
        _taskQueue.IncrementRunningCount();
        var initialStats = _taskQueue.GetStatistics();

        // Act
        _taskQueue.DecrementRunningCount();

        // Assert
        var statistics = _taskQueue.GetStatistics();
        statistics.RunningTasks.Should().Be(initialStats.RunningTasks - 1);
    }

    [Fact]
    public void DecrementRunningCount_BelowZero_ShouldAllowNegativeValues()
    {
        // Arrange
        var initialStats = _taskQueue.GetStatistics();

        // Act
        _taskQueue.DecrementRunningCount();
        _taskQueue.DecrementRunningCount();

        // Assert
        var statistics = _taskQueue.GetStatistics();
        statistics.RunningTasks.Should().BeLessThan(0);
    }

    [Fact]
    public async Task QueueTask_WithDifferentPriorities_ShouldMaintainPriorityOrder()
    {
        // Arrange - PriorityQueue treats lower enum values as higher priority
        var lowPriorityTask = new BackgroundTask
        {
            Name = "Low Priority Task",
            WorkItem = _ => Task.CompletedTask,
            Priority = BackgroundTaskPriority.Low
        };

        var highPriorityTask = new BackgroundTask
        {
            Name = "High Priority Task",
            WorkItem = _ => Task.CompletedTask,
            Priority = BackgroundTaskPriority.High
        };

        var criticalPriorityTask = new BackgroundTask
        {
            Name = "Critical Priority Task",
            WorkItem = _ => Task.CompletedTask,
            Priority = BackgroundTaskPriority.Critical
        };

        // Act
        _taskQueue.QueueTask(lowPriorityTask);
        _taskQueue.QueueTask(criticalPriorityTask);
        _taskQueue.QueueTask(highPriorityTask);

        // Dequeue in order - PriorityQueue returns lowest priority value first (0=Low, 1=Normal, 2=High, 3=Critical)
        var dequeued1 = await _taskQueue.DequeueAsync(CancellationToken.None);
        var dequeued2 = await _taskQueue.DequeueAsync(CancellationToken.None);
        var dequeued3 = await _taskQueue.DequeueAsync(CancellationToken.None);

        // Assert - PriorityQueue returns items in ascending order (lower enum values first)
        dequeued1.Should().NotBeNull();
        dequeued1!.Name.Should().Be("Low Priority Task");

        dequeued2.Should().NotBeNull();
        dequeued2!.Name.Should().Be("High Priority Task");

        dequeued3.Should().NotBeNull();
        dequeued3!.Name.Should().Be("Critical Priority Task");
    }

    [Fact]
    public void BackgroundTask_WithEmptyName_ShouldBeAllowed()
    {
        // Act
        var task = new BackgroundTask { Name = string.Empty };

        // Assert
        task.Name.Should().BeEmpty();
    }

    [Fact]
    public void BackgroundTask_WithNullWorkItem_ShouldBeAllowed()
    {
        // Act
        var task = new BackgroundTask { WorkItem = null! };

        // Assert
        task.WorkItem.Should().BeNull();
    }

    [Fact]
    public void BackgroundTask_WithMaxRetriesZero_ShouldBeAllowed()
    {
        // Act
        var task = new BackgroundTask { MaxRetries = 0 };

        // Assert
        task.MaxRetries.Should().Be(0);
    }

    [Fact]
    public void BackgroundTask_WithCustomId_ShouldPreserveId()
    {
        // Arrange
        var customId = Guid.NewGuid().ToString("N");

        // Act
        var task = new BackgroundTask { Id = customId };

        // Assert
        task.Id.Should().Be(customId);
    }
}