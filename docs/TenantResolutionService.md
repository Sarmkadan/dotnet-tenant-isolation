# TenantResolutionService

The `TenantResolutionService` is responsible for determining the current tenant based on the execution context (e.g., an HTTP request) and making that tenant information available to the rest of the application. It encapsulates the logic for tenant identification, caching the resolved tenant for the lifetime of the current scope, and providing simple query methods to check whether a tenant is present and to retrieve its details.

## API

### TenantResolutionService()

Initializes a new instance of the `TenantResolutionService`. The constructor does not require any parameters and prepares the service to resolve tenants when `ResolveTenantAsync` is invoked. No state is stored until a resolution attempt is made.

### ResolveTenantAsync()

```csharp
public async Task<Tenant> ResolveTenantAsync()
```

**Purpose:** Attempts to identify the tenant for the current context (e.g., by examining headers, subdomain, or claims) and stores the result internally.

**Parameters:** None.

**Return Value:** A `Task<Tenant>` that completes with the resolved tenant object.

**Exceptions:**  
- `InvalidOperationException` if the service is unable to determine a tenant due to missing or malformed context information.  
- `TenantResolutionException` (or a derived type) if the resolution process encounters an unexpected error.

### GetCurrentTenant()

```csharp
public Tenant? GetCurrentTenant()
```

**Purpose:** Retrieves the tenant that was most recently resolved by `ResolveTenantAsync` for the current scope.

**Parameters:** None.

**Return Value:** The resolved `Tenant` instance, or `null` if no tenant has been resolved yet or if resolution failed.

**Exceptions:** None.

### GetCurrentTenantId()

```csharp
public Guid? GetCurrentTenantId()
```

**Purpose:** Retrieves the identifier of the currently resolved tenant.

**Parameters:** None.

**Return Value:** The `Guid` representing the tenant’s ID, or `null` if no tenant is currently resolved.

**Exceptions:** None.

### HasTenant

```csharp
public bool HasTenant
```

**Purpose:** Indicates whether a tenant has been successfully resolved and is available for the current scope.

**Parameters:** None.

**Return Value:** `true` if a tenant is present; otherwise `false`.

**Exceptions:** None.

## Usage

### Example 1: ASP.NET Core Middleware

```csharp
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenantResolutionService _tenantService;

    public TenantMiddleware(RequestDelegate next, TenantResolutionService tenantService)
    {
        _next = next;
        _tenantService = tenantService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Resolve the tenant for this request.
        await _tenantService.ResolveTenantAsync()...
        if (_tenantService.HasTenant)
        {
            var tenant = _tenantService.GetCurrentTenant();
            // Use tenant information, e.g., to select a database connection.
            context.Items["Tenant"] = tenant;
        }

        await _next(context);
    }
}
```

### Example 2: Service Method Consuming Tenant Data

```csharp
public class OrderService
{
    private readonly TenantResolutionService _tenantService;

    public OrderService(TenantResolutionService tenantService)
    {
        _tenantService = tenantService;
    }

    public async Task<OrderDto> GetOrderAsync(Guid orderId)
    {
        // Ensure a tenant is resolved before accessing tenant‑scoped data.
        if (!_tenantService.HasTenant)
        {
            throw new InvalidOperationException("No tenant context available.");
        }

        var tenantId = _tenantService.GetCurrentTenantId()
                     ?? throw new InvalidOperationException("Tenant ID missing.");

        // Use tenantId to query the appropriate data store.
        var order = await _orderRepository.GetByTenantAndIdAsync(tenantId, orderId);
        return _mapper.Map<OrderDto>(order);
    }
}
```

## Notes

- The service is designed to be used within a scoped lifetime (e.g., per HTTP request). Calling `ResolveTenantAsync` multiple times within the same scope will overwrite the previously cached tenant with the result of the latest call.
- If `ResolveTenantAsync` throws, no tenant is cached; subsequent calls to `GetCurrentTenant`, `GetCurrentTenantId`, or checking `HasTenant` will return `null`/`false` until a successful resolution occurs.
- The members `GetCurrentTenant`, `GetCurrentTenantId`, and `HasTenant` are thread‑safe for read‑only access after resolution, but the service itself is not thread‑safe for concurrent calls to `ResolveTenantAsync`. Concurrent invocations may result in race conditions where the final cached tenant reflects whichever task completes last.
- Consumers should not rely on the service retaining tenant information beyond the scope in which `ResolveTenantAsync` was called; creating a new scope (or a new instance) is required for each distinct context that needs tenant resolution.
