// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

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
                    @event.UserId = userId?.ToString();
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
    /// Register event handler
    /// </summary>
    void RegisterHandler<TEvent>(Func<TEvent, Task> handler, string? handlerName = null)
        where TEvent : TenantEvent;

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
    public string EventType { get; set; } = string.Empty;
    public string HandlerName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}

/// <summary>
/// Event subscription registry implementation
/// </summary>
public class EventSubscriptionRegistry : IEventSubscriptionRegistry
{
    private readonly Dictionary<Type, List<EventHandlerInfo>> _handlers = new();
    private readonly object _lockObject = new();

    public void RegisterHandler<TEvent>(Func<TEvent, Task> handler, string? handlerName = null)
        where TEvent : TenantEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        lock (_lockObject)
        {
            var eventType = typeof(TEvent);
            if (!_handlers.ContainsKey(eventType))
                _handlers[eventType] = new List<EventHandlerInfo>();

            var handlerInfo = new EventHandlerInfo
            {
                EventType = eventType.Name,
                HandlerName = handlerName ?? handler.Method.Name,
                RegisteredAt = DateTime.UtcNow
            };

            _handlers[eventType].Add(handlerInfo);
        }
    }

    public IEnumerable<EventHandlerInfo> GetHandlers<TEvent>() where TEvent : TenantEvent
    {
        lock (_lockObject)
        {
            var eventType = typeof(TEvent);
            return _handlers.TryGetValue(eventType, out var handlers) ? handlers.ToList() : new List<EventHandlerInfo>();
        }
    }

    public IEnumerable<EventHandlerInfo> GetAllHandlers()
    {
        lock (_lockObject)
        {
            return _handlers.Values.SelectMany(h => h).ToList();
        }
    }
}
