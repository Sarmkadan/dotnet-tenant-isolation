# IEventPublisher

The `IEventPublisher` interface defines the contract for publishing events within the `dotnet-tenant-isolation` framework, facilitating decoupled communication between components while maintaining tenant context. It supports both single event dispatch and batch processing, provides mechanisms for dynamic handler registration, and exposes metadata regarding registered handlers and the publisher's identity to support observability and runtime introspection.

## API

### `EventPublisher`
Represents the concrete implementation type associated with this interface. This property or member indicates the specific class responsible for executing the publishing logic defined by the interface.

### `Task PublishAsync<TEvent>(TEvent event)`
Asynchronously publishes a single event instance to all registered handlers capable of processing the specified event type.
*   **Parameters**:
    *   `event`: The event instance to be published.
*   **Return Value**: A `Task` that completes when the event has been dispatched to all relevant handlers.
*   **Exceptions**: May throw exceptions if a registered handler fails during execution or if the event payload is invalid for the target handlers.

### `Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events)`
Asynchronously publishes a collection of events of the same type in a single operation. This method optimizes throughput by reducing overhead associated with multiple individual publish calls.
*   **Parameters**:
    *   `events`: An enumerable collection of event instances to be published.
*   **Return Value**: A `Task` that completes when all events in the batch have been dispatched.
*   **Exceptions**: May throw if the collection is null, empty (depending on implementation policy), or if any individual event within the batch causes a handler failure.

### `string EventType`
Gets the unique string identifier representing the primary event type or category associated with this publisher instance. This value is typically used for routing, logging, or filtering purposes within the tenant isolation boundary.

### `string HandlerName`
Gets the logical name assigned to the handler or the publisher context. This identifier helps distinguish between multiple publishers or handler groups operating within the same application domain.

### `DateTime RegisteredAt`
Gets the precise timestamp indicating when this publisher or its primary handler context was registered within the system. This is useful for auditing, lifecycle management, and debugging initialization order issues.

### `void RegisterHandler<TEvent>(Action<TEvent> handler)`
Dynamically registers a new event handler for a specific event type at runtime.
*   **Parameters**:
    *   `handler`: The delegate method to be invoked when an event of type `TEvent` is published.
*   **Return Value**: None.
*   **Exceptions**: May throw if the handler is null or if the registration violates thread-safety constraints during active enumeration.

### `IEnumerable<EventHandlerInfo> GetHandlers<TEvent>()`
Retrieves a collection of metadata describing all handlers currently registered for a specific event type.
*   **Parameters**: None (inferred via generic type `TEvent`).
*   **Return Value**: An enumerable collection of `EventHandlerInfo` objects containing details such as handler names, registration times, and types.
*   **Exceptions**: Generally does not throw unless internal state is corrupted.

### `IEnumerable<EventHandlerInfo> GetAllHandlers()`
Retrieves a comprehensive collection of metadata for every handler registered across all event types within this publisher instance.
*   **Parameters**: None.
*   **Return Value**: An enumerable collection of `EventHandlerInfo` objects.
*   **Exceptions**: Generally does not throw unless internal state is corrupted.

## Usage

### Publishing a Single Event with Dynamic Registration
This example demonstrates registering a specific handler for an order event and subsequently publishing an instance of that event.

```csharp
using System;
using System.Threading.Tasks;

public class OrderCreatedEvent 
{
    public string OrderId { get; set; }
    public string TenantId { get; set; }
}

public async Task ProcessOrderAsync(IEventPublisher publisher)
{
    // Register a handler specifically for OrderCreatedEvent
    publisher.RegisterHandler<OrderCreatedEvent>(evt => 
    {
        Console.WriteLine($"Processing order {evt.OrderId} for tenant {evt.TenantId}");
    });

    var newOrder = new OrderCreatedEvent 
    { 
        OrderId = "ORD-12345", 
        TenantId = "TENANT-A" 
    };

    // Publish the event asynchronously
    await publisher.PublishAsync(newOrder);
}
```

### Batch Publishing and Handler Introspection
This example illustrates publishing a batch of events and inspecting the registered handlers to verify configuration before execution.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public async Task BulkImportAsync(IEventPublisher publisher)
{
    // Inspect current handlers for InventoryUpdatedEvent
    var handlers = publisher.GetHandlers<InventoryUpdatedEvent>();
    if (!handlers.Any())
    {
        Console.WriteLine("Warning: No handlers registered for InventoryUpdatedEvent.");
        return;
    }

    var events = new List<InventoryUpdatedEvent>
    {
        new InventoryUpdatedEvent { ItemId = "ITEM-1", Change = 10 },
        new InventoryUpdatedEvent { ItemId = "ITEM-2", Change = -5 },
        new InventoryUpdatedEvent { ItemId = "ITEM-3", Change = 20 }
    };

    // Publish all events in a single batch operation
    await publisher.PublishBatchAsync(events);
    
    Console.WriteLine($"Batch published at {DateTime.UtcNow}. Publisher: {publisher.HandlerName}");
}

public class InventoryUpdatedEvent 
{
    public string ItemId { get; set; }
    public int Change { get; set; }
}
```

## Notes

*   **Thread Safety**: The `RegisterHandler` method modifies the internal collection of handlers. If `PublishAsync` or `GetHandlers` is executing concurrently on a different thread, care must be taken to ensure the underlying collection supports concurrent reads and writes. Implementations should ideally use concurrent collections or locking mechanisms to prevent `InvalidOperationException` during enumeration.
*   **Handler Execution Order**: The interface does not explicitly guarantee the order in which handlers are invoked during `PublishAsync` or `PublishBatchAsync`. Consumers should not rely on a specific sequence unless documented by the concrete `EventPublisher` implementation.
*   **Exception Propagation**: In `PublishBatchAsync`, the behavior regarding partial failures is critical. If one event in the batch causes a handler to throw, the implementation may either halt the entire batch or continue processing remaining events. Callers should wrap batch calls in try-catch blocks and assume that some events might not have been processed successfully if an exception occurs.
*   **Generic Constraints**: The generic methods `PublishAsync`, `PublishBatchAsync`, `RegisterHandler`, and `GetHandlers` rely on the runtime type of `TEvent`. Passing an event instance that does not match the generic type parameter at compile time will result in a compilation error, while mismatched handler registrations will result in the handler simply not being invoked for that specific generic closure.
*   **Metadata Accuracy**: The `RegisteredAt` and `HandlerName` properties reflect the state at the time of the publisher's initialization or the specific handler's registration. Dynamic re-registration of a handler with the same signature may or may not update the metadata depending on the implementation strategy (e.g., replacing vs. appending).
