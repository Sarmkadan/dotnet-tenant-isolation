# ServiceRegistrationExtensions

Provides extension methods for registering tenant-isolation phase 2 services into the dependency injection container and configuring the middleware pipeline. This type bridges service composition and application bootstrapping, enabling tenant-aware caching and logging scopes that respect tenant boundaries.

## API

### AddTenantIsolationPhase2Services (IServiceCollection)

```csharp
public static IServiceCollection AddTenantIsolationPhase2Services(this IServiceCollection services)
```

Registers all services required for phase 2 of the tenant isolation feature into the provided service collection. This includes tenant-aware cache providers, logging infrastructure, and any supporting dependencies. Returns the same `IServiceCollection` instance to support fluent chaining.

**Parameters**
- `services` — The `IServiceCollection` to which phase 2 services will be added. Must not be null.

**Return Value**
The modified `IServiceCollection` instance.

**Exceptions**
- `ArgumentNullException` — Thrown when `services` is null.

---

### AddTenantIsolationPhase2Services (IServiceCollection, Action<TenantIsolationOptions>)

```csharp
public static IServiceCollection AddTenantIsolationPhase2Services(this IServiceCollection services, Action<TenantIsolationOptions> configure)
```

Registers phase 2 tenant isolation services and applies additional configuration through a delegate. The configuration delegate is invoked against a `TenantIsolationOptions` instance, allowing customization of tenant boundaries, cache policies, or logging behaviour.

**Parameters**
- `services` — The `IServiceCollection` to populate. Must not be null.
- `configure` — A delegate that receives `TenantIsolationOptions` for customisation. May be null, in which case defaults apply.

**Return Value**
The modified `IServiceCollection` instance.

**Exceptions**
- `ArgumentNullException` — Thrown when `services` is null.

---

### AddTenantAwareCacheProvider

```csharp
public static IServiceCollection AddTenantAwareCacheProvider(this IServiceCollection services)
```

Registers a cache provider that isolates cached data per tenant. This ensures that cached entries from one tenant are not inadvertently served to another. Returns the service collection for fluent configuration.

**Parameters**
- `services` — The `IServiceCollection` to which the tenant-aware cache provider is added. Must not be null.

**Return Value**
The modified `IServiceCollection` instance.

**Exceptions**
- `ArgumentNullException` — Thrown when `services` is null.

---

### UseTenantIsolationPhase2Middleware

```csharp
public static IApplicationBuilder UseTenantIsolationPhase2Middleware(this IApplicationBuilder app)
```

Adds phase 2 tenant isolation middleware to the application pipeline. This middleware typically establishes the tenant context for the current request, ensuring downstream components operate within the correct tenant scope. Returns the `IApplicationBuilder` for fluent pipeline construction.

**Parameters**
- `app` — The `IApplicationBuilder` instance. Must not be null.

**Return Value**
The modified `IApplicationBuilder` instance.

**Exceptions**
- `ArgumentNullException` — Thrown when `app` is null.

---

### LogPhase2ServicesOnStartup

```csharp
public static IApplicationBuilder LogPhase2ServicesOnStartup(this IApplicationBuilder app)
```

Emits diagnostic log entries during application startup describing the registered phase 2 services and their configuration state. This aids in verifying that tenant isolation components are correctly wired. Returns the `IApplicationBuilder` for fluent chaining.

**Parameters**
- `app` — The `IApplicationBuilder` instance. Must not be null.

**Return Value**
The modified `IApplicationBuilder` instance.

**Exceptions**
- `ArgumentNullException` — Thrown when `app` is null.

---

### Log\<TState\>

```csharp
public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
```

Writes a log entry with the specified level, event identifier, state object, optional exception, and a formatter function that produces the log message. This method is part of the `ILogger` interface implementation used internally by the tenant isolation logging infrastructure.

**Parameters**
- `logLevel` — The severity of the log entry.
- `eventId` — A unique identifier for the event being logged.
- `state` — The state object carrying contextual data for the log entry.
- `exception` — An optional exception associated with the log entry.
- `formatter` — A function that converts the state and exception into a human-readable message.

