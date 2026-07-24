#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TenantIsolation.Events;

/// <summary>
/// Event handler delegate
/// </summary>
public delegate Task EventHandlerDelegate<in TEvent>(TEvent @event) where TEvent : TenantEvent;

/// <summary>
/// Configuration options controlling per-handler retry and backoff behavior for <see cref="EventBus"/>
/// </summary>
public sealed class PublisherResilienceOptions
{
    /// <summary>
    /// Maximum number of retry attempts performed for a handler after its initial failed invocation
    /// Defaults to 3
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay used to compute exponential backoff between retry attempts
    /// The delay before attempt N is <c>BaseDelay * 2^(N-1)</c>
    /// Defaults to 200 milliseconds
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);
}

/// <summary>
/// Sink that receives events whose handler exhausted all configured retry attempts
/// </summary>
public interface IDeadLetterSink
{
    /// <summary>
    /// Handle an event whose delivery to a specific handler permanently failed
    /// </summary>
    /// <param name="event">The event that could not be delivered</param>
    /// <param name="handlerName">Name of the handler that failed to process the event</param>
    /// <param name="exception">The last exception thrown by the handler</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/>, <paramref name="handlerName"/>, or <paramref name="exception"/> is null</exception>
    Task HandleAsync<TEvent>(TEvent @event, string handlerName, Exception exception) where TEvent : TenantEvent;
}

/// <summary>
/// Default dead-letter sink implementation that logs the failure
/// Suitable as a fallback when no dedicated dead-letter storage is configured
/// </summary>
public sealed class LoggingDeadLetterSink : IDeadLetterSink
{
    private readonly ILogger<LoggingDeadLetterSink> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="LoggingDeadLetterSink"/>
    /// </summary>
    /// <param name="logger">Logger used to record dead-lettered events</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null</exception>
    public LoggingDeadLetterSink(ILogger<LoggingDeadLetterSink> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Logs the event, handler name, and terminal exception at error level
    /// </summary>
    /// <param name="event">The event that could not be delivered</param>
    /// <param name="handlerName">Name of the handler that failed to process the event</param>
    /// <param name="exception">The last exception thrown by the handler</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/>, <paramref name="handlerName"/>, or <paramref name="exception"/> is null</exception>
    public Task HandleAsync<TEvent>(TEvent @event, string handlerName, Exception exception) where TEvent : TenantEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentException.ThrowIfNullOrEmpty(handlerName);
        ArgumentNullException.ThrowIfNull(exception);

        _logger.LogError(exception,
            "Dead-lettered event {EventType} (ID: {EventId}) for tenant {TenantId} after exhausting retries for handler {HandlerName}",
            typeof(TEvent).Name, @event.EventId, @event.TenantId, handlerName);

        return Task.CompletedTask;
    }
}

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
    private readonly PublisherResilienceOptions _resilienceOptions;
    private readonly IDeadLetterSink _deadLetterSink;

    /// <summary>
    /// Initializes a new instance of <see cref="EventBus"/>
    /// </summary>
    /// <param name="logger">Logger used to record subscription and publishing activity</param>
    /// <param name="resilienceOptions">
    /// Optional retry/backoff configuration. When null, default <see cref="PublisherResilienceOptions"/> values are used
    /// </param>
    /// <param name="deadLetterSink">
    /// Optional sink invoked once a handler exhausts its retries. When null, a <see cref="LoggingDeadLetterSink"/> backed by <paramref name="logger"/>'s logger factory is not available,
    /// so a minimal internal fallback that logs through <paramref name="logger"/> is used instead
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null</exception>
    public EventBus(
        ILogger<EventBus> logger,
        IOptions<PublisherResilienceOptions>? resilienceOptions = null,
        IDeadLetterSink? deadLetterSink = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _resilienceOptions = resilienceOptions?.Value ?? new PublisherResilienceOptions();
        _deadLetterSink = deadLetterSink ?? new FallbackDeadLetterSink(_logger);
    }

    /// <summary>
    /// Minimal dead-letter sink used when no <see cref="IDeadLetterSink"/> is registered in DI,
    /// so retry exhaustion is never silently swallowed
    /// </summary>
    private sealed class FallbackDeadLetterSink : IDeadLetterSink
    {
        private readonly ILogger _logger;

        public FallbackDeadLetterSink(ILogger logger) => _logger = logger;

        public Task HandleAsync<TEvent>(TEvent @event, string handlerName, Exception exception) where TEvent : TenantEvent
        {
            ArgumentNullException.ThrowIfNull(@event);
            ArgumentException.ThrowIfNullOrEmpty(handlerName);
            ArgumentNullException.ThrowIfNull(exception);

            _logger.LogError(exception,
                "Dead-lettered event {EventType} (ID: {EventId}) for tenant {TenantId} after exhausting retries for handler {HandlerName}",
                typeof(TEvent).Name, @event.EventId, @event.TenantId, handlerName);

            return Task.CompletedTask;
        }
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

        // Dispatch to each handler independently so a failing handler (even after
        // exhausting its retries) never prevents delivery to the remaining handlers
        foreach (var handler in handlers)
        {
            if (handler is Func<TEvent, Task> typedHandler)
            {
                await DispatchWithRetryAsync(typedHandler, @event);
            }
        }
    }

    /// <summary>
    /// Invokes a single handler for an event, retrying on failure with exponential backoff.
    /// If all attempts fail, the event is forwarded to the configured <see cref="IDeadLetterSink"/>
    /// instead of propagating the exception, keeping handler failures isolated
    /// </summary>
    private async Task DispatchWithRetryAsync<TEvent>(Func<TEvent, Task> handler, TEvent @event)
        where TEvent : TenantEvent
    {
        var handlerName = handler.Method.Name;
        var maxRetries = Math.Max(0, _resilienceOptions.MaxRetries);
        var attempt = 0;

        while (true)
        {
            try
            {
                await handler(@event);
                return;
            }
            catch (Exception ex)
            {
                if (attempt >= maxRetries)
                {
                    _logger.LogError(ex,
                        "Handler {HandlerName} for {EventType} (ID: {EventId}) failed after {AttemptCount} attempt(s); dead-lettering",
                        handlerName, typeof(TEvent).Name, @event.EventId, attempt + 1);

                    await _deadLetterSink.HandleAsync(@event, handlerName, ex);
                    return;
                }

                var delay = TimeSpan.FromMilliseconds(
                    _resilienceOptions.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt));

                _logger.LogWarning(ex,
                    "Handler {HandlerName} for {EventType} (ID: {EventId}) failed on attempt {Attempt}; retrying in {DelayMs}ms",
                    handlerName, typeof(TEvent).Name, @event.EventId, attempt + 1, delay.TotalMilliseconds);

                attempt++;
                await Task.Delay(delay);
            }
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
    /// Uses the default EventPublisher implementation
    /// </summary>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDeadLetterSink, LoggingDeadLetterSink>();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        return services;
    }
}
