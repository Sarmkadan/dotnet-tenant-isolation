// existing content ...

## WebhookPayload

The `WebhookPayload` class represents the payload structure sent to registered webhook endpoints when events occur within a tenant. It contains all necessary information for external services to process tenant-specific events including the event identifier, type, tenant context, timestamp, and the actual event data. The payload also includes a signature for security verification.

### Members

- `EventId` - Unique identifier for the event
- `EventType` - Type of the event being delivered
- `TenantId` - Identifier of the tenant this event belongs to
- `Timestamp` - When the event occurred (UTC)
- `Data` - The actual event data being delivered
- `Signature` - HMAC-SHA256 signature for payload verification

### Example Usage

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        // Create a sample tenant event
        var tenantEvent = new TenantCreatedEvent(
            tenantName: "Acme Corporation",
            tenantSlug: "acme-corp",
            adminEmail: "admin@acme.com",
            isolationStrategy: "IsolationStrategy1"
        );
        
        // Create webhook payload
        var payload = new WebhookPayload
        {
            EventId = tenantEvent.EventId,
            EventType = nameof(TenantCreatedEvent),
            TenantId = tenantEvent.TenantId,
            Timestamp = tenantEvent.OccurredAt,
            Data = tenantEvent,
            Signature = string.Empty // Will be populated by WebhookHandler
        };
        
        // Serialize payload for transmission
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        Console.WriteLine($"Webhook payload for {payload.EventType}: {json}");
        
        // External service would verify signature using shared secret
        // string receivedSignature = request.Headers["X-Webhook-Signature"];
        // bool isValid = CryptographyUtility.VerifyHmacSha256(json, receivedSignature, webhook.Secret);
    }
}
```

## BackgroundTask

The `BackgroundTask` class represents a background task that can be queued for execution by the `BackgroundTaskQueue`. It tracks task execution metrics including pending, running, completed, and failed task counts, along with average execution time. Tasks can be prioritized and configured with retry policies.

### Members

- `Id` - Unique identifier for the task
- `Name` - Human-readable name describing the task
- `WorkItem` - The asynchronous delegate to execute (`Func<CancellationToken, Task>`)
- `EnqueuedAt` - When the task was enqueued
- `Priority` - Task priority level (`BackgroundTaskPriority`)
- `MaxRetries` - Maximum retry attempts for failed executions
- `PendingTasks` - Count of pending tasks
- `CompletedTasks` - Count of completed tasks
- `FailedTasks` - Count of failed tasks
- `RunningTasks` - Count of currently running tasks
- `AverageExecutionTime` - Average execution duration across completed tasks

### Example Usage

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup DI container
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundTaskQueue();
        var serviceProvider = services.BuildServiceProvider();

        // Resolve the background task queue
        var taskQueue = serviceProvider.GetRequiredService<BackgroundTaskQueue>();

        // Create a background task with a work item
        var task = new BackgroundTask(
            name: "Database Cleanup",
            workItem: async (cancellationToken) =>
            {
                Console.WriteLine("Starting database cleanup...");
                await Task.Delay(1000, cancellationToken);
                Console.WriteLine("Database cleanup completed!");
            },
            priority: BackgroundTaskPriority.Normal,
            maxRetries: 3
        );

        // Queue the task
        taskQueue.QueueTask(task);

        // Process tasks in a background service
        var hostedService = serviceProvider.GetRequiredService<BackgroundTaskHostedService>();
        await hostedService.StartAsync(CancellationToken.None);

        // Get queue statistics
        var stats = taskQueue.GetStatistics();
        Console.WriteLine($"Pending: {stats.PendingTasks}, Running: {stats.RunningTasks}, " +
                        $"Completed: {stats.CompletedTasks}, Failed: {stats.FailedTasks}");
    }
}

// Register BackgroundTaskQueue in DI container
public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddBackgroundTaskQueue();
    }
}
```

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

## IEventPublisher

The `IEventPublisher` interface provides a mechanism for publishing domain events with automatic request context injection. It extends the basic event publishing capabilities by automatically injecting correlation IDs, tenant IDs, and user IDs from the current HTTP request context into published events.

