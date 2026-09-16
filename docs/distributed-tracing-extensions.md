# DistributedTracingExtensions

`DistributedTracingExtensions` provides an ambient `TracingContext` for an asynchronous execution flow, helpers for nested operations, correlation-aware logging, and ASP.NET Core registration and middleware extensions. The implementation is in `src/TenantIsolation/Utilities/DistributedTracingExtensions.cs` under the `TenantIsolation.Utilities` namespace.

These helpers manage the library's own IDs and context. They do not create `System.Diagnostics.Activity` instances, emit OpenTelemetry spans, or automatically propagate W3C `traceparent` headers. `ActivityTraceId` and `ActivitySpanId` are used only to generate correctly sized random identifier strings.

## Tracing context

A new `TracingContext` initializes:

- `CorrelationId` as a 32-character GUID in `N` format.
- `TraceId` and `SpanId` as random `ActivityTraceId` and `ActivitySpanId` strings.
- `StartTime` to `DateTime.UtcNow`.
- `Metadata` to an empty `Dictionary<string, string>`.

It can also carry `ParentSpanId`, `RequestPath`, `TenantId`, and `UserId`. The current context is stored in `AsyncLocal<TracingContext?>`, so it follows the normal .NET execution context across `await` calls. The context and its metadata dictionary remain mutable; do not mutate the same instance concurrently without synchronization.

## Context helpers

| Method | Behavior |
| --- | --- |
| `GetCurrentContext()` | Returns the current context, or `null` when no scope or context has been established. |
| `SetCurrentContext(context)` | Makes the supplied non-null instance current. It does not return or dispose the previous context. |
| `GetOrCreateContext()` | Returns the current instance or creates a new one. Creation alone does **not** make the new instance current. |
| `CreateChildContext(operationName)` | Creates a new span context without making it current. It inherits correlation ID, trace ID, tenant ID, user ID, request path, and a copy of the parent's metadata. The parent's span ID becomes `ParentSpanId`, while the child receives a new span ID. A non-empty operation name is stored as metadata under `operation`. |
| `BeginTracingScope(context)` | Makes the supplied context current and returns an `IDisposable` that restores the previous context when disposed. |
| `AddMetadata(key, value)` | Adds or replaces metadata on the current context. If none exists, it creates a context and makes it current. The key must not be null or empty. |

Always dispose scopes, normally with `using`, so nested work does not leak its context into the caller:

```csharp
using TenantIsolation.Utilities;

var requestContext = new TracingContext
{
    TenantId = tenantId,
    UserId = userId,
    RequestPath = "/orders"
};

using (DistributedTracingExtensions.BeginTracingScope(requestContext))
{
    var child = DistributedTracingExtensions.CreateChildContext("load-order");

    using (DistributedTracingExtensions.BeginTracingScope(child))
    {
        DistributedTracingExtensions.AddMetadata("order_id", orderId.ToString());
        await LoadOrderAsync(orderId);
    }
}
```

The child gets the parent's `TenantId`, so nested operations remain associated with the same tenant unless the caller explicitly changes it. Metadata is copied into a new dictionary: adding a key to the child does not add it to the parent's dictionary.

## Tenant context in HTTP traces

Registering tracing adds `IHttpContextAccessor`:

```csharp
builder.Services.AddDistributedTracing();
```

`UseDistributedTracing()` creates one `TracingContext` per request. It reads:

- `RequestPath` from `HttpRequest.Path`.
- `TenantId` from `HttpContext.Items["TenantId"]` when that value is a `Guid`.
- `UserId` from `HttpContext.Items["UserId"]` when that value is a `string`.
- `TraceId` from `X-Trace-Id`, when supplied; otherwise it retains the generated ID.
- `CorrelationId` from `X-Correlation-Id`, when supplied; otherwise it retains the generated ID.

The middleware places that context in a tracing scope for downstream components and writes `X-Trace-Id` and `X-Correlation-Id` response headers. Incoming header values are accepted as strings without format validation.