**Return Value**
None.

**Exceptions**
- `ArgumentNullException` — Thrown when `formatter` is null.

---

### IsEnabled

```csharp
public bool IsEnabled(LogLevel logLevel)
```

Determines whether logging is active for the given `LogLevel`. Returns `true` if the logger is configured to accept entries at the specified level; otherwise `false`.

**Parameters**
- `logLevel` — The log severity level to check.

**Return Value**
`true` if the logger is enabled for the specified level; `false` otherwise.

**Exceptions**
None.

---

### BeginScope\<TState\>

```csharp
public IDisposable? BeginScope<TState>(TState state)
```

Creates a logical logging scope that attaches the provided state to all log entries emitted within its lifetime. The scope is tenant-aware when used within the tenant isolation infrastructure, ensuring log correlation remains bounded to the current tenant context. Returns an `IDisposable` that, when disposed, ends the scope.

**Parameters**
- `state` — The state object to associate with the scope.

**Return Value**
An `IDisposable` instance representing the scope, or `null` if scopes are not supported by the underlying logger.

**Exceptions**
None.

## Usage

### Example 1: Basic Registration and Middleware Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register phase 2 services with default options
builder.Services.AddTenantIsolationPhase2Services();

// Optionally add tenant-aware caching separately
builder.Services.AddTenantAwareCacheProvider();

var app = builder.Build();

// Insert phase 2 middleware into the pipeline
app.UseTenantIsolationPhase2Middleware();

// Log registered services during startup for diagnostics
app.LogPhase2ServicesOnStartup();

app.Run();
```

### Example 2: Customised Registration with Options

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTenantIsolationPhase2Services(options =>
{
    options.TenantHeaderName = "X-Custom-Tenant-Id";
    options.DefaultTenantId = "default-tenant";
    options.CacheSlidingExpiration = TimeSpan.FromMinutes(15);
});

builder.Services.AddTenantAwareCacheProvider();

var app = builder.Build();

app.UseTenantIsolationPhase2Middleware();
app.LogPhase2ServicesOnStartup();

app.MapGet("/data", async (HttpContext context, ITenantAwareCache cache) =>
{
    var tenantId = context.Items["TenantId"]?.ToString() ?? "unknown";
    var cachedValue = await cache.GetOrCreateAsync(tenantId, "key", () => Task.FromResult("value"));
    return Results.Ok(new { tenantId, cachedValue });
});

app.Run();
```

## Notes

- **Registration order**: `AddTenantIsolationPhase2Services` must be called before `AddTenantAwareCacheProvider` if both are used, as the latter may depend on infrastructure registered by the former. Reversing the order can lead to missing service exceptions at runtime.
- **Middleware placement**: `UseTenantIsolationPhase2Middleware` should be placed early in the pipeline, before any middleware that requires tenant context (e.g., authorisation, multi-tenant data access). Placing it too late may result in downstream components operating without a resolved tenant.
- **Startup logging**: `LogPhase2ServicesOnStartup` emits logs at application start. In environments with high restart frequency, consider disabling it to avoid log noise. It is intended for development and troubleshooting scenarios.
- **Thread safety**: The extension methods that configure `IServiceCollection` and `IApplicationBuilder` are designed for single-threaded startup paths and are not thread-safe for concurrent modification. The `Log`, `IsEnabled`, and `BeginScope` members are implemented on logger instances that follow the standard `ILogger` thread-safety guarantees — they are safe for concurrent use across multiple request threads.
- **Scope disposal**: When using `BeginScope<TState>`, always dispose the returned `IDisposable` to prevent state leaking across requests. In the tenant isolation infrastructure, scopes are typically managed automatically by the middleware, but manual usage requires explicit disposal, preferably via a `using` statement.
- **Null configuration delegate**: Passing `null` for the `configure` parameter in the overload of `AddTenantIsolationPhase2Services` is safe and results in default options being applied. No customisation occurs, and no exception is thrown.
