# ComponentHealthInfo

The `ComponentHealthInfo` type serves as the primary data model and utility class within the `dotnet-tenant-isolation` project for representing and executing health checks on isolated tenant components. It encapsulates the current status, diagnostic messages, and performance metrics of a specific component while providing recursive support for nested sub-components. Beyond its role as a data carrier, the class includes static and instance methods to register health check services, perform asynchronous validations, and retrieve cached reports, facilitating a comprehensive monitoring strategy for multi-tenant environments.

## API

### Properties

*   **`public string Name`**
    Gets the unique identifier or display name of the component being monitored.

*   **`public HealthStatus Status`**
    Gets the current health state of the component (e.g., Healthy, Degraded, Unhealthy). Note: This property appears twice in the definition but represents a single state value.

*   **`public string Message`**
    Gets a human-readable description of the current health state, often containing error details if the status is not healthy.

*   **`public long ResponseTimeMs`**
    Gets the duration of the last health check execution in milliseconds.

*   **`public DateTime CheckedAt`**
    Gets the timestamp indicating when the last health check was performed. Note: This property appears twice in the definition but represents a single point in time.

*   **`public Dictionary<string, ComponentHealthInfo> Components`**
    Gets a collection of nested sub-components associated with this component, allowing for hierarchical health reporting. The key is the sub-component name, and the value is its corresponding `ComponentHealthInfo` instance.

*   **`public TimeSpan TotalCheckDuration`**
    Gets the aggregate time taken to check this component and all its nested sub-components.

*   **`public string GetMessage`**
    Gets a delegate or property accessor intended to retrieve a dynamic or formatted message string. (Note: Based on the signature provided, this acts as a string accessor).

*   **`public HealthCheckService HealthCheckService`**
    Gets the instance of the `HealthCheckService` associated with this component, used to orchestrate check executions.

### Methods

*   **`public async Task<HealthReport> PerformHealthCheckAsync()`**
    Executes a full health check routine for the component and all its nested dependencies.
    *   **Return Value**: A `HealthReport` object containing the aggregated results of the check.
    *   **Exceptions**: May throw exceptions if the underlying health check logic fails critically or if the service is not properly initialized.

*   **`public async Task<ComponentHealthInfo> CheckComponentAsync()`**
    Performs a health check specifically for this component instance, updating its internal state (`Status`, `Message`, `ResponseTimeMs`, etc.).
    *   **Return Value**: Returns the updated `ComponentHealthInfo` instance reflecting the latest check results.
    *   **Exceptions**: May throw if the specific component check logic encounters an unhandled error.

*   **`public HealthReport? GetCachedHealthReport()`**
    Retrieves the most recently generated health report from the internal cache, if available.
    *   **Return Value**: A `HealthReport` object if a cached report exists; otherwise, `null`.
    *   **Exceptions**: Generally does not throw unless the cache state is corrupted.

### Static Methods

*   **`public static IServiceCollection AddHealthCheckService(this IServiceCollection services)`**
    Registers the `HealthCheckService` and related dependencies into the dependency injection container.
    *   **Parameters**: `services` - The `IServiceCollection` to add services to.
    *   **Return Value**: The modified `IServiceCollection` to allow for method chaining.
    *   **Exceptions**: Throws `ArgumentNullException` if `services` is null.

## Usage

### Example 1: Registering and Performing a Recursive Health Check

This example demonstrates how to register the health check service in the application startup and subsequently perform an asynchronous check that evaluates both the root component and its nested dependencies.

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotnetTenantIsolation; // Hypothetical namespace based on project name

// 1. Register the service in the DI container
var services = new ServiceCollection();
services.AddHealthCheckService();
var serviceProvider = services.BuildServiceProvider();

// 2. Resolve a component (assuming retrieval logic exists or is injected)
// In a real scenario, this might be resolved via a factory or injected directly
var rootComponent = new ComponentHealthInfo 
{ 
    Name = "DatabaseCluster", 
    // Initialization of nested components would occur here
};

// 3. Perform the asynchronous health check
try 
{
    HealthReport report = await rootComponent.PerformHealthCheckAsync();
    
    Console.WriteLine($"Overall Status: {report.Status}");
    Console.WriteLine($"Total Duration: {rootComponent.TotalCheckDuration}");
    
    // Inspect nested components
    foreach (var subComponent in rootComponent.Components)
    {
        Console.WriteLine($"Sub-component '{subComponent.Key}': {subComponent.Value.Status}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Health check failed: {ex.Message}");
}
```

### Example 2: Utilizing Cached Reports and Individual Component Checks

This example illustrates checking a specific component in isolation and attempting to retrieve a cached report to reduce overhead during high-frequency polling.

```csharp
using System;
using System.Threading.Tasks;

public async Task MonitorComponentAsync(ComponentHealthInfo component)
{
    // Attempt to retrieve a cached report first
    var cachedReport = component.GetCachedHealthReport();
    
    if (cachedReport != null && (DateTime.UtcNow - cachedReport.GeneratedAt).TotalSeconds < 5)
    {
        Console.WriteLine("Using cached health report.");
        return;
    }

    // If cache is stale or missing, perform a fresh check on the specific component
    try 
    {
        ComponentHealthInfo updatedInfo = await component.CheckComponentAsync();
        
        Console.WriteLine($"Component: {updatedInfo.Name}");
        Console.WriteLine($"Status: {updatedInfo.Status}");
        Console.WriteLine($"Message: {updatedInfo.Message}");
        Console.WriteLine($"Response Time: {updatedInfo.ResponseTimeMs}ms");
        Console.WriteLine($"Checked At: {updatedInfo.CheckedAt}");
    }
    catch (Exception ex)
    {
        // Log the specific failure for this component
        Console.WriteLine($"Failed to check component {component.Name}: {ex.Message}");
    }
}
```

## Notes

*   **Thread Safety**: The presence of mutable properties such as `Status`, `Message`, and `CheckedAt`, combined with asynchronous update methods (`CheckComponentAsync`), implies that `ComponentHealthInfo` instances are not inherently thread-safe for concurrent writes. External synchronization or immutable patterns should be employed if multiple threads may trigger updates on the same instance simultaneously.
*   **Recursive Depth**: The `Components` dictionary allows for arbitrary nesting. Consumers of `PerformHealthCheckAsync` should be aware of potential stack overflow risks or excessive latency if the component hierarchy becomes excessively deep or if a circular reference is inadvertently introduced in the `Components` graph.
*   **Caching Strategy**: The `GetCachedHealthReport` method returns a nullable type, indicating that the cache is not pre-warmed. Callers must handle the `null` case gracefully by falling back to an explicit check execution. The expiration policy for the cache is not exposed via this API and is likely managed internally by the `HealthCheckService`.
*   **Duplicate Signatures**: The API definition lists `Status` and `CheckedAt` multiple times. In implementation, these represent single storage locations; however, consumers should rely on the latest value set by the most recent asynchronous operation.
*   **Dependency Injection**: The `AddHealthCheckService` extension method must be called during application composition (e.g., in `Program.cs` or `Startup.cs`) before any component attempting to resolve `HealthCheckService` is instantiated.
