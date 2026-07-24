using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TenantIsolation.Events;
using Xunit;

namespace TenantIsolation.Tests;

/// <summary>
/// Tests for retry-with-backoff and dead-letter handling in <see cref="EventBus"/>
/// </summary>
public class EventBusResilienceTests
{
    private sealed class RecordingDeadLetterSink : IDeadLetterSink
    {
        public List<(string HandlerName, Exception Exception)> Received { get; } = new();

        public Task HandleAsync<TEvent>(TEvent @event, string handlerName, Exception exception) where TEvent : TenantEvent
        {
            Received.Add((handlerName, exception));
            return Task.CompletedTask;
        }
    }

    private sealed class TestEvent : TenantEvent
    {
        public TestEvent() => TenantId = Guid.NewGuid();
    }

    private static EventBus CreateBus(RecordingDeadLetterSink sink, int maxRetries = 3) =>
        new(
            NullLogger<EventBus>.Instance,
            Options.Create(new PublisherResilienceOptions
            {
                MaxRetries = maxRetries,
                BaseDelay = TimeSpan.FromMilliseconds(1)
            }),
            sink);

    [Fact]
    public async Task PublishAsync_HandlerThrowsOnceThenSucceeds_EventuallyInvokesHandlerSuccessfully()
    {
        // Arrange
        var sink = new RecordingDeadLetterSink();
        var bus = CreateBus(sink);
        var attempts = 0;
        var succeeded = false;

        bus.Subscribe<TestEvent>(_ =>
        {
            attempts++;
            if (attempts == 1)
                throw new InvalidOperationException("transient failure");

            succeeded = true;
            return Task.CompletedTask;
        });

        // Act
        await bus.PublishAsync(new TestEvent());

        // Assert
        Assert.Equal(2, attempts);
        Assert.True(succeeded);
        Assert.Empty(sink.Received);
    }

    [Fact]
    public async Task PublishAsync_HandlerAlwaysThrows_IsDeadLetteredAfterExhaustingRetries()
    {
        // Arrange
        var sink = new RecordingDeadLetterSink();
        var bus = CreateBus(sink, maxRetries: 2);
        var attempts = 0;

        bus.Subscribe<TestEvent>(_ =>
        {
            attempts++;
            throw new InvalidOperationException("permanent failure");
        });

        // Act
        await bus.PublishAsync(new TestEvent());

        // Assert
        Assert.Equal(3, attempts); // initial attempt + 2 retries
        Assert.Single(sink.Received);
        Assert.IsType<InvalidOperationException>(sink.Received[0].Exception);
    }

    [Fact]
    public async Task PublishAsync_OneHandlerAlwaysFails_DoesNotBlockDeliveryToOtherHandlers()
    {
        // Arrange
        var sink = new RecordingDeadLetterSink();
        var bus = CreateBus(sink, maxRetries: 1);
        var secondHandlerInvoked = false;

        bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("always fails"));
        bus.Subscribe<TestEvent>(_ =>
        {
            secondHandlerInvoked = true;
            return Task.CompletedTask;
        });

        // Act
        await bus.PublishAsync(new TestEvent());

        // Assert
        Assert.True(secondHandlerInvoked);
        Assert.Single(sink.Received);
    }
}
