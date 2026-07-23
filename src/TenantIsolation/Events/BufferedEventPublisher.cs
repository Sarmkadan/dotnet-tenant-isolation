#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace TenantIsolation.Events;

/// <summary>
/// Configuration options for buffered event publishing behavior
/// </summary>
public sealed class BufferedEventPublisherOptions
{
    /// <summary>
    /// Maximum number of events to buffer in the channel
    /// Defaults to 10,000 events
    /// </summary>
    public int ChannelCapacity { get; set; } = 10_000;

    /// <summary>
    /// Behavior when the channel is full
    /// DropOldest: Drop the oldest event to make room for new ones (recommended for telemetry)
    /// Wait: Block until space is available (recommended for critical lifecycle events)
    /// </summary>
    public BoundedChannelFullMode OverflowMode { get; set; } = BoundedChannelFullMode.DropOldest;

    /// <summary>
    /// Whether to enable buffering for IHighFrequencyEvent events
    /// Defaults to true
    /// </summary>
    public bool EnableHighFrequencyBuffering { get; set; } = true;

    /// <summary>
    /// Whether to enable buffering for all events (when EnableHighFrequencyBuffering is false)
    /// Defaults to false (only high-frequency events are buffered)
    /// </summary>
    public bool EnableAllEventsBuffering { get; set; } = false;

    /// <summary>
    /// Maximum number of events to process in a single batch
    /// Defaults to 100 events
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum delay before processing a batch, even if batch size isn't reached
    /// Defaults to 100 milliseconds
    /// </summary>
    public TimeSpan MaxBatchDelay { get; set; } = TimeSpan.FromMilliseconds(100);
}

/// <summary>
/// Event publisher decorator that buffers high-frequency events using System.Threading.Channels
/// to provide fire-and-forget publishing with configurable overflow behavior.
///
/// <para>Benefits:</para>
/// <list type="bullet">
/// <item><description>Non-blocking PublishAsync for high-frequency events</description></item>
/// <item><description>Configurable overflow behavior (DropOldest/Wait)</description></item>
/// <item><description>Background batch processing for efficiency</description></item>
/// <item><description>Metrics for dropped events and processing latency</description></item>
/// </list>
/// </summary>
/// <remarks>
/// Usage example:
/// <code>
/// services.AddEventBus();
/// services.AddSingleton&lt;IEventPublisher, BufferedEventPublisher&gt;();
/// services.Configure&lt;BufferedEventPublisherOptions&gt;(options => {
///     options.ChannelCapacity = 50_000;
///     options.OverflowMode = BoundedChannelFullMode.DropOldest;
/// });
/// </code>
/// </remarks>
public class BufferedEventPublisher : IEventPublisher, IDisposable
{
    private readonly IEventPublisher _innerPublisher;
    private readonly ILogger<BufferedEventPublisher> _logger;
    private readonly BufferedEventPublisherOptions _options;
    private readonly Channel<TenantEvent> _channel;
    private readonly ConcurrentDictionary<Type, long> _droppedEventsCounter = new();
    private readonly ConcurrentDictionary<Type, long> _publishedEventsCounter = new();
    private readonly ConcurrentDictionary<Type, long> _processingLatencyMs = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _drainLoopTask;
    private int _disposed;

    /// <summary>
    /// Creates a new BufferedEventPublisher that wraps an inner event publisher
    /// </summary>
    /// <param name="innerPublisher">The underlying event publisher to delegate to</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <param name="options">Configuration options for buffering behavior</param>
    /// <exception cref="ArgumentNullException">Thrown if innerPublisher or logger is null</exception>
    public BufferedEventPublisher(
        IEventPublisher innerPublisher,
        ILogger<BufferedEventPublisher> logger,
        BufferedEventPublisherOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(innerPublisher);
        ArgumentNullException.ThrowIfNull(logger);

        _innerPublisher = innerPublisher;
        _logger = logger;
        _options = options ?? new BufferedEventPublisherOptions();

        // Create the appropriate channel based on overflow mode
        _channel = _options.OverflowMode == BoundedChannelFullMode.DropOldest
            ? Channel.CreateBounded<TenantEvent>(new BoundedChannelOptions(_options.ChannelCapacity)
            {
                FullMode = _options.OverflowMode,
                SingleReader = true,
                SingleWriter = false
            })
            : Channel.CreateBounded<TenantEvent>(new BoundedChannelOptions(_options.ChannelCapacity)
            {
                FullMode = _options.OverflowMode,
                SingleReader = true,
                SingleWriter = false
            });

        _logger.LogInformation(
            "BufferedEventPublisher initialized with capacity={Capacity}, overflowMode={OverflowMode}, " +
            "highFrequencyBuffering={EnableHighFrequencyBuffering}, allEventsBuffering={EnableAllEventsBuffering}",
            _options.ChannelCapacity,
            _options.OverflowMode,
            _options.EnableHighFrequencyBuffering,
            _options.EnableAllEventsBuffering);
    }