Middleware order determines whether the tenant is captured. A component that resolves the tenant must populate `HttpContext.Items["TenantId"]` **before** `UseDistributedTracing()` executes:

```csharp
app.UseTenantResolution();
app.UseDistributedTracing();
```

If tracing runs first, its request context is created before tenant resolution and `TenantId` remains `null`; later changes to `HttpContext.Items` are not synchronized into the already-created `TracingContext`. This is relevant to `UseTenantIsolationPhase2Middleware()`: its current built-in ordering installs distributed tracing before `TenantResolutionMiddleware`, so request tracing from that combined extension does not automatically capture the tenant resolved later in the pipeline. Code can explicitly set `GetCurrentContext()!.TenantId` after resolution when changing middleware order is not possible.

The middleware reads the exact `"TenantId"` item and requires its runtime value to be `Guid`. A string GUID is not converted. Tenant resolution middleware in this library stores a `Guid` under that key on successful resolution, including cache hits.

## Logging helpers

`LogWithTracing<T>` prefixes the message template with the current correlation ID:

```csharp
logger.LogWithTracing(
    LogLevel.Information,
    "Processing order {OrderId}",
    orderId);
```

With a current context, the resulting template begins with `[CorrelationId: ...]`. Without one, the original template is logged unchanged. This helper does not add tenant, trace, span, user, or metadata values to the log entry.

`GetTracingLogState()` provides a separate structured dictionary containing:

| Key | Value |
| --- | --- |
| `CorrelationId` | Current correlation ID |
| `TraceId` | Current trace ID |
| `SpanId` | Current span ID |
| `TenantId` | Tenant GUID as a string, or `"unknown"` |
| `UserId` | User ID, or `"anonymous"` |

It returns an empty dictionary when no context is current. The dictionary does not include `ParentSpanId`, `RequestPath`, `StartTime`, or entries from `Metadata`. Consumers must attach the returned values to their logging scope or telemetry themselves; calling the method does not emit a log or span.

```csharp
using (logger.BeginScope(DistributedTracingExtensions.GetTracingLogState()))
{
    logger.LogInformation("Loading tenant configuration");
}
```

In this example, a logging provider that supports scopes can record `TenantId` with the message.

## Timed operations

`ExecuteWithTracingAsync<T>` creates and scopes a child context, invokes the supplied asynchronous operation once, and returns its result:

```csharp
var order = await DistributedTracingExtensions.ExecuteWithTracingAsync(
    "load-order",
    () => repository.GetAsync(orderId),
    logger);
```

The helper:

1. Copies the parent context, including `TenantId`, into a child context.
2. Adds `operation` and an ISO 8601 UTC `operation_start` metadata value.
3. Logs operation start and completion at `Information`, including operation name and correlation ID.
4. Stores elapsed milliseconds in `operation_duration_ms` on successful completion.
5. Logs failures at `Error` with elapsed milliseconds and rethrows the original exception.
6. Restores the previous context when the operation completes or throws.

Its built-in start, completion, and failure messages include the correlation ID but not the tenant ID. Use a logging scope created from `GetTracingLogState()`, or log the tenant explicitly, when the tenant must be present in those records. The child still carries the inherited tenant context for code executing inside the operation.

## Limitations and safety notes

- The tracing context is correlation metadata, not an authorization boundary. Never use `TenantId` from a trace to grant data access; use the tenant-resolution and isolation services.
- `X-Trace-Id` and `X-Correlation-Id` are caller-controlled and are not validated by this middleware.
- No tenant ID is read directly from headers, claims, routes, or services. It must already be in `HttpContext.Items["TenantId"]`, or be assigned to the context explicitly.
- Context propagation is in-process. Cross-service propagation is limited to accepting and returning the two custom headers; outbound clients must forward them deliberately.
- `TracingContext.Metadata` is diagnostic data. `GetTracingLogState()` does not export it automatically.
