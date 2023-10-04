# IEventBus

The `IEventBus` interface defines a lightweight, in-memory pub/sub mechanism designed for decoupling components within a tenant-isolated .NET application. It facilitates asynchronous event propagation where publishers emit events without knowledge of specific subscribers, allowing multiple handlers to react to domain events such as tenant provisioning, configuration changes, or lifecycle transitions. The implementation prioritizes simplicity and type safety through generic type parameters, ensuring that event payloads are strongly typed while maintaining loose coupling between the event source and its consumers.

## API

### `EventHandlerDelegate<in TEvent>`
A delegate definition representing the signature required for event handlers.
*   **Purpose**: Defines the contract for methods that handle events of type `TEvent`.
*   **Parameters**: Takes a single parameter of type `TEvent` representing the event payload.
*   **Return Value**: Returns a `Task`, indicating that the handler operation is asynchronous.
*   **Exceptions**: Throws exceptions propagated from the underlying handler implementation if the asynchronous operation fails.

### `EventBus`
The concrete implementation of the event bus logic.
*   **Purpose**: Provides the runtime infrastructure for managing subscriptions and dispatching events.
*   **Parameters**: N/A (Instantiated via default constructor or dependency injection).
*   **Return Value**: N/A.
*   **Exceptions**: May throw `ArgumentNullException` during construction if required dependencies (if any are injected in specific overloads) are null, though the default instance is typically stateless regarding external dependencies.

### `void Subscribe<TEvent>(EventHandlerDelegate<TEvent> handler)`
Registers a new handler for a specific event type.
*   **Purpose**: Adds a delegate to the internal list of subscribers for the specified `TEvent`.
*   **Parameters**:
    *   `handler`: The `EventHandlerDelegate<TEvent>` to invoke when the event is published.
*   **Return Value**: None.
*   **Exceptions**: Throws `ArgumentNullException` if `handler` is null. May throw if the internal subscription store reaches capacity limits (implementation dependent).

### `void Unsubscribe<TEvent>(EventHandlerDelegate<TEvent> handler)`
Removes a previously registered handler for a specific event type.
*   **Purpose**: Detaches a delegate from the subscription list for `TEvent`, preventing it from receiving future events.
*   **Parameters**:
    *   `handler`: The `EventHandlerDelegate<TEvent>` instance to remove.
*   **Return Value**: None.
*   **Exceptions**: Throws `ArgumentNullException` if `handler` is null. Does not throw if the handler was not previously subscribed (operation is idempotent).

### `async Task PublishAsync<TEvent>(TEvent event)`
Dispatches an event to all currently registered subscribers.
*   **Purpose**: Iterates through all subscribers registered for `TEvent` and invokes them asynchronously.
*   **Parameters**:
    *   `event`: The instance of `TEvent` to be delivered to handlers.
*   **Return Value**: A `Task` that completes when all subscriber handlers have finished execution.
*   **Exceptions**: Throws `ArgumentNullException` if `event` is null. Propagates exceptions thrown by individual handlers; if multiple handlers fail, the behavior depends on the specific aggregation strategy (typically the first encountered exception is thrown, or an `AggregateException` is raised).

### `int GetSubscriberCount<TEvent>()`
Retrieves the number of active subscribers for a specific event type.
*   **Purpose**: Provides visibility into the current subscription state for monitoring or debugging.
*   **Parameters**: None (infers `TEvent` from generic context).
*   **Return Value**: An `int` representing the count of handlers currently subscribed to `TEvent`.
*   **Exceptions**: None.

### `void ClearSubscriptions()`
Removes all registered handlers for all event types.
*   **Purpose**: Resets the event bus to a clean state, useful for testing scenarios or application shutdown.
*   **Parameters**: None.
*   **Return Value**: None.
*   **Exceptions**: None.

### `static IServiceCollection AddEventBus(this IServiceCollection services)`
Extension method for registering the event bus with the .NET dependency injection container.
*   **Purpose**: Configures the `IEventBus` service lifetime (typically Singleton) within the `IServiceCollection`.
*   **Parameters**:
    *   `services`: The `IServiceCollection` to add the service to.