    /// <summary>
    /// Publish event with automatic buffering based on event type
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="event">Event to publish</param>
    /// <returns>Task representing the publish operation</returns>
    /// <exception cref="ArgumentNullException">Thrown if event is null</exception>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : TenantEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        // Track published event count
        _publishedEventsCounter.AddOrUpdate(
            typeof(TEvent),
            1,
            (_, count) => count + 1);

        // Determine if this event should be buffered
        bool shouldBuffer = ShouldBufferEvent(@event);

        if (shouldBuffer)
        {
            await TryWriteToChannelAsync(@event);
        }
        else
        {
            // Non-buffered events are published directly (critical lifecycle events)
            await _innerPublisher.PublishAsync(@event);
        }
    }

    /// <summary>
    /// Publish multiple events in batch
    /// Events are distributed between buffered and direct publishing based on their type
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="events">Collection of events to publish</param>
    /// <returns>Task representing the batch publish operation</returns>
    /// <exception cref="ArgumentNullException">Thrown if events is null</exception>
    public async Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : TenantEvent
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        if (!eventList.Any())
        {
            return;
        }

        // Track published event count
        _publishedEventsCounter.AddOrUpdate(
            typeof(TEvent),
            eventList.Count,
            (_, count) => count + eventList.Count);

        // Process each event
        foreach (var @event in eventList)
        {
            bool shouldBuffer = ShouldBufferEvent(@event);

            if (shouldBuffer)
            {
                await TryWriteToChannelAsync(@event);
            }
            else
            {
                // Non-buffered events are published directly
                await _innerPublisher.PublishAsync(@event);
            }
        }
    }

    /// <summary>
    /// Gets the number of events dropped due to channel overflow
    /// </summary>
    /// <typeparam name="TEvent">Event type to check</typeparam>
    /// <returns>Number of dropped events, or 0 if none</returns>
    public long GetDroppedEventsCount<TEvent>() where TEvent : TenantEvent
    {
        return _droppedEventsCounter.GetValueOrDefault(typeof(TEvent), 0);
    }

    /// <summary>
    /// Gets the total number of events published
    /// </summary>
    /// <typeparam name="TEvent">Event type to check</typeparam>
    /// <returns>Number of published events, or 0 if none</returns>
    public long GetPublishedEventsCount<TEvent>() where TEvent : TenantEvent
    {
        return _publishedEventsCounter.GetValueOrDefault(typeof(TEvent), 0);
    }

    /// <summary>
    /// Gets the average processing latency in milliseconds for events of the specified type
    /// </summary>
    /// <typeparam name="TEvent">Event type to check</typeparam>
    /// <returns>Average processing latency in milliseconds, or 0 if no events processed</returns>
    public double GetAverageProcessingLatencyMs<TEvent>() where TEvent : TenantEvent
    {
        var type = typeof(TEvent);
        if (_processingLatencyMs.TryGetValue(type, out var totalMs) && totalMs > 0)
        {
            var count = _publishedEventsCounter.GetValueOrDefault(type, 1);
            return (double)totalMs / count;
        }
        return 0;
    }

    /// <summary>
    /// Starts the background drain loop if not already running
    /// </summary>
    private void StartDrainLoopIfNeeded()
    {
        if (_drainLoopTask == null || _drainLoopTask.IsCompleted)
        {
            _drainLoopTask = Task.Run(DrainChannelAsync);
            _logger.LogInformation("Started BufferedEventPublisher drain loop");
        }
    }

    /// <summary>
    /// Background task that drains events from the channel and publishes them
    /// </summary>
    private async Task DrainChannelAsync()
    {
        try
        {
            _logger.LogInformation("BufferedEventPublisher drain loop started");

            while (!_cts.IsCancellationRequested)
            {
                var batch = new List<TenantEvent>(_options.BatchSize);
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    // Try to read a batch of events
                    for (int i = 0; i < _options.BatchSize; i++)
                    {
                        if (_cts.IsCancellationRequested)
                        {
                            break;
                        }

                        if (await _channel.Reader.WaitToReadAsync(_cts.Token))
                        {
                            if (_channel.Reader.TryRead(out var @event))
                            {
                                batch.Add(@event);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (batch.Count > 0)
                    {
                        await ProcessBatchAsync(batch);
                    }
                    else
                    {
                        // No events available, wait for more
                        await Task.Delay(_options.MaxBatchDelay, _cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("BufferedEventPublisher drain loop cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in BufferedEventPublisher drain loop");
                    await Task.Delay(TimeSpan.FromSeconds(1), _cts.Token);
                }

                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 100)
                {
                    _logger.LogDebug("BufferedEventPublisher batch processing took {ElapsedMs}ms for {BatchSize} events",
                        stopwatch.ElapsedMilliseconds, batch.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BufferedEventPublisher drain loop failed");
        }
        finally
        {
            _logger.LogInformation("BufferedEventPublisher drain loop stopped");
        }
    }

    /// <summary>
    /// Process a batch of events
    /// </summary>
    private async Task ProcessBatchAsync(IReadOnlyList<TenantEvent> batch)
    {
        if (batch.Count == 0)
        {
            return;
        }

        var eventType = batch[0].GetType();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Processing batch of {BatchSize} {EventType} events", batch.Count, eventType.Name);

            // Publish all events in the batch
            var publishTasks = batch.Select(e => _innerPublisher.PublishAsync(e));
            await Task.WhenAll(publishTasks);

            stopwatch.Stop();
            var processingMs = stopwatch.ElapsedMilliseconds;

            // Update metrics
            _processingLatencyMs.AddOrUpdate(
                eventType,
                processingMs,
                (_, totalMs) => totalMs + processingMs);

            _logger.LogInformation(
                "Processed batch of {BatchSize} {EventType} events in {ProcessingMs}ms",
                batch.Count, eventType.Name, processingMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process batch of {BatchSize} {EventType} events",
                batch.Count, eventType.Name);
        }
    }

    /// <summary>
    /// Determines if an event should be buffered based on its type
    /// </summary>
    private bool ShouldBufferEvent(TenantEvent @event)
    {
        // Buffer high-frequency events if enabled
        if (@event is IHighFrequencyEvent && _options.EnableHighFrequencyBuffering)
        {
            return true;
        }

        // Buffer all events if explicitly enabled
        if (_options.EnableAllEventsBuffering)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to write an event to the channel, tracking drops if channel is full
    /// </summary>
    private async ValueTask TryWriteToChannelAsync(TenantEvent @event)
    {
        try
        {
            // Start drain loop if not already running
            StartDrainLoopIfNeeded();

            // Try to write to channel without blocking the caller
            if (await _channel.Writer.WaitToWriteAsync(_cts.Token))
            {
                if (!_channel.Writer.TryWrite(@event))
                {
                    // Channel is full and overflow mode is DropOldest, so TryWrite returns false
                    TrackDroppedEvent(@event);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // If we're shutting down, publish directly
            await _innerPublisher.PublishAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write event {EventType} to channel, publishing directly",
                @event.GetType().Name);
            await _innerPublisher.PublishAsync(@event);
        }
    }

    /// <summary>
    /// Tracks an event that was dropped due to channel overflow
    /// </summary>
    private void TrackDroppedEvent(TenantEvent @event)
    {
        var type = @event.GetType();
        _droppedEventsCounter.AddOrUpdate(
            type,
            1,
            (_, count) => count + 1);

        _logger.LogWarning("Dropped event {EventType} due to channel overflow (dropped count: {DroppedCount})",
            type.Name, _droppedEventsCounter.GetValueOrDefault(type, 0));
    }

    /// <summary>
    /// Disposes the background drain loop and cleans up resources
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _logger.LogInformation("Disposing BufferedEventPublisher...");

        try
        {
            _cts.Cancel();

            // Complete the channel writer to allow drain loop to finish
            _channel.Writer.Complete();

            // Wait for drain loop to complete
            if (_drainLoopTask != null)
            {
                try
                {
                    _drainLoopTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException) { /* Ignore task cancellation */ }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during BufferedEventPublisher disposal");
        }
        finally
        {
            _cts.Dispose();
            _logger.LogInformation("BufferedEventPublisher disposed");
        }
    }
}

/// <summary>
/// Extension methods for configuring BufferedEventPublisher in DI container
/// </summary>
public static class BufferedEventPublisherExtensions
{
    /// <summary>
    /// Registers BufferedEventPublisher as the IEventPublisher implementation
    /// Uses default BufferedEventPublisherOptions
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown if services is null</exception>
    public static IServiceCollection AddBufferedEventPublisher(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEventBus(); // Ensure IEventBus and EventPublisher are registered
        services.AddSingleton<IEventPublisher>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<BufferedEventPublisher>>();
            var innerPublisher = provider.GetRequiredService<IEventPublisher>();
            var options = provider.GetService<IOptions<BufferedEventPublisherOptions>>()?.Value
                ?? new BufferedEventPublisherOptions();

            return new BufferedEventPublisher(innerPublisher, logger, options);
        });

        services.AddOptions<BufferedEventPublisherOptions>();

        return services;
    }

    /// <summary>
    /// Registers BufferedEventPublisher with custom configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>Service collection for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown if services or configure is null</exception>
    public static IServiceCollection AddBufferedEventPublisher(
        this IServiceCollection services,
        Action<BufferedEventPublisherOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddBufferedEventPublisher();
        services.Configure(configure);

        return services;
    }
}