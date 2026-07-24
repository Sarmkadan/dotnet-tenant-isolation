using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TenantIsolation.Events;
using Xunit;

namespace TenantIsolation.Tests;

/// <summary>
/// Tests for EventPublisher error paths and event-type routing behavior
/// </summary>
public class EventPublisherErrorPathsTests
{
    private class TestEvent : TenantEvent
    {
        public TestEvent() => TenantId = Guid.NewGuid();
    }

    private class TestSubEvent : TestEvent
    {
        public TestSubEvent() => TenantId = Guid.NewGuid();
    }

    private class AnotherTestEvent : TenantEvent
    {
        public AnotherTestEvent() => TenantId = Guid.NewGuid();
    }

    private static EventBus CreateEventBus()
    {
        return new EventBus(
            NullLogger<EventBus>.Instance,
            Options.Create(new PublisherResilienceOptions
            {
                MaxRetries = 0,
                BaseDelay = TimeSpan.FromMilliseconds(1)
            }),
            new LoggingDeadLetterSink(NullLogger<LoggingDeadLetterSink>.Instance)
        );
    }

    private static EventPublisher CreateEventPublisher(EventBus? eventBus = null)
    {
        eventBus ??= CreateEventBus();
        var httpContextAccessor = new FakeHttpContextAccessor();
        var logger = NullLogger<EventPublisher>.Instance;
        return new EventPublisher(eventBus, logger, httpContextAccessor);
    }

    [Fact]
    public async Task PublishAsync_EventWithZeroSubscribers_CompletesWithoutError()
    {
        // Arrange
        var eventBus = CreateEventBus();
        var publisher = CreateEventPublisher(eventBus);
        var @event = new TestEvent();

        // Act & Assert - should complete without throwing
        await publisher.PublishAsync(@event);
    }

