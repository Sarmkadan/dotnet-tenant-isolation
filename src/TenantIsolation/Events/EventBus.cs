#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace TenantIsolation.Events;

/// <summary>
/// Event handler delegate
/// </summary>
public delegate Task EventHandlerDelegate<in TEvent>(TEvent @event) where TEvent : TenantEvent;

/// <summary>
/// Event bus interface for pub-sub communication
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribe to events of specific type
    /// </summary>
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : TenantEvent;

    /// <summary>
    /// Unsubscribe from events
    /// </summary>
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : TenantEvent;

    /// <summary>
    /// Publish event to all subscribers
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : TenantEvent;

    /// <summary>
    /// Get count of subscribers for event type
    /// </summary>
    int GetSubscriberCount<TEvent>() where TEvent : TenantEvent;
}

/// <summary>
/// In-memory event bus implementation using publish-subscribe pattern
/// Supports type-safe event subscriptions and async handling
/// </summary>
public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ILogger<EventBus> _logger;
    private readonly object _lockObject = new();

    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to events of specific type
    /// Multiple handlers can subscribe to same event type
    /// </summary>
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : TenantEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        lock (_lockObject)
        {
            var eventType = typeof(TEvent);
            if (!_subscribers.ContainsKey(eventType))
                _subscribers[eventType] = new List<Delegate>();

            _subscribers[eventType].Add(handler);
            _logger.LogInformation("Subscribed to {EventType}. Total subscribers: {Count}",
                eventType.Name, _subscribers[eventType].Count);
        }
    }

    /// <summary>
    /// Unsubscribe from events
    /// </summary>
    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : TenantEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        lock (_lockObject)
        {
            var eventType = typeof(TEvent);
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
                _logger.LogInformation("Unsubscribed from {EventType}. Remaining subscribers: {Count}",
                    eventType.Name, handlers.Count);
            }
        }
    }

    /// <summary>
    /// Publish event to all subscribers
    /// Executes handlers sequentially, collecting any exceptions
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : TenantEvent
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent);
        List<Delegate>? handlers;

        lock (_lockObject)
        {
            if (!_subscribers.TryGetValue(eventType, out handlers) || handlers.Count == 0)
            {
                _logger.LogDebug("No subscribers for event {EventType}", eventType.Name);
                return;
            }

            // Create a copy to allow concurrent modifications
            handlers = new List<Delegate>(handlers);
        }

        _logger.LogInformation("Publishing {EventType} (ID: {EventId}) to {HandlerCount} subscribers",
            eventType.Name, @event.EventId, handlers.Count);

        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
        {
            try
            {
                if (handler is Func<TEvent, Task> typedHandler)
                {
                    await typedHandler(@event);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in event handler for {EventType}",
                    eventType.Name);
                exceptions.Add(ex);
            }
        }

        // If there were exceptions, throw aggregate exception
        if (exceptions.Count > 0)
        {
            throw new AggregateException(
                $"One or more handlers failed when processing {eventType.Name}",
                exceptions);
        }
    }

    /// <summary>
    /// Get count of subscribers for event type
    /// </summary>
    public int GetSubscriberCount<TEvent>() where TEvent : TenantEvent
    {
        lock (_lockObject)
        {
            var eventType = typeof(TEvent);
            return _subscribers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
        }
    }

    /// <summary>
    /// Clear all subscriptions (useful for testing)
    /// </summary>
    public void ClearSubscriptions()
    {
        lock (_lockObject)
        {
            _subscribers.Clear();
            _logger.LogInformation("Cleared all event subscriptions");
        }
    }
}

/// <summary>
/// Extension methods for DI registration
/// </summary>
public static class EventBusExtensions
{
    /// <summary>
    /// Register event bus and event publisher in DI container
    /// </summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, EventBus>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        return services;
    }
}
