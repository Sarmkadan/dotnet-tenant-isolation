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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="@event"/> is null.</exception>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : TenantEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

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
    /// <param name="tenantId">Optional tenant ID to filter by. If null or empty, returns all handlers regardless of tenant.</param>
    IEnumerable<EventHandlerInfo> GetHandlers<TEvent>(Guid? tenantId = null) where TEvent : TenantEvent;

    /// <summary>
    /// Get all registered handlers
    /// </summary>
    /// <param name="tenantId">Optional tenant ID to filter by. If null or empty, returns all handlers regardless of tenant.</param>
    IEnumerable<EventHandlerInfo> GetAllHandlers(Guid? tenantId = null);
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
    /// Tenant ID that registered this handler (empty if not tenant-scoped)
    /// </summary>
    public Guid TenantId { get; set; }

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
/// Base class for tenant-scoped event handlers
/// </summary>
/// <typeparam name="TEvent">Event type</typeparam>
public abstract class TenantEventHandler<TEvent> where TEvent : TenantEvent
{
    /// <summary>
    /// Tenant ID that this handler is scoped to
    /// </summary>
    public Guid TenantId { get; }

    /// <summary>
    /// Initializes a new tenant-scoped event handler
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    protected TenantEventHandler(Guid tenantId)
    {
        TenantId = tenantId;
    }

    /// <summary>
    /// Handle the event
    /// </summary>
    /// <param name="@event">Event to handle</param>
    /// <returns>Task</returns>
    public abstract Task HandleAsync(TEvent @event);
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
    // Maximum number of handlers allowed per event type to prevent unbounded fan-out DoS
    // Each tenant can register up to this many handlers for a single event type
    private const int MaxHandlersPerEventType = 100;

    private readonly ConcurrentDictionary<Type, ImmutableList<EventHandlerInfo>> _handlers = new();

    /// <summary>
    /// Register event handler and return a subscription token for automatic cleanup
    /// </summary>
    /// <param name="handler">Event handler to register</param>
    /// <param name="handlerName">Optional name for the handler</param>
    /// <returns>Subscription token that automatically unsubscribes when disposed</returns>
    /// <exception cref="InvalidOperationException">Thrown when the maximum number of handlers per event type has been reached</exception>
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
            RegisteredAt = DateTime.UtcNow,
            TenantId = handler.Target switch
            {
                // Extract tenant ID from handler's target if it's a tenant-scoped handler
                TenantEventHandler<TEvent> tenantHandler => tenantHandler.TenantId,
                _ => Guid.Empty
            }
        };

        // Check if we've reached the maximum number of handlers per event type
        var currentHandlers = _handlers.GetOrAdd(eventType, static _ => ImmutableList<EventHandlerInfo>.Empty);
        if (currentHandlers.Count >= MaxHandlersPerEventType)
        {
            throw new InvalidOperationException(
                $"Cannot register handler for event type '{eventType.Name}'. " +
                $"Maximum of {MaxHandlersPerEventType} handlers per event type has been reached. " +
                "This limit prevents unbounded fan-out DoS attacks.");
        }

    // Use direct assignment for thread-safe update
    _handlers[eventType] = _handlers.TryGetValue(eventType, out var existingList)
        ? existingList.Add(handlerInfo)
        : ImmutableList.Create(handlerInfo);

        return new SubscriptionToken(this, eventType, handler);
    }

    /// <summary>
    /// Register event handler and return a subscription token for automatic cleanup
    /// </summary>
    /// <param name="handler">Event handler to register</param>
    /// <param name="handlerName">Optional name for the handler</param>
    /// <returns>Subscription token that automatically unsubscribes when disposed</returns>
    /// <exception cref="InvalidOperationException">Thrown when the maximum number of handlers per event type has been reached</exception>
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
            RegisteredAt = DateTime.UtcNow,
            TenantId = handler.Target switch
            {
                // Extract tenant ID from handler's target if it's a tenant-scoped handler
                TenantEventHandler<TEvent> tenantHandler => tenantHandler.TenantId,
                _ => Guid.Empty
            }
        };

        // Check if we've reached the maximum number of handlers per event type
        var currentHandlers = _handlers.GetOrAdd(eventType, static _ => ImmutableList<EventHandlerInfo>.Empty);
        if (currentHandlers.Count >= MaxHandlersPerEventType)
        {
            throw new InvalidOperationException(
                $"Cannot register handler for event type '{eventType.Name}'. " +
                $"Maximum of {MaxHandlersPerEventType} handlers per event type has been reached. " +
                "This limit prevents unbounded fan-out DoS attacks.");
        }

    // Use direct assignment for thread-safe update
    _handlers[eventType] = _handlers.TryGetValue(eventType, out var existingList)
        ? existingList.Add(handlerInfo)
        : ImmutableList.Create(handlerInfo);

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

    // Direct assignment is thread-safe for this operation
    _handlers[eventType] = newList;

        return true;
    }


    /// <summary>
    /// Get registered handlers for event type, optionally filtered by tenant
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="tenantId">Optional tenant ID to filter by. If null or empty, returns all handlers regardless of tenant.</param>
    /// <returns>Filtered list of handlers</returns>
    public IEnumerable<EventHandlerInfo> GetHandlers<TEvent>(Guid? tenantId = null) where TEvent : TenantEvent
    {
        var eventType = typeof(TEvent);
        var handlers = _handlers.GetOrAdd(eventType, static _ => ImmutableList<EventHandlerInfo>.Empty);

        if (!tenantId.HasValue)
        {
            // Return all handlers if no tenant filter specified
            return handlers.Where(h => string.Equals(h.EventType, eventType.Name, StringComparison.Ordinal));
        }

        // Filter by tenant ID if specified
        return handlers.Where(h => string.Equals(h.EventType, eventType.Name, StringComparison.Ordinal) && h.TenantId == tenantId.Value);
    }

    /// <summary>
    /// Clear all subscriptions (useful for testing)
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
    }

    /// <summary>
    /// Get all registered handlers
    /// </summary>
    /// <param name="tenantId">Optional tenant ID to filter by. If null or empty, returns all handlers regardless of tenant.</param>
    /// <returns>Filtered list of all handlers</returns>
    public IEnumerable<EventHandlerInfo> GetAllHandlers(Guid? tenantId = null)
    {
        if (!tenantId.HasValue)
        {
            // Return all handlers if no tenant filter specified
            return _handlers.Values.SelectMany(h => h);
        }

        // Filter by tenant ID if specified
        return _handlers.Values.SelectMany(h => h).Where(h => h.TenantId == tenantId.Value);
    }
}