### Members

- `PublishAsync<TEvent>(TEvent @event)` - Publish a single event with automatic context injection
- `PublishBatchAsync<TEvent>(IEnumerable<TEvent> events)` - Publish multiple events atomically

### Example Usage

```csharp
public class Program
{
 public static async Task Main(string[] args)
 {
 // Setup DI container
 var services = new ServiceCollection();
 services.AddLogging();
 services.AddHttpContextAccessor();
 services.AddEventBus();
 services.AddSingleton<IEventPublisher, EventPublisher>();
 var serviceProvider = services.BuildServiceProvider();

 // Resolve dependencies
 var eventPublisher = serviceProvider.GetRequiredService<IEventPublisher>();
 var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

 // Create a tenant event
 var tenantEvent = new TenantCreatedEvent(
 "My Tenant", 
 "my-tenant", 
 "admin@example.com", 
 "IsolationStrategy1");

 // Publish single event (automatically injects context from HttpContext)
 await eventPublisher.PublishAsync(tenantEvent);

 // Publish multiple events in batch
 var events = new List<TenantEvent>
 {
 new TenantCreatedEvent("Tenant 1", "tenant-1", "user1@example.com", "StrategyA"),
 new TenantCreatedEvent("Tenant 2", "tenant-2", "user2@example.com", "StrategyB"),
 new TenantDeletedEvent("tenant-3")
 };
 await eventPublisher.PublishBatchAsync(events);
 }
}
```

## Event Subscription Registry

The event subscription registry provides discovery and management of event handlers. It allows registering handlers for specific event types and querying registered handlers.

### Members

- `RegisterHandler<TEvent>(Func<TEvent, Task> handler, string? handlerName = null)` - Register an event handler
- `GetHandlers<TEvent>()` - Get all registered handlers for a specific event type
- `GetAllHandlers()` - Get all registered handlers across all event types

### Example Usage

```csharp
// Register a handler
var registry = new EventSubscriptionRegistry();
registry.RegisterHandler<TenantCreatedEvent>(async (@event) => 
{
 Console.WriteLine($"Handler received: {@event.TenantName}");
}, "TenantCreatedLogger");

// Get handlers for specific event type
var tenantHandlers = registry.GetHandlers<TenantCreatedEvent>();
foreach (var handler in tenantHandlers)
{
 Console.WriteLine($"Handler: {handler.HandlerName}, Registered: {handler.RegisteredAt}");
}

// Get all registered handlers
var allHandlers = registry.GetAllHandlers();
foreach (var handler in allHandlers)
{
 Console.WriteLine($"{handler.EventType}: {handler.HandlerName}");
}
```

## ApiCallResult

`ApiCallResult<T>` encapsulates the outcome of an HTTP request made through `ExternalApiClient`. It indicates whether the call succeeded, provides the deserialized response payload, any error message, the HTTP status code, and the duration of the request.

**Example usage**

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Integration;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Register the external API client in DI
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddExternalApiClient();

        var provider = services.BuildServiceProvider();

        var apiClient = provider.GetRequiredService<IExternalApiClient>();

        // Perform a GET request
        var getResult = await apiClient.GetAsync<MyResponse>("https://api.example.com/resource");

        if (getResult.IsSuccess && getResult.Data != null)
        {
            Console.WriteLine($"GET succeeded in {getResult.Duration.TotalMilliseconds} ms. Data: {getResult.Data}");
        }
        else
        {
            Console.WriteLine($"GET failed (HTTP {(getResult.HttpStatusCode ?? 0)}): {getResult.ErrorMessage}");
        }

        // Perform a POST request
        var payload = new { Name = "Sample", Value = 42 };
        var postResult = await apiClient.PostAsync<MyResponse>("https://api.example.com/resource", payload);

        Console.WriteLine($"POST success: {postResult.IsSuccess}, status: {postResult.HttpStatusCode}");
    }

    // Sample DTO for deserialization
    public class MyResponse
    {
        public string? Id { get; set; }
        public string? Status { get; set; }

        public override string ToString() => $"Id={Id}, Status={Status}";
    }
}
```

// existing content ...````