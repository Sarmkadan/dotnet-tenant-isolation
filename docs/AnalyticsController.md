# AnalyticsController

AnalyticsController is an ASP.NET Core API controller that exposes endpoints for retrieving health, activity, usage, and error metrics related to a tenant within the dotnet-tenant-isolation solution. The controller does not maintain state; each request is processed independently using injected services.

## API

### GetHealth
```csharp
public ActionResult<ApiResponse<HealthStatus>> GetHealth()
```
**Purpose:** Returns the current health status of the tenant‑scoped service.  
**Parameters:** None.  
**Return Value:** An `ActionResult` wrapping an `ApiResponse<HealthStatus>`; on success the response contains a `HealthStatus` instance with details such as overall status, timestamp, and component health.  
**Throws:** May return a 500 status code if the underlying health check service throws an exception; otherwise returns appropriate 2xx or 4xx results based on validation.

### GetTenantActivity
```csharp
public ActionResult<ApiResponse<TenantActivityMetrics>> GetTenantActivity()
```
**Purpose:** Retrieves activity metrics for the current tenant (e.g., active users, request volume).  
**Parameters:** None.  
**Return Value:** An `ActionResult` wrapping an `ApiResponse<TenantActivityMetrics>`; on success the payload includes `ActiveUsers`, `RequestsPerHour`, `LastActivityAt`, and related fields.  
**Throws:** Returns 500 if the metrics service fails; validation errors produce 400 responses.

### GetUsageStatistics
```csharp
public ActionResult<ApiResponse<UsageStatistics>> GetUsageStatistics()
```
**Purpose:** Provides usage statistics such as data processed and storage consumed.  
**Parameters:** None.  
**Return Value:** An `ActionResult` wrapping an `ApiResponse<UsageStatistics>`; on success the payload contains `DataProcessedGb`, `StorageUsedGb`, `TotalRequests`, and `Period`.  
**Throws:** Propagates service failures as 500 responses; invalid state yields 400.

### GetErrorMetrics
```csharp
public ActionResult<ApiResponse<ErrorMetrics>> GetErrorMetrics()
```
**Purpose:** Returns error‑related metrics for the tenant (e.g., error counts, rates).  
**Parameters:** None.  
**Return Value:** An `ActionResult` wrapping an `ApiResponse<ErrorMetrics>`; on success the payload includes error counts and timing information.  
**Throws:** Returns 500 on internal service errors; otherwise standard HTTP status codes.

### Status (string)
**Purpose:** Indicates the overall health status (e.g., "Healthy", "Degraded", "Unhealthy").  
**Type:** `string` (read‑only).  

### Timestamp
**Purpose:** The point in time at which the status snapshot was taken.  
**Type:** `DateTime` (read‑only).  

### Components
**Purpose:** A mapping of component names to their individual health states.  
**Type:** `Dictionary<string, ComponentHealth>` (read‑only).  

### Name
**Purpose:** Identifier for the tenant or service instance being reported.  
**Type:** `string` (read‑only).  

### Status (duplicate string)
**Purpose:** Additional status field present in certain metric DTOs (e.g., operation status).  
**Type:** `string` (read‑only).  

### ResponseTimeMs
**Purpose:** Average response time measured in milliseconds for the reported interval.  
**Type:** `int` (read‑only).  

### TenantId
**Purpose:** Unique identifier of the tenant to which the metrics belong.  
**Type:** `Guid` (read‑only).  

### ActiveUsers
**Purpose:** Number of uniquely identified users active during the reporting period.  
**Type:** `int` (read‑only).  

### RequestsPerHour
**Purpose:** Request throughput averaged over an hour.  
**Type:** `int` (read‑only).  

### DataProcessedGb
**Purpose:** Volume of data processed, expressed in gigabytes.  
**Type:** `decimal` (read‑only).  

### StorageUsedGb
**Purpose:** Amount of storage currently consumed by the tenant, in gigabytes.  
**Type:** `decimal` (read‑only).  

### LastActivityAt
**Purpose:** Timestamp of the most recent recorded activity for the tenant.  
**Type:** `DateTime` (read‑only).  

### Period (duplicate string)
**Purpose:** Describes the reporting interval (e.g., "24h", "7d", "monthly").  
**Type:** `string` (read‑only).  

### TotalRequests
**Purpose:** Cumulative count of requests processed since the tenant’s inception or since the counter was last reset.  
**Type:** `long` (read‑only).  

## Usage

### Example 1: Calling the health endpoint with HttpClient
```csharp
using System.Net.Http.Json;
using System.Threading.Tasks;

public async Task<HealthStatus> CheckTenantHealthAsync(HttpClient client, string baseUrl)
{
    var response = await client.GetFromJsonAsync<ApiResponse<HealthStatus>>($"{baseUrl}/analytics/health");
    response?.EnsureSuccessStatusCode(); // throws if not successful
    return response?.Value ?? throw new InvalidOperationException("Health response missing payload");
}
```

### Example 2: Using the controller directly in a unit test
```csharp
using Microsoft.AspNetCore.Mvc;
using Xunit;

public class AnalyticsControllerTests
{
    [Fact]
    public void GetTenantActivity_ReturnsMetrics()
    {
        // Arrange
        var controller = new AnalyticsController(/* mocked dependencies */);

        // Act
        var result = controller.GetTenantActivity();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<TenantActivityMetrics>>(okResult.Value);
        Assert.NotNull(apiResponse.Value);
        Assert.InRange(apiResponse.Value.ActiveUsers, 0, int.MaxValue);
    }
}
```

## Notes
- The controller is stateless; all members that appear as properties are read‑only DTO fields returned by the action methods, not mutable state of the controller itself. Consequently, the controller is thread‑safe for concurrent request handling.
- Duplicate member names (`Status` and `Period`) arise from their presence in different metric DTOs (e.g., `HealthStatus` vs. `UsageStatistics`). They represent distinct logical concepts despite sharing a name and type.
- Consumers should guard against null `Value` properties in the `ApiResponse<T>` wrapper, as a non‑successful HTTP status may still produce a response body with a null payload.
- Metric values such as `ResponseTimeMs`, `ActiveUsers`, and `RequestsPerHour` are expected to be non‑negative; negative values indicate a service error and should be treated as invalid.
- The `TenantId` Guid is assumed to be supplied by middleware; supplying an empty Guid (`Guid.Empty`) will result in a 400 Bad Request from underlying services.
