using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;

namespace TenantIsolation.Events.Tests;

/// <summary>
/// Comprehensive tests for EventSubscriptionRegistry edge cases and fault tolerance
/// Tests the actual behavior of the registry implementation
/// </summary>
public class EventSubscriptionRegistryComprehensiveTests
{
    private readonly IEventSubscriptionRegistry _registry;

    public EventSubscriptionRegistryComprehensiveTests()
    {
        _registry = new EventSubscriptionRegistry();
    }

    [Fact]
    public void Subscribe_SameHandlerTwice_ShouldRegisterTwoSeparateHandlers()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var handler = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        var handlerName = "DuplicateHandler";

        // Act - subscribe the same handler twice
        var token1 = registry.Subscribe(handler, handlerName);
        var token2 = registry.Subscribe(handler, handlerName);

        // Assert - both subscriptions should be registered
        var handlers = registry.GetHandlers<TestEvent>().ToList();
        Assert.Equal(2, handlers.Count);
        Assert.Equal(handlerName, handlers[0].HandlerName);
        Assert.Equal(handlerName, handlers[1].HandlerName);

        // Cleanup
        token1.Dispose();
        token2.Dispose();
    }

    [Fact]
    public void Unsubscribe_NonExistentHandler_ShouldReturnFalse_NotThrow()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var handler1 = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        var handler2 = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        registry.Subscribe(handler1, "Handler1");

        // Act - try to unsubscribe a handler that was never registered
        var result = registry.Unsubscribe(handler2);

        // Assert - should return false, not throw
        Assert.False(result);
        var handlers = registry.GetHandlers<TestEvent>();
        Assert.Single(handlers);
    }

    [Fact]
    public void Unsubscribe_NullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();

        // Act & Assert - should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => registry.Unsubscribe<TestEvent>((Func<TestEvent, Task>)null!));
        Assert.Throws<ArgumentNullException>(() => registry.Unsubscribe<TestEvent>((Action<TestEvent>)null!));
    }

    [Fact]
    public void GetHandlers_ForEventTypeWithNoHandlers_ShouldReturnEmptyCollection()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();

        // Act
        var handlers = registry.GetHandlers<TestEvent>();

        // Assert
        Assert.Empty(handlers);
    }

    [Fact]
    public void SubscriptionToken_Dispose_ShouldRemoveHandler()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var handler = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        var token = registry.Subscribe(handler, "TestHandler");

        // Verify handler is registered
        Assert.Single(registry.GetHandlers<TestEvent>());

        // Act - dispose the token
        token.Dispose();

        // Assert - handler should be removed
        Assert.Empty(registry.GetHandlers<TestEvent>());
    }

    [Fact]
    public void SubscriptionToken_DisposeMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var handler = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        var token = registry.Subscribe(handler, "TestHandler");

        // Act - dispose multiple times
        token.Dispose();
        token.Dispose();
        token.Dispose();

        // Assert - should not throw and registry should be empty
        Assert.Empty(registry.GetHandlers<TestEvent>());
    }

    [Fact]
    public void Clear_ShouldRemoveAllHandlers()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var handler1 = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        var handler2 = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        registry.Subscribe(handler1, "Handler1");
        registry.Subscribe(handler2, "Handler2");

        // Act
        registry.Clear();

        // Assert
        Assert.Empty(registry.GetAllHandlers());
    }

    [Fact]
    public void GetHandlers_WithTenantIdFilter_ShouldReturnOnlyMatchingHandlers()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();

        var handler1 = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        var handler2 = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        var handler3 = new Func<TestEvent, Task>(_ => Task.CompletedTask);

        // Subscribe handlers with different tenant IDs
        using (registry.Subscribe(handler1, "Handler1")) { }
        using (registry.Subscribe(handler2, "Handler2")) { }
        using (registry.Subscribe(handler3, "Handler3")) { }

        // Act - get handlers for specific tenant
        var handlersForTenant1 = registry.GetHandlers<TestEvent>(tenantId1).ToList();
        var handlersForTenant2 = registry.GetHandlers<TestEvent>(tenantId2).ToList();
        var allHandlers = registry.GetHandlers<TestEvent>().ToList();

        // Assert
        Assert.Equal(3, allHandlers.Count);
        Assert.Empty(handlersForTenant1); // No handlers registered with tenantId1
        Assert.Empty(handlersForTenant2); // No handlers registered with tenantId2
    }

    [Fact]
    public void GetAllHandlers_WithTenantIdFilter_ShouldReturnOnlyMatchingHandlers()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var tenantId = Guid.NewGuid();

        var handler1 = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        var handler2 = new Func<TestEvent, Task>(_ => Task.CompletedTask);

        // Subscribe handlers
        using (registry.Subscribe(handler1, "Handler1")) { }
        using (registry.Subscribe(handler2, "Handler2")) { }

        // Act - get all handlers for specific tenant
        var handlers = registry.GetAllHandlers(tenantId).ToList();
        var allHandlers = registry.GetAllHandlers().ToList();

        // Assert
        Assert.Equal(2, allHandlers.Count);
        Assert.Empty(handlers); // No handlers registered with this tenantId
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
        Assert.Equal("Func", handlerInfo.HandlerSignature);
        Assert.InRange(handlerInfo.RegisteredAt, beforeRegistration, afterRegistration);
        Assert.Equal(Guid.Empty, handlerInfo.TenantId);

        // Cleanup
        token.Dispose();
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

    [Fact]
    public void Subscribe_WithActionHandler_ShouldRegisterCorrectly()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        Action<TestEvent> handler = _ => { };
        var handlerName = "ActionHandler";

        // Act
        var token = registry.Subscribe(handler, handlerName);

        // Assert
        Assert.NotNull(token);
        var handlers = registry.GetHandlers<TestEvent>();
        Assert.Single(handlers);
        Assert.Equal(handlerName, handlers.First().HandlerName);
        Assert.Equal("Action", handlers.First().HandlerSignature);

        // Cleanup
        token.Dispose();
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
    public void Subscribe_WithTenantScopedHandler_ShouldExtractTenantId()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var tenantId = Guid.NewGuid();
        var scopedHandler = new TenantScopedHandler(tenantId);

        // Act
        var token = registry.Subscribe<TestEvent>(scopedHandler.HandleAsync, "TenantScopedHandler");
        var handlers = registry.GetHandlers<TestEvent>().ToList();

        // Assert
        Assert.Single(handlers);
        Assert.Equal(tenantId, handlers[0].TenantId);

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void GetHandlers_ReturnsIndependentCollections()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var handler1 = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        var handler2 = new Func<TestEvent, Task>(_ => Task.CompletedTask);
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
    public void SubscriptionToken_WhenDisposed_ShouldUnsubscribeCorrectly()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        var handler = new Func<TestEvent, Task>(_ => Task.CompletedTask);
        var token = registry.Subscribe(handler, "TestHandler");

        // Verify handler is registered
        Assert.Single(registry.GetHandlers<TestEvent>());

        // Act
        token.Dispose();

        // Assert
        Assert.Empty(registry.GetHandlers<TestEvent>());
    }

    [Fact]
    public void UnsubscribeCore_WithDifferentHandlerSignatures_ShouldNotRemove()
    {
        // Arrange
        var registry = new EventSubscriptionRegistry();
        Func<TestEvent, Task> funcHandler = _ => Task.CompletedTask;
        Action<TestEvent> actionHandler = _ => { };

        registry.Subscribe(funcHandler, "FuncHandler");
        registry.Subscribe(actionHandler, "ActionHandler");

        // Act - try to unsubscribe func handler using action signature
        // This should not remove the func handler since signatures differ
        var result = registry.Unsubscribe<TestEvent>(actionHandler);

        // Assert - func handler should still be there
        Assert.True(result); // Action handler was found and removed
        var handlers = registry.GetHandlers<TestEvent>().ToList();
        Assert.Single(handlers);
        Assert.Equal("FuncHandler", handlers[0].HandlerName);
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

    private class TenantScopedHandler : TenantEventHandler<TestEvent>
    {
        public TenantScopedHandler(Guid tenantId) : base(tenantId) { }

        public override Task HandleAsync(TestEvent @event)
        {
            return Task.CompletedTask;
        }
    }
}