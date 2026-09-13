# EventBus

`EventBus` is the library's in-memory publish/subscribe mechanism for `TenantEvent` objects. It lets publishers announce tenant lifecycle and operational events without knowing which components handle them. It is process-local: it does not persist events, communicate across application instances, or provide an external message broker.

## Core Components

### EventBus
The central pub-sub implementation that manages event subscriptions and delivery with built-in resilience patterns.

### IEventBus
Interface defining the contract for subscribing, unsubscribing, publishing events, and getting subscriber counts.

### EventPublisher
Scoped service that enriches events with HTTP context (CorrelationId, UserId) before publishing to the event bus.

### IEventPublisher
Interface for the event publisher service.

### TenantEvent
Base class for all domain events in the tenant isolation system, providing common properties like EventId, TenantId, Timestamp, etc.

### PublisherResilienceOptions
Configuration options controlling per-handler retry and backoff behavior.

### IDeadLetterSink
Interface for handling events whose delivery exhausted all retry attempts.

### LoggingDeadLetterSink
Default dead-letter sink implementation that logs failures.

### EventSubscriptionRegistry
Advanced thread-safe subscription management service with automatic disposal tokens.

### SubscriptionToken
IDisposable token for automatic event subscription cleanup.

### BufferedEventPublisher
Decorator over `IEventPublisher` that buffers high-frequency events through a bounded channel and drains them in background batches.

## Registration and lifetime

Register the default event infrastructure with dependency injection:

```csharp
services.AddEventBus();
```

This registers:

- `IEventBus` as a singleton `EventBus`, so its subscription collection is shared for the lifetime of the application process.
- `IEventPublisher` as a scoped `EventPublisher`.
- `IDeadLetterSink` as a singleton `LoggingDeadLetterSink`.

`PublisherResilienceOptions` controls retry behavior. Its defaults are three retries after the initial attempt and a 200 ms base delay. Configure it through the normal options mechanism when different values are required:

```csharp
services.Configure<PublisherResilienceOptions>(options =>
{
    options.MaxRetries = 2;
    options.BaseDelay = TimeSpan.FromMilliseconds(100);
});
services.AddEventBus();
```

## Publishing events

Application code normally publishes through `IEventPublisher`. The default `EventPublisher` enriches an event from the current HTTP context when available: it copies `CorrelationId` and `UserId` from `HttpContext.Items`, then forwards the event to `IEventBus`.

```csharp
public sealed class TenantLifecycleService
{
    private readonly IEventPublisher _publisher;

    public TenantLifecycleService(IEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task AnnounceActivationAsync(Guid tenantId)
    {
        return _publisher.PublishAsync(new TenantActivatedEvent(tenantId));
    }
}
```

Callers can also inject `IEventBus` and call `PublishAsync` directly when HTTP-context enrichment is not wanted. Publishing `null` throws `ArgumentNullException`. Publishing an event with no matching subscribers completes successfully.

Routing is based on the generic `TEvent` supplied to `PublishAsync`, not a separate topic name. In normal use, allow type inference to preserve the concrete event type:

```csharp
await eventBus.PublishAsync(new TenantActivatedEvent(tenantId));
```

Avoid widening an event to `TenantEvent` before publishing, because that changes the generic routing type.

### Advanced Publishing Options

#### Buffered Event Publisher
For high-throughput scenarios, `BufferedEventPublisher` decorates `IEventPublisher` to buffer high-frequency events in a bounded channel and drain them in background batches. Register it in place of the default publisher:

```csharp
services.AddBufferedEventPublisher(options =>
{
    options.ChannelCapacity = 50_000;
    options.OverflowMode = BoundedChannelFullMode.DropOldest;
});
```

By default only events implementing `IHighFrequencyEvent` (such as `TenantResourceAccessedEvent`) are buffered; critical lifecycle events are published directly. Set `EnableAllEventsBuffering` to buffer every event. When the channel is full, `DropOldest` discards the oldest buffered event (tracked via `GetDroppedEventsCount<TEvent>()`), while `Wait` blocks the caller until space is available.

#### Batch Publishing
Publish multiple events efficiently:

```csharp
await eventPublisher.PublishBatchAsync(events);
```

## Subscribing to events

Handlers can subscribe through `IEventBus.Subscribe<TEvent>` or via the `IEventSubscriptionRegistry` for more advanced scenarios:

