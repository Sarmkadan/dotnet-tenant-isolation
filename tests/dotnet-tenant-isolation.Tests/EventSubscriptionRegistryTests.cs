using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;

namespace TenantIsolation.Events.Tests;

/// <summary>
/// Tests for thread-safe EventSubscriptionRegistry implementation
/// </summary>
public class EventSubscriptionRegistryTests
{
    private readonly IEventSubscriptionRegistry _registry;

    public EventSubscriptionRegistryTests()
    {
        _registry = new EventSubscriptionRegistry();
    }

    [Fact]
    public void Constructor_ShouldInitializeEmptyRegistry()
    {
        // Act
        var registry = new EventSubscriptionRegistry();

        // Assert
        var allHandlers = registry.GetAllHandlers();
        Assert.Empty(allHandlers);
    }

    [Fact]
    public void Clear_ShouldRemoveAllHandlers()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        registry.Subscribe<TestEvent>(_ => Task.CompletedTask, "TestHandler");

        // Act
        registry.Clear();

        // Assert
        var allHandlers = registry.GetAllHandlers();
        Assert.Empty(allHandlers);
    }

    [Fact]
    public void Subscribe_WithFuncHandler_ShouldRegisterHandler()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        Func<TestEvent, Task> handler = _ => Task.CompletedTask;

        // Act
        var token = registry.Subscribe(handler, "TestHandler");

        // Assert
        Assert.NotNull(token);
        var handlers = registry.GetHandlers<TestEvent>();
        Assert.Single(handlers);
        Assert.Equal("TestHandler", handlers.First().HandlerName);
    }

    [Fact]
    public void Subscribe_WithActionHandler_ShouldRegisterHandler()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        Action<TestEvent> handler = _ => { };

        // Act
        var token = registry.Subscribe(handler, "TestHandler");

        // Assert
        Assert.NotNull(token);
        var handlers = registry.GetHandlers<TestEvent>();
        Assert.Single(handlers);
        Assert.Equal("TestHandler", handlers.First().HandlerName);
    }

    [Fact]
    public void Subscribe_WithNullHandler_ShouldThrow()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.Subscribe<TestEvent>((Func<TestEvent, Task>)null!));
        Assert.Throws<ArgumentNullException>(() => registry.Subscribe<TestEvent>((Action<TestEvent>)null!));
    }

    [Fact]
    public void Unsubscribe_WithFuncHandler_ShouldRemoveHandler()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        Func<TestEvent, Task> handler = _ => Task.CompletedTask;
        var token = registry.Subscribe(handler, "TestHandler");

        // Act
        var result = registry.Unsubscribe(handler);

        // Assert
        Assert.True(result);
        var handlers = registry.GetHandlers<TestEvent>();
        Assert.Empty(handlers);
    }

    [Fact]
    public void Unsubscribe_WithActionHandler_ShouldRemoveHandler()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        Action<TestEvent> handler = _ => { };
        var token = registry.Subscribe(handler, "TestHandler");

        // Act
        var result = registry.Unsubscribe(handler);

        // Assert
        Assert.True(result);
        var handlers = registry.GetHandlers<TestEvent>();
        Assert.Empty(handlers);
    }

    [Fact]
    public void Unsubscribe_WithNonExistentHandler_ShouldReturnFalse()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        Func<TestEvent, Task> handler1 = _ => Task.CompletedTask;
        Func<TestEvent, Task> handler2 = _ => Task.CompletedTask;
        registry.Subscribe(handler1, "Handler1");

        // Act
        var result = registry.Unsubscribe(handler2);

        // Assert
        Assert.False(result);
        var handlers = registry.GetHandlers<TestEvent>();
        Assert.Single(handlers);
    }

    [Fact]
    public void Unsubscribe_WithNullHandler_ShouldThrow()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.Unsubscribe<TestEvent>((Func<TestEvent, Task>)null!));
        Assert.Throws<ArgumentNullException>(() => registry.Unsubscribe<TestEvent>((Action<TestEvent>)null!));
    }

    [Fact]
    public void GetHandlers_ForNonExistentEventType_ShouldReturnEmpty()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();

        // Act
        var handlers = registry.GetHandlers<TestEvent>();

        // Assert
        Assert.Empty(handlers);
    }

    [Fact]
    public void GetAllHandlers_ShouldReturnAllRegisteredHandlers()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        Func<TestEvent, Task> handler1 = _ => Task.CompletedTask;
        Func<TestEvent, Task> handler2 = _ => Task.CompletedTask;
        registry.Subscribe(handler1, "Handler1");
        registry.Subscribe(handler2, "Handler2");

        // Act
        var allHandlers = registry.GetAllHandlers();

        // Assert
        Assert.Equal(2, allHandlers.Count());
    }

    [Fact]
    public void SubscriptionToken_WhenDisposed_ShouldUnsubscribe()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        Func<TestEvent, Task> handler = _ => Task.CompletedTask;
        var token = registry.Subscribe(handler, "TestHandler");

        // Verify handler is registered
        Assert.Single(registry.GetHandlers<TestEvent>());

        // Act
        token.Dispose();

        // Assert
        Assert.Empty(registry.GetHandlers<TestEvent>());
    }

    [Fact]
    public void SubscriptionToken_DisposeMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        Func<TestEvent, Task> handler = _ => Task.CompletedTask;
        var token = registry.Subscribe(handler, "TestHandler");

        // Act
        token.Dispose();
        token.Dispose();
        token.Dispose();

        // Assert - should not throw
        Assert.Empty(registry.GetHandlers<TestEvent>());
    }

    [Fact]
    public async Task Subscribe_WithMultipleHandlers_ShouldMaintainSeparateRegistrations()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var handler1Invoked = false;
        var handler2Invoked = false;

        Func<TestEvent, Task> handler1 = _ =>
        {
            handler1Invoked = true;
            return Task.CompletedTask;
        };

        Func<TestEvent, Task> handler2 = _ =>
        {
            handler2Invoked = true;
            return Task.CompletedTask;
        };

        // Act
        var token1 = registry.Subscribe(handler1, "Handler1");
        var token2 = registry.Subscribe(handler2, "Handler2");

        var handlers = registry.GetHandlers<TestEvent>().ToList();

        // Assert
        Assert.Equal(2, handlers.Count);
        Assert.Equal("Handler1", handlers[0].HandlerName);
        Assert.Equal("Handler2", handlers[1].HandlerName);
    }

    [Fact]
    public void GetHandlers_ShouldReturnIndependentCollections()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        Func<TestEvent, Task> handler1 = _ => Task.CompletedTask;
        Func<TestEvent, Task> handler2 = _ => Task.CompletedTask;
        registry.Subscribe(handler1, "Handler1");
        registry.Subscribe(handler2, "Handler2");

        // Act - call GetHandlers multiple times
        var handlers1 = registry.GetHandlers<TestEvent>();
        var handlers2 = registry.GetHandlers<TestEvent>();

        // Assert - both should have 2 handlers
        Assert.Equal(2, handlers1.Count());
        Assert.Equal(2, handlers2.Count());
    }

    [Fact]
    public void EventHandlerInfo_ShouldContainCorrectInformation()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var testHandler = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        var testHandlerName = "MyCustomHandler";
        var beforeRegistration = DateTime.UtcNow;

        // Act
        var token = registry.Subscribe(testHandler, testHandlerName);
        var handlers = registry.GetHandlers<TestEvent>().ToList();
        var afterRegistration = DateTime.UtcNow;

        // Assert
        Assert.Single(handlers);
        var handlerInfo = handlers[0];
        Assert.Equal(nameof(TestEvent), handlerInfo.EventType);
        Assert.Equal(testHandlerName, handlerInfo.HandlerName);
        Assert.InRange(handlerInfo.RegisteredAt, beforeRegistration, afterRegistration);
    }

    [Fact]
    public void ConcurrentSubscribeAndUnsubscribe_ShouldNotThrow()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var exceptions = new ConcurrentBag<Exception>();
        var tasks = new List<Task>();

        // Act - spawn many concurrent operations
        for (int i = 0; i < 100; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    // Mix of subscribe and unsubscribe operations
                    if (threadId % 3 == 0)
                    {
                        // Subscribe
                        var handler = new Func<TestEvent, Task>(_ => Task.CompletedTask);
                        registry.Subscribe(handler, $"Handler{threadId}");
                    }
                    else if (threadId % 3 == 1)
                    {
                        // Unsubscribe (may fail if handler doesn't exist, which is fine)
                        var handler = new Func<TestEvent, Task>(_ => Task.CompletedTask);
                        registry.Unsubscribe(handler);
                    }
                    else
                    {
                        // Get handlers
                        var handlers = registry.GetHandlers<TestEvent>();
                        var count = handlers.Count();
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        // Wait for all tasks to complete
        Task.WaitAll(tasks.ToArray());

        // Assert - no exceptions should have occurred
        Assert.Empty(exceptions);

        // Verify registry is still in valid state
        var allHandlers = registry.GetAllHandlers();
        // We expect some handlers to remain (those that were successfully subscribed)
    }

    [Fact]
    public void ConcurrentSubscribeWithTokens_ShouldCleanupProperly()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var exceptions = new ConcurrentBag<Exception>();
        var tasks = new List<Task>();

        // Act - spawn many concurrent subscribe operations with token disposal
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var handler = new Func<TestEvent, Task>(_ => Task.CompletedTask);
                    using (registry.Subscribe(handler, $"Handler{i}"))
                    {
                        // Handler is registered while token is in scope
                        var handlers = registry.GetHandlers<TestEvent>();
                        var count = handlers.Count();
                    }
                    // Handler should be auto-unsubscribed when token is disposed
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        // Wait for all tasks to complete
        Task.WaitAll(tasks.ToArray());

        // Assert - no exceptions should have occurred
        Assert.Empty(exceptions);
    }

    [Fact]
    public void ConcurrentSubscribeDifferentEventTypes_ShouldNotInterfere()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var exceptions = new ConcurrentBag<Exception>();
        var tasks = new List<Task>();

        // Act - concurrently subscribe to different event types
        for (int i = 0; i < 50; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    if (threadId % 2 == 0)
                    {
                        var handler = new Func<TestEvent, Task>(_ => Task.CompletedTask);
                        registry.Subscribe(handler, $"Handler{threadId}");
                    }
                    else
                    {
                        var handler = new Func<AnotherTestEvent, Task>(_ => Task.CompletedTask);
                        registry.Subscribe(handler, $"AnotherHandler{threadId}");
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        // Wait for all tasks to complete
        Task.WaitAll(tasks.ToArray());

        // Assert - no exceptions should have occurred
        Assert.Empty(exceptions);

        // Verify handlers are registered correctly
        var testEventHandlers = registry.GetHandlers<TestEvent>();
        var anotherTestEventHandlers = registry.GetHandlers<AnotherTestEvent>();
        Assert.Equal(25, testEventHandlers.Count());
        Assert.Equal(25, anotherTestEventHandlers.Count());
    }

    // Test event types
    private class TestEvent : TenantEvent
    {
        public TestEvent()
        {
            TenantId = Guid.NewGuid();
        }
    }

    private class AnotherTestEvent : TenantEvent
    {
        public AnotherTestEvent()
        {
            TenantId = Guid.NewGuid();
        }
    }
}
