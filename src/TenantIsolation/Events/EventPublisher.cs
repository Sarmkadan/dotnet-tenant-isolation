#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace TenantIsolation.Events;

/// <summary>
/// Event publisher interface for publishing domain events
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publish event to event bus
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : TenantEvent;

    /// <summary>
    /// Publish multiple events
    /// </summary>
    Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : TenantEvent;
}

/// <summary>
/// Event publisher implementation
/// Publishes events to the event bus with request context injection
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<EventPublisher> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EventPublisher(
        IEventBus eventBus,
        ILogger<EventPublisher> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _eventBus = eventBus;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Publish event with automatic context injection
    /// Injects correlation ID, tenant ID, and user ID from current request
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : TenantEvent
    {
        try
        {
            // Inject request context if available
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId))
                    @event.CorrelationId = correlationId?.ToString();

                if (httpContext.Items.TryGetValue("UserId", out var userId))
                    @event.SetUserId(userId?.ToString());
            }

            _logger.LogInformation("Publishing event {EventType} (ID: {EventId}) for tenant {TenantId}",
                typeof(TEvent).Name, @event.EventId, @event.TenantId);

            await _eventBus.PublishAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}",
                typeof(TEvent).Name);
            throw;
        }
    }

    /// <summary>
    /// Publish multiple events in batch
    /// Useful for atomic multi-event operations
    /// </summary>
    public async Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : TenantEvent
    {
        var eventList = events.ToList();
        if (!eventList.Any())
            return;

        _logger.LogInformation("Publishing batch of {Count} {EventType} events",
            eventList.Count, typeof(TEvent).Name);

        var tasks = eventList.Select(e => PublishAsync(e));
        await Task.WhenAll(tasks);
    }
}

/// <summary>
/// Event subscription registry for maintaining subscriptions
/// Provides discovery and management of event handlers
/// </summary>
public interface IEventSubscriptionRegistry
{
    /// <summary>
    /// Register event handler and return an <see cref="IDisposable"/> token for unsubscribing
    /// </summary>
    /// <param name="handler">Event handler to register</param>
    /// <param name="handlerName">Optional name for the handler</param>
    /// <returns>Subscription token that automatically unsubscribes when disposed</returns>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler, string? handlerName = null)
        where TEvent : TenantEvent;

    /// <summary>
    /// Register event handler and return an <see cref="IDisposable"/> token for unsubscribing
    /// </summary>
    /// <param name="handler">Event handler to register</param>
    /// <param name="handlerName">Optional name for the handler</param>
    /// <returns>Subscription token that automatically unsubscribes when disposed</returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler, string? handlerName = null)
        where TEvent : TenantEvent;

    /// <summary>
    /// Unregister a specific handler for an event type
    /// </summary>
    /// <param name="handler">Handler to unsubscribe</param>
    /// <returns>True if handler was found and removed; false otherwise</returns>
    bool Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : TenantEvent;

    /// <summary>
    /// Unregister a specific handler for an event type
    /// </summary>
    /// <param name="handler">Handler to unsubscribe</param>
    /// <returns>True if handler was found and removed; false otherwise</returns>
    bool Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : TenantEvent;

    /// <summary>
    /// Get all registered handlers for event type
    /// </summary>
    IEnumerable<EventHandlerInfo> GetHandlers<TEvent>() where TEvent : TenantEvent;

    /// <summary>
    /// Get all registered handlers
    /// </summary>
    IEnumerable<EventHandlerInfo> GetAllHandlers();
}

/// <summary>
/// Information about registered event handler
/// </summary>
public class EventHandlerInfo
{
    /// <summary>
    /// Type name of the event
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Name of the handler
    /// </summary>
    public string HandlerName { get; set; } = string.Empty;

    /// <summary>
    /// When the handler was registered
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Handler method signature hash for matching
    /// </summary>
    public string HandlerSignature { get; set; } = string.Empty;
}

/// <summary>
/// Subscription token that allows unsubscribing from events
/// </summary>
public sealed class SubscriptionToken : IDisposable
{
    private readonly EventSubscriptionRegistry _registry;
    private readonly Type _eventType;
    private readonly Delegate _handler;
    private bool _disposed;

    /// <summary>
    /// Initializes a new subscription token
    /// </summary>
    /// <param name="registry">Registry instance</param>
    /// <param name="eventType">Event type</param>
    /// <param name="handler">Handler delegate</param>
    public SubscriptionToken(EventSubscriptionRegistry registry, Type eventType, Delegate handler)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _eventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Unsubscribes from the event
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _registry.UnsubscribeCore(_eventType, _handler);
        }
    }
}

