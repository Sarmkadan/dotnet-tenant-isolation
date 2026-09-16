# BufferedEventPublisher

`BufferedEventPublisher` is an `IEventPublisher` decorator for reducing the caller-side cost of high-volume, in-process event publication. It sends selected events through a bounded `System.Threading.Channels` channel and publishes them from a background drain loop. Events that are not selected for buffering go directly to the wrapped publisher.

This component trades immediate delivery guarantees for bounded memory use and caller throughput. It is not a durable queue: buffered events exist only in the current process and can be lost because of overflow, process termination, disposal, or a downstream publishing failure.

## Event routing

For each event, `BufferedEventPublisher` applies these rules in order:

1. If the event implements `IHighFrequencyEvent` and `EnableHighFrequencyBuffering` is `true`, enqueue it.
2. Otherwise, if `EnableAllEventsBuffering` is `true`, enqueue it.
3. Otherwise, await `_innerPublisher.PublishAsync(...)` on the caller's path.

With the default options, only `IHighFrequencyEvent` events are buffered. Tenant lifecycle and other ordinary events are therefore published directly. Setting `EnableHighFrequencyBuffering` to `false` does not enable buffering for ordinary events; `EnableAllEventsBuffering` must be set separately.

Both `PublishAsync` and `PublishBatchAsync` use these rules. `PublishBatchAsync` first materializes the input and then processes every event individually. It does not place the input collection into the channel as one atomic unit, and direct events are awaited sequentially.

## Buffering and draining

The publisher owns one bounded channel of `TenantEvent` values. It is configured for multiple writers and a single reader. The background drain loop starts lazily when the first buffered event is submitted.

The drain loop reads up to `BatchSize` events and passes the resulting list to the wrapped publisher. It starts all publications in that list and awaits them together with `Task.WhenAll`, so downstream publication within a drained batch is concurrent. A drained batch can contain different runtime event types; it is not partitioned by type.

`BatchSize` is an upper bound on the number of events processed together, not a threshold callers must reach before processing starts. In the current implementation, each read waits for another item until the batch is full, cancellation occurs, or the channel completes. Consequently, `MaxBatchDelay` is only used when a drain iteration produces no events; it is not a strict deadline that flushes a partially filled batch.

Ordering should not be treated as a delivery guarantee. The channel has one reader and yields queued items in channel order, but events in a drained batch are handed to the inner publisher concurrently, so their handlers can complete in a different order.

## Options

`BufferedEventPublisherOptions` provides the following settings:

| Option | Default | Effect |
| --- | --- | --- |
| `ChannelCapacity` | `10_000` | Maximum number of events held by the bounded channel. |
| `OverflowMode` | `BoundedChannelFullMode.DropOldest` | Channel behavior when capacity is reached. |
| `EnableHighFrequencyBuffering` | `true` | Buffers events implementing `IHighFrequencyEvent`. |
| `EnableAllEventsBuffering` | `false` | Buffers all events not already selected by the high-frequency rule. |
| `BatchSize` | `100` | Maximum number of channel items collected for one background publish operation. |
| `MaxBatchDelay` | `100 ms` | Delay used when a drain iteration has no events; it does not currently time-limit a partial batch. |

The options are consumed when `BufferedEventPublisher` is constructed. Changing an options object afterward is not a supported way to reconfigure the channel because its capacity and full mode have already been established.

### Overflow modes

The channel uses the configured `BoundedChannelFullMode` directly:

- `DropOldest` removes the oldest queued item to accept a new one. This favors recent telemetry but permits silent data loss.
- `Wait` applies backpressure: `PublishAsync` waits until the channel can accept the event.
- Other `BoundedChannelFullMode` values are also passed through to the channel and follow the .NET channel semantics.

`GetDroppedEventsCount<TEvent>()` only counts occasions when the publisher's `TryWrite` call returns `false`. Automatic eviction performed internally by a drop-mode channel is not necessarily observable through that return value, so this counter should not be used as an authoritative loss metric for `DropOldest`.

## Construction and configuration

The decorator needs the publisher that performs actual delivery, a logger, and optionally an options instance:

```csharp
var options = new BufferedEventPublisherOptions
{
    ChannelCapacity = 50_000,
    BatchSize = 200,
    OverflowMode = BoundedChannelFullMode.DropOldest
};

using var publisher = new BufferedEventPublisher(
    innerPublisher,
    logger,
    options);

await publisher.PublishAsync(resourceAccessedEvent);
```

The library also exposes `AddBufferedEventPublisher()` and an overload accepting `Action<BufferedEventPublisherOptions>`:

```csharp
services.AddBufferedEventPublisher(options =>
{
    options.ChannelCapacity = 50_000;
    options.BatchSize = 200;
    options.OverflowMode = BoundedChannelFullMode.Wait;
});
```

The registered instance is a singleton, while `AddEventBus()` registers the default `IEventPublisher` as scoped. The extension's factory resolves `IEventPublisher` while it is itself constructing the final `IEventPublisher`; applications should validate this registration in their dependency-injection setup and can construct the decorator explicitly around a known inner publisher when service resolution or lifetime composition is unsuitable.

## Completion and error behavior

For direct events, the returned task represents the inner publish operation and propagates its exception.

For buffered events, the returned task normally represents acceptance into the channel, not handler completion. Once accepted, downstream failures happen in the background. A failure from any publication in a drained batch is logged, the exception is not propagated to the original caller, and the implementation does not retry or re-enqueue that batch. Any retry or dead-letter behavior supplied by the inner publisher still applies before its task fails.

If channel submission is cancelled during shutdown, or submission throws for another reason, the publisher falls back to publishing that event directly through the inner publisher.

`Dispose()` cancels the drain loop, completes the writer, and waits for the loop for up to five seconds. Cancellation can stop the loop without draining all queued events, so disposal is not a flush guarantee. Call `Dispose()` during application shutdown to release the cancellation token source, but use a durable broker when every accepted event must survive shutdown or process failure.

## Counters and logging

The publisher exposes three per-generic-type observations:

- `GetPublishedEventsCount<TEvent>()` counts events submitted to `PublishAsync` or `PublishBatchAsync`, including direct, buffered, failed, and potentially dropped events. It is an attempted-publication count, not a delivered-event count.
- `GetDroppedEventsCount<TEvent>()` reports explicit failed channel writes recorded by the publisher, subject to the overflow caveat above.
- `GetAverageProcessingLatencyMs<TEvent>()` divides accumulated background batch time by the submitted count for `TEvent`. Batch latency is attributed to the runtime type of the first event in each drained batch, so mixed-type batches and direct events make this an approximate diagnostic rather than a precise per-event latency measurement.

Initialization, drain-loop lifecycle, batch completion, overflow tracking, and failures are also written through `ILogger<BufferedEventPublisher>`.

## Choosing a strategy

Use `DropOldest` for replaceable telemetry where keeping recent data and avoiding producer backpressure matter more than complete delivery. Use `Wait` when bounded memory and preserving queued events are more important than caller latency. Leave lifecycle and control-plane events on the direct path unless their consumers explicitly tolerate asynchronous, non-durable, potentially reordered delivery.

For persistence, cross-process delivery, acknowledgements, replay, or strong delivery guarantees, use an external message broker rather than this in-memory decorator.

## Related documentation

- [Event bus](./event-bus.md)
- [Tenant events](./TenantEvent.md)
- [IEventPublisher](./IEventPublisher.md)