```csharp
public class TenantNotificationHandler
{
    private readonly IEventBus _eventBus;

    public TenantNotificationHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
        
        // Subscribe to tenant activation events
        _eventBus.Subscribe<TenantActivatedEvent>(HandleTenantActivatedAsync);
        
        // Subscribe to tenant deactivation events
        _eventBus.Subscribe<TenantDeactivatedEvent>(HandleTenantDeactivatedAsync);
    }

    private Task HandleTenantActivatedAsync(TenantActivatedEvent @event)
    {
        // Send welcome email, provision resources, etc.
        return Task.CompletedTask;
    }

    private Task HandleTenantDeactivatedAsync(TenantDeactivatedEvent @event)
    {
        // Send notification, schedule cleanup, etc.
        return Task.CompletedTask;
    }
}
```

### Subscription Registry (Advanced)
For automatic disposal and advanced subscription management:

```csharp
using var subscription = eventSubscriptionRegistry.Subscribe<TenantActivatedEvent>(
    HandleTenantActivatedAsync,
    "TenantActivationHandler"
// Subscription automatically disposes when token is disposed
);

// Or with tenant-scoped handlers:
using var tenantSubscription = eventSubscriptionRegistry.Subscribe<TenantActivatedEvent>(
    new TenantSpecificHandler(tenantId),
    "TenantSpecificActivationHandler"
);
```

### Handler Types Supported
- `Func<TEvent, Task>` - Async handlers
- `Action<TEvent>` - Synchronous handlers (wrapped automatically)
- Tenant-scoped handlers inheriting from `TenantEventHandler<TEvent>`

## Event Delivery Mechanics

### Covariant Dispatch
The event bus supports covariant dispatch - events are delivered to handlers registered for base types. For example, if you have a handler subscribed to `TenantEvent`, it will receive all specific tenant events like `TenantCreatedEvent`, `TenantActivatedEvent`, etc.

### Handler Resilience
Each handler invocation is wrapped with retry logic:
- Configurable number of retry attempts (default: 3)
- Exponential backoff between retries (default: 200ms base delay)
- Failed handlers after exhausting retries are forwarded to the dead-letter sink
- Handler failures are isolated - one handler's failure doesn't prevent delivery to other handlers

### Thread Safety
All subscription and publishing operations are thread-safe:
- Subscription modifications use locking to prevent race conditions
- Handler collections are copied during publication to allow concurrent modifications
- Dead-letter processing is isolated per handler

## Dead Letter Handling

When a handler fails after exhausting all retry attempts, the event is forwarded to an `IDeadLetterSink` implementation:

### LoggingDeadLetterSink (Default)
Logs failed events at Error level with full exception details and event context.

### Custom Implementations
Applications can provide custom dead-letter sinks for:
- Persistent storage of failed events
- Alerting/notification systems
- Dead-letter queues for later replay
- Analytics on failure patterns

Register a custom dead-letter sink:

```csharp
services.AddSingleton<IDeadLetterSink, CustomDeadLetterSink>();
services.AddEventBus();
```

## Event Types

See [TenantEvent](./TenantEvent.md) for the complete hierarchy of domain events, including:
- Tenant lifecycle events (Created, Activated, Suspended, Deactivated, Reactivated, Deleted)
- Configuration change events
- User management events
- Feature flag events
- Resource access events (high-frequency)
- Subscription update events
- Data isolation policy change events

## Best Practices

### Handler Implementation
- Keep handlers idempotent when possible
- Handle exceptions gracefully within handlers
- Avoid long-running operations in event handlers
- Consider using background queues for expensive operations

### Event Design
- Events should be immutable after creation
- Include all necessary context in the event itself
- Use correlation IDs for distributed tracing
- Keep events focused on a single concept or state change

### Performance Considerations
- The event bus is designed for in-process communication
- For cross-process communication, consider integrating with external message brokers
- Monitor subscriber counts to prevent unbounded growth (max 100 handlers per event type enforced)
- Use the high-frequency event marker (`IHighFrequencyEvent`) for events that may need sampling
- Consider `BufferedEventPublisher` for high-throughput scenarios

## Subscription Limits
To prevent unbounded fan-out DoS attacks, the event bus enforces:
- Maximum 100 handlers per event type
- Automatic rejection when limit is exceeded with clear error message

## Related Components
- [TenantEvent](./TenantEvent.md) - Base event class and specific event types
- [EventPublisher](./EventPublisher.md) - Context-enriching event publisher
- [BufferedEventPublisher](./BufferedEventPublisher.md) - High-throughput event publishing
- [IEventSubscriptionRegistry](./EventPublisher.md#event-subscription-registry) - Advanced subscription management