/// <summary>
/// Thread-safe event subscription registry implementation using immutable collections
/// Supports concurrent subscription and unsubscription without collection modification exceptions
/// </summary>
public class EventSubscriptionRegistry : IEventSubscriptionRegistry
{
    private readonly ConcurrentDictionary<Type, ImmutableList<EventHandlerInfo>> _handlers = new();

    /// <summary>
    /// Register event handler and return a subscription token for automatic cleanup
    /// </summary>
    /// <param name="handler">Event handler to register</param>
    /// <param name="handlerName">Optional name for the handler</param>
    /// <returns>Subscription token that automatically unsubscribes when disposed</returns>
    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler, string? handlerName = null)
        where TEvent : TenantEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        var handlerInfo = new EventHandlerInfo
        {
            EventType = eventType.Name,
            HandlerName = handlerName ?? handler.Method.Name,
            HandlerSignature = "Func",
            RegisteredAt = DateTime.UtcNow
        };

        _handlers.AddOrUpdate(
            eventType,
            ImmutableList.Create(handlerInfo),
            (_, existing) => existing.Add(handlerInfo)
        );

        return new SubscriptionToken(this, eventType, handler);
    }

    /// <summary>
    /// Register event handler and return a subscription token for automatic cleanup
    /// </summary>
    /// <param name="handler">Event handler to register</param>
    /// <param name="handlerName">Optional name for the handler</param>
    /// <returns>Subscription token that automatically unsubscribes when disposed</returns>
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler, string? handlerName = null)
        where TEvent : TenantEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        var handlerInfo = new EventHandlerInfo
        {
            EventType = eventType.Name,
            HandlerName = handlerName ?? handler.Method.Name,
            HandlerSignature = "Action",
            RegisteredAt = DateTime.UtcNow
        };

        _handlers.AddOrUpdate(
            eventType,
            ImmutableList.Create(handlerInfo),
            (_, existing) => existing.Add(handlerInfo)
        );

        return new SubscriptionToken(this, eventType, handler);
    }

    /// <summary>
    /// Unregister a specific handler for an event type
    /// </summary>
    /// <param name="handler">Handler to unsubscribe</param>
    /// <returns>True if handler was found and removed; false otherwise</returns>
    public bool Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : TenantEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        return UnsubscribeCore(typeof(TEvent), handler);
    }

    /// <summary>
    /// Unregister a specific handler for an event type
    /// </summary>
    /// <param name="handler">Handler to unsubscribe</param>
    /// <returns>True if handler was found and removed; false otherwise</returns>
    public bool Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : TenantEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        return UnsubscribeCore(typeof(TEvent), handler);
    }


    internal bool UnsubscribeCore(Type eventType, Delegate handler)
    {
        var oldList = _handlers.GetOrAdd(eventType, static _ => ImmutableList<EventHandlerInfo>.Empty);
        var handlerName = handler.Method.Name;
        var handlerSignature = handler switch
        {
            Func<TenantEvent, Task> => "Func",
            Action<TenantEvent> => "Action",
            _ => "Unknown"
        };

        var newList = oldList.RemoveAll(h =>
            string.Equals(h.HandlerName, handlerName, StringComparison.Ordinal) &&
            string.Equals(h.HandlerSignature, handlerSignature, StringComparison.Ordinal));

        if (newList.Count == oldList.Count)
        {
            return false; // Handler not found
        }

        _handlers.AddOrUpdate(
            eventType,
            newList,
            (_, _) => newList
        );

        return true;
    }

    /// <summary>
    /// Get all registered handlers for event type
    /// </summary>
    public IEnumerable<EventHandlerInfo> GetHandlers<TEvent>() where TEvent : TenantEvent
    {
        var eventType = typeof(TEvent);
        return _handlers.GetOrAdd(eventType, static _ => ImmutableList<EventHandlerInfo>.Empty)
            .Where(h => string.Equals(h.EventType, eventType.Name, StringComparison.Ordinal));
    }

    /// <summary>
    /// Get all registered handlers
    /// </summary>
    public IEnumerable<EventHandlerInfo> GetAllHandlers()
    {
        return _handlers.Values.SelectMany(h => h);
    }

    /// <summary>
    /// Clear all subscriptions (useful for testing)
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
    }
}
