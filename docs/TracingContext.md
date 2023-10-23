# TracingContext

The `TracingContext` type provides a structured mechanism for capturing and propagating distributed tracing information across asynchronous operations within a single application process. It holds correlation, trace, and span identifiers along with contextual data such as the current tenant ID, user ID, request path, and a mutable metadata dictionary. Static methods allow retrieval, creation, and scoping of the context, making it suitable for use in middleware, background jobs, and any code that needs to correlate log entries or telemetry with a specific request or operation.

## API

### Instance Properties

- **`CorrelationId`** (`string`)  
  Gets or sets the correlation identifier, typically used to group related operations across different services or components.

- **`TraceId`** (`string`)  
  Gets or sets the trace identifier, representing the root of a distributed trace.

- **`SpanId`** (`string`)  
  Gets or sets the span identifier, representing a single unit of work within a trace.

- **`ParentSpanId`** (`string?`)  
  Gets or sets the identifier of the parent span. Can be `null` for root spans.

- **`RequestPath`** (`string?`)  
  Gets or sets the HTTP request path or logical operation path associated with this context. Can be `null`.

- **`TenantId`** (`Guid?`)  
  Gets or sets the tenant identifier. Can be `null` when no tenant is known.

- **`UserId`** (`string?`)  
  Gets or sets the user identifier. Can be `null` when no user is authenticated.

- **`StartTime`** (`DateTime`)  
  Gets or sets the timestamp when this context was created or when the associated operation began.

- **`Metadata`** (`Dictionary<string, string>`)  
  Gets or sets a mutable dictionary of arbitrary key-value pairs for additional contextual data. The dictionary is not thread-safe; external synchronization is required if accessed concurrently.

### Static Methods

- **`GetCurrentContext`** (`static TracingContext?`)  
  Returns the current `TracingContext` for the executing asynchronous flow, or `null` if no context has been set. The context is stored in an `AsyncLocal` and flows with `await` but does not cross thread boundaries unless explicitly propagated.

- **`SetCurrentContext`** (`static void`)  
  Sets the current `TracingContext` for the executing asynchronous flow. The parameter is a `TracingContext?` instance; passing `null` clears the current context. This method does not dispose the previous context.

- **`GetOrCreateContext`** (`static TracingContext`)  
  Returns the current `TracingContext` if one exists; otherwise creates a new context with a new correlation ID, trace ID, span ID, and the current UTC time, sets it as the current context, and returns it.

- **`CreateChildContext`** (`static TracingContext`)  
  Creates a new `TracingContext` that inherits the `CorrelationId`, `TraceId`, `RequestPath`, `TenantId`, `UserId`, and `Metadata` from the current context (if any). The new context receives a new `SpanId` and sets the current context’s `SpanId` as its `ParentSpanId`. If no current context exists, a new root context is created. The new context is not automatically set as the current context.

- **`BeginTracingScope`** (`static IDisposable`)  
  Creates a child context via `CreateChildContext`, sets it as the current context, and returns a `TracingScope` (which implements `IDisposable`). When the returned scope is disposed, the previous context is restored. This method is typically used with a `using` statement to scope a unit of work.

- **`AddMetadata`** (`static void`)  
  Adds a key-value pair to the `Metadata` dictionary of the current context. If no current context exists, the call is ignored. The key and value are strings. Duplicate keys are overwritten.

- **`LogWithTracing<T>`** (`static void`)  
  Invokes a logging delegate of type `Action<T>` with the provided state, after enriching the logging state with the current tracing context’s properties. The enrichment is performed by calling `GetTracingLogState` and merging it into the log scope or log context (implementation‑specific). The generic parameter `T` represents the type of the state object passed to the delegate.

- **`ExecuteWithTracingAsync<T>`** (`static async Task<T>`)  
  Executes an asynchronous function (`Func<Task<T>>`) within the current tracing context. The context is captured before execution and restored after completion, even if the function throws. Returns the result of the function.

- **`GetTracingLogState`** (`static Dictionary<string, object>`)  
  Returns a dictionary containing the current tracing context’s properties (`CorrelationId`, `TraceId`, `SpanId`, `ParentSpanId`, `RequestPath`, `TenantId`, `UserId`, `StartTime`) and all entries from the `Metadata` dictionary. If no current context exists, an empty dictionary is returned.

### Nested Type: `TracingScope`

- **`TracingScope`** (class, implements `IDisposable`)  
  Returned by `BeginTracingScope`. Upon creation it stores the previous current context and sets the new child context as current. When `Dispose` is called, it restores the previous context. The scope does not dispose the child context; the child context remains available for later use.

### Instance Method

- **`Dispose`** (`void`)  
  Releases resources held by this `TracingContext` instance. The default implementation clears the `Metadata` dictionary and sets all string properties to `string.Empty` and nullable properties to `null`. This method does not affect the static current context.

## Usage

### Example 1: Middleware that creates a tracing scope per request

```csharp
public class TracingMiddleware
{
    private readonly RequestDelegate _next;

    public TracingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext httpContext)
    {
        using (TracingContext.BeginTracingScope())
        {
            var ctx = TracingContext.GetCurrentContext();
            ctx.RequestPath = httpContext.Request.Path;
            ctx.TenantId = httpContext.Request.Headers["X-Tenant-Id"] switch
            {
                string val when Guid.TryParse(val, out var guid) => guid,
                _ => null
            };
            ctx.UserId = httpContext.User?.Identity?.Name;

            // Log the incoming request with tracing info
            TracingContext.LogWithTracing<string>(log => 
                Console.WriteLine($"[{log}] Incoming request"), "RequestStart");

            await _next(httpContext);
        }
    }
}
```

### Example 2: Executing a background operation with a child context

```csharp
public async Task ProcessOrderAsync(Order order)
{
    // Create a child context that inherits the current correlation and trace IDs
    using var scope = TracingContext.BeginTracingScope();
    var childCtx = TracingContext.GetCurrentContext();
    childCtx.AddMetadata("orderId", order.Id.ToString());

    // Execute a traced operation
    var result = await TracingContext.ExecuteWithTracingAsync(async () =>
    {
        // Simulate work
        await Task.Delay(100);
        return "Processed";
    });

    // Log the result with tracing state
    TracingContext.LogWithTracing<string>(msg => Console.WriteLine(msg), result);
}
```

## Notes

- **Thread safety**: The static current context is stored in an `AsyncLocal<T>`, which provides per‑asynchronous‑flow isolation. This means the context flows correctly with `await` but is not automatically shared across threads (e.g., when using `Task.Run` or `Parallel.ForEach`). Explicit propagation is required in such scenarios.
- **Concurrent access to Metadata**: The `Metadata` dictionary is not thread‑safe. If multiple threads or tasks modify the same `TracingContext` instance concurrently, external synchronization (e.g., a lock) must be used.
- **Null handling**: `GetCurrentContext` returns `null` when no context has been set. `SetCurrentContext(null)` clears the current context. `AddMetadata` silently ignores the call if no current context exists. `CreateChildContext` and `BeginTracingScope` always produce a valid context even when no parent exists.
- **Dispose behavior**: Calling `Dispose` on a `TracingContext` instance resets its own properties but does not affect the static current context or any `TracingScope` that may reference it. The `TracingScope` returned by `BeginTracingScope` restores the previous context on disposal, not the disposed instance.
- **Scope nesting**: `BeginTracingScope` can be nested. Each scope saves the previous context and restores it upon disposal, allowing correct restoration of the context stack.
- **Performance**: Creating a new `TracingContext` allocates a new dictionary for `Metadata`. Consider reusing contexts or clearing metadata when appropriate in high‑throughput scenarios.