    [Fact]
    public async Task PublishAsync_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreateEventPublisher();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => publisher.PublishAsync<TestEvent>(null!));

        Assert.Equal("event", exception.ParamName);
    }

    [Fact]
    public async Task PublishAsync_SubtypeEventWithBaseTypeSubscribers_CovariantDispatchBehavior()
    {
        // Arrange
        var eventBus = CreateEventBus();
        var publisher = CreateEventPublisher(eventBus);
        var subEvent = new TestSubEvent();
        var receivedEvents = new List<TenantEvent>();

        // Subscribe to base type
        eventBus.Subscribe<TestEvent>(e =>
        {
            receivedEvents.Add(e);
            return Task.CompletedTask;
        });

        // Act - publish derived event
        await publisher.PublishAsync(subEvent);

        // Assert - verify whether base-type subscribers receive derived events
        // This test documents the current behavior for covariant dispatch
        Assert.Single(receivedEvents);
        Assert.IsType<TestSubEvent>(receivedEvents[0]);
    }

    [Fact]
    public async Task PublishAsync_DerivedEventWithDerivedTypeSubscribers_ExactTypeMatch()
    {
        // Arrange
        var eventBus = CreateEventBus();
        var publisher = CreateEventPublisher(eventBus);
        var subEvent = new TestSubEvent();
        var receivedEvents = new List<TenantEvent>();

        // Subscribe to exact derived type
        eventBus.Subscribe<TestSubEvent>(e =>
        {
            receivedEvents.Add(e);
            return Task.CompletedTask;
        });

        // Act - publish derived event
        await publisher.PublishAsync(subEvent);

        // Assert
        Assert.Single(receivedEvents);
        Assert.IsType<TestSubEvent>(receivedEvents[0]);
    }

    [Fact]
    public async Task PublishAsync_MultipleConcurrentPublishCalls_NoRaceConditions()
    {
        // Arrange
        var eventBus = CreateEventBus();
        var publisher = CreateEventPublisher(eventBus);
        var completedTasks = 0;
        var exceptions = new List<Exception>();

        // Subscribe a handler that tracks completion
        eventBus.Subscribe<TestEvent>(e =>
        {
            Interlocked.Increment(ref completedTasks);
            return Task.CompletedTask;
        });

        // Act - multiple concurrent publish calls
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await publisher.PublishAsync(new TestEvent());
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - all tasks completed successfully, no race conditions
        Assert.Empty(exceptions);
        Assert.Equal(10, completedTasks);
    }

    [Fact]
    public async Task PublishAsync_MixedEventTypes_CorrectRouting()
    {
        // Arrange
        var eventBus = CreateEventBus();
        var publisher = CreateEventPublisher(eventBus);
        var testEventsReceived = new List<TenantEvent>();
        var anotherEventsReceived = new List<TenantEvent>();

        // Subscribe to different event types
        eventBus.Subscribe<TestEvent>(e =>
        {
            testEventsReceived.Add(e);
            return Task.CompletedTask;
        });

        eventBus.Subscribe<AnotherTestEvent>(e =>
        {
            anotherEventsReceived.Add(e);
            return Task.CompletedTask;
        });

        // Act - publish different event types
        await publisher.PublishAsync(new TestEvent());
        await publisher.PublishAsync(new AnotherTestEvent());
        await publisher.PublishAsync(new TestEvent());

        // Assert - correct routing
        Assert.Equal(2, testEventsReceived.Count);
        Assert.Single(anotherEventsReceived);
        Assert.All(testEventsReceived, e => Assert.IsType<TestEvent>(e));
        Assert.All(anotherEventsReceived, e => Assert.IsType<AnotherTestEvent>(e));
    }

    [Fact]
    public async Task PublishBatchAsync_NullEvents_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreateEventPublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => publisher.PublishBatchAsync<TestEvent>(null!));
    }

    [Fact]
    public async Task PublishBatchAsync_EmptyCollection_NoError()
    {
        // Arrange
        var publisher = CreateEventPublisher();

        // Act & Assert - should complete without error
        await publisher.PublishBatchAsync(Array.Empty<TestEvent>());
    }

    [Fact]
    public async Task PublishBatchAsync_MixedEventTypes_CorrectRouting()
    {
        // Arrange
        var eventBus = CreateEventBus();
        var publisher = CreateEventPublisher(eventBus);
        var testEventsReceived = new List<TenantEvent>();
        var anotherEventsReceived = new List<TenantEvent>();

        // Subscribe to different event types
        eventBus.Subscribe<TestEvent>(e =>
        {
            testEventsReceived.Add(e);
            return Task.CompletedTask;
        });

        eventBus.Subscribe<AnotherTestEvent>(e =>
        {
            anotherEventsReceived.Add(e);
            return Task.CompletedTask;
        });

        // Act - publish batch with different event types
        var testEvents = new List<TestEvent> { new TestEvent(), new TestEvent() };
        var anotherEvents = new List<AnotherTestEvent> { new AnotherTestEvent() };

        await publisher.PublishBatchAsync(testEvents);
        await publisher.PublishBatchAsync(anotherEvents);

        // Assert - correct routing
        Assert.Equal(2, testEventsReceived.Count);
        Assert.Single(anotherEventsReceived);
    }

    [Fact]
    public async Task PublishAsync_BufferedEventPublisher_WrapsInnerPublisher()
    {
        // Arrange
        var eventBus = CreateEventBus();
        var innerPublisher = CreateEventPublisher(eventBus);
        var logger = NullLogger<BufferedEventPublisher>.Instance;
        var options = new BufferedEventPublisherOptions();
        var bufferedPublisher = new BufferedEventPublisher(innerPublisher, logger, options);

        var receivedEvents = new List<TenantEvent>();
        eventBus.Subscribe<TestEvent>(e =>
        {
            receivedEvents.Add(e);
            return Task.CompletedTask;
        });

        // Act
        await bufferedPublisher.PublishAsync(new TestEvent());

        // Wait a bit for background processing
        await Task.Delay(100);

        // Assert
        Assert.Single(receivedEvents);

        bufferedPublisher.Dispose();
    }
}

/// <summary>
/// Fake HttpContextAccessor for testing without ASP.NET Core context
/// </summary>
internal sealed class FakeHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; } = null;
}