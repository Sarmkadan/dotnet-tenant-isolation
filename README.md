// existing content ...

## IEventBus

The `IEventBus` interface represents an event bus that enables publish-subscribe communication between components. It allows subscribers to register for specific event types and publishers to send events to all registered subscribers.

### Example Usage

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        // Create an instance of IEventBus
        var eventBus = new EventBus(new NullLogger<EventBus>());

        // Subscribe to TenantCreatedEvent
        eventBus.Subscribe<TenantCreatedEvent>(async (@event) =>
        {
            Console.WriteLine($"Received TenantCreatedEvent: {@event.TenantName}");
        });

        // Publish TenantCreatedEvent
        var tenantCreatedEvent = new TenantCreatedEvent("My Tenant", "my-tenant", "admin@example.com", "IsolationStrategy1");
        eventBus.PublishAsync(tenantCreatedEvent).Wait();

        // Get subscriber count
        var subscriberCount = eventBus.GetSubscriberCount<TenantCreatedEvent>();
        Console.WriteLine($"Subscriber count: {subscriberCount}");

        // Unsubscribe
        eventBus.Unsubscribe<TenantCreatedEvent>(async (@event) =>
        {
            Console.WriteLine($"Received TenantCreatedEvent: {@event.TenantName}");
        });

        // Clear all subscriptions
        eventBus.ClearSubscriptions();
    }
}

// Register IEventBus in DI container
public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddEventBus();
    }
}
```

// existing content ...