*   **Return Value**: Returns the modified `IServiceCollection` to allow method chaining.
*   **Exceptions**: Throws `ArgumentNullException` if `services` is null.

## Usage

### Example 1: Basic Subscription and Publishing
This example demonstrates defining an event, subscribing a handler, and publishing the event within a scoped service.

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

// Define a domain event
public record TenantCreatedEvent(string TenantId, string Name);

public class Program
{
    public static async Task Main()
    {
        var services = new ServiceCollection();
        
        // Register the EventBus
        services.AddEventBus();
        
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<EventBus>();

        // Subscribe to the event
        eventBus.Subscribe<TenantCreatedEvent>(async (evt) =>
        {
            Console.WriteLine($"Processing tenant creation: {evt.Name} ({evt.TenantId})");
            await Task.Delay(100); // Simulate async work
            Console.WriteLine($"Completed processing for {evt.TenantId}");
        });

        // Publish the event
        var eventData = new TenantCreatedEvent("t-123", "Acme Corp");
        await eventBus.PublishAsync(eventData);
        
        // Verify count
        Console.WriteLine($"Active subscribers: {eventBus.GetSubscriberCount<TenantCreatedEvent>()}");
    }
}
```

### Example 2: Dynamic Unsubscription and Cleanup
This example illustrates managing the lifecycle of a subscription, including unsubscribing a specific handler and clearing all subscriptions.

```csharp
using System;
using System.Threading.Tasks;

public class EventLifecycleManager
{
    private readonly EventBus _eventBus;
    private readonly EventHandlerDelegate<TenantUpdatedEvent> _handler;

    public EventLifecycleManager(EventBus eventBus)
    {
        _eventBus = eventBus;
        
        // Define handler as a field to allow reference for unsubscription
        _handler = async (evt) => 
        {
            await Task.CompletedTask;
            Console.WriteLine($"Tenant {evt.TenantId} updated.");
        };
        
        _eventBus.Subscribe<TenantUpdatedEvent>(_handler);
    }

    public async Task ProcessUpdate(string tenantId)
    {
        // Publish while subscribed
        await _eventBus.PublishAsync(new TenantUpdatedEvent(tenantId));
        
        // Unsubscribe specific handler
        _eventBus.Unsubscribe<TenantUpdatedEvent>(_handler);
        
        // Verify no subscribers remain
        if (_eventBus.GetSubscriberCount<TenantUpdatedEvent>() == 0)
        {
            Console.WriteLine("Handler successfully removed.");
        }
    }

    public void Reset()
    {
        // Clear all subscriptions globally
        _eventBus.ClearSubscriptions();
    }
}

public record TenantUpdatedEvent(string TenantId);
```

## Notes

*   **Thread Safety**: The `Subscribe`, `Unsubscribe`, and `PublishAsync` methods are designed to be thread-safe. Internal collections managing subscribers utilize concurrent patterns to allow subscriptions to be modified while events are being dispatched. However, modifying the collection during iteration (e.g., subscribing inside a handler) may result in the new subscription receiving the current event depending on the specific enumeration strategy of the underlying implementation.
*   **Exception Propagation**: `PublishAsync` awaits all handlers. If a handler throws an exception, the task returned by `PublishAsync` will fault. Callers must wrap publish calls in `try-catch` blocks to prevent unobserved task exceptions from crashing the host process.
*   **Handler Equality**: `Unsubscribe` relies on delegate equality. If an anonymous delegate or lambda is passed directly to `Subscribe` without being stored in a variable, it cannot be unsubscribed later unless the exact same delegate instance is provided. Always store handlers in variables if dynamic unsubscription is required.
*   **Event Ordering**: There is no guaranteed order of execution for multiple subscribers listening to the same event type. Handlers should be designed to be independent of one another.
*   **Memory Leaks**: Since the `EventBus` is typically registered as a Singleton, long-lived subscriptions to short-lived scoped services can cause memory leaks. Ensure that handlers do not capture scoped dependencies directly unless the handler itself is managed within the appropriate scope or uses weak references.
