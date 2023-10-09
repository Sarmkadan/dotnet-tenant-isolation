# RequestContextMiddlewareExtensions
The `RequestContextMiddlewareExtensions` type provides a set of extension methods for working with request context in ASP.NET Core applications, specifically designed for multi-tenancy scenarios. It allows developers to add request context middleware, access tenant information, and configure diagnostics and correlation IDs. This enables more efficient and organized handling of requests in multi-tenant environments.

## API
* `public static IApplicationBuilder UseRequestContextWithDiagnostics`: Adds middleware to the application pipeline that enables diagnostics for request context. It returns the `IApplicationBuilder` instance, allowing for method chaining. This method does not throw exceptions under normal circumstances.
* `public static RequestContext? GetRequestContext`: Retrieves the current request context. It returns a `RequestContext` object if available, otherwise `null`. This method does not throw exceptions.
* `public static IServiceCollection AddRequestContext`: Adds services required for request context to the specified `IServiceCollection`. It returns the `IServiceCollection` instance for further configuration. This method does not throw exceptions under normal circumstances.
* `public static bool HasTenantContext`: Checks if a tenant context is available for the current request. It returns `true` if a tenant context exists, otherwise `false`. This property does not throw exceptions.
* `public static string? GetTenantId`: Retrieves the ID of the current tenant. It returns the tenant ID as a string if available, otherwise `null`. This property does not throw exceptions.
* `public static string GetCorrelationId`: Retrieves the correlation ID for the current request. It returns the correlation ID as a string. This property does not throw exceptions.
* `public bool EnableRequestTiming`: Gets or sets a value indicating whether request timing is enabled. This property does not throw exceptions.
* `public bool IncludeTenantInHeaders`: Gets or sets a value indicating whether the tenant ID should be included in headers. This property does not throw exceptions.
* `public bool IncludeUserInHeaders`: Gets or sets a value indicating whether user information should be included in headers. This property does not throw exceptions.
* `public bool EnableCorrelationId`: Gets or sets a value indicating whether correlation IDs are enabled. This property does not throw exceptions.
* `public bool EnableTenantFromHeader`: Gets or sets a value indicating whether tenants can be identified from headers. This property does not throw exceptions.
* `public bool EnableTenantFromRoute`: Gets or sets a value indicating whether tenants can be identified from route data. This property does not throw exceptions.
* `public bool EnableTenantFromSubdomain`: Gets or sets a value indicating whether tenants can be identified from subdomains. This property does not throw exceptions.

## Usage
The following examples demonstrate how to use the `RequestContextMiddlewareExtensions` type in a real-world scenario:
```csharp
// Example 1: Adding request context middleware
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRequestContextWithDiagnostics();
    // Further configuration...
}

// Example 2: Accessing tenant information
public IActionResult MyAction()
{
    var tenantId = RequestContextMiddlewareExtensions.GetTenantId;
    if (tenantId != null)
    {
        // Handle tenant-specific logic
    }
    return View();
}
```

## Notes
When using the `RequestContextMiddlewareExtensions` type, consider the following edge cases and thread-safety remarks:
* The `GetRequestContext` and `GetTenantId` methods may return `null` if no request context or tenant ID is available, respectively. Developers should handle these cases accordingly.
* The `EnableRequestTiming`, `IncludeTenantInHeaders`, `IncludeUserInHeaders`, `EnableCorrelationId`, `EnableTenantFromHeader`, `EnableTenantFromRoute`, and `EnableTenantFromSubdomain` properties are thread-safe, as they are simple getters and setters. However, the underlying configuration may not be thread-safe, depending on the implementation.
* The `AddRequestContext` and `UseRequestContextWithDiagnostics` methods are designed to be used during application startup and configuration. They should not be called multiple times or from different threads, as this may lead to unexpected behavior.
