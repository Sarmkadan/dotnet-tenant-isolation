# AnalyticsControllerValidation
The `AnalyticsControllerValidation` type is used to validate and store analytics data for a tenant in a multi-tenant system. It provides a comprehensive overview of the tenant's analytics, including response times, data processed, storage used, and request statistics.

## API
The `AnalyticsControllerValidation` type has the following public members:
* `Status`: A string indicating the current status of the analytics validation.
* `Timestamp`: A `DateTime` object representing the timestamp of the analytics validation.
* `Components`: A `Dictionary<string, ComponentHealth>` containing the health of various components.
* `Name`: A string representing the name of the analytics validation.
* `TenantId`: A `Guid` representing the ID of the tenant.
* `ActiveUsers`: An integer representing the number of active users.
* `RequestsPerHour`: An integer representing the number of requests per hour.
* `DataProcessedGb`: A decimal representing the amount of data processed in gigabytes.
* `StorageUsedGb`: A decimal representing the amount of storage used in gigabytes.
* `LastActivityAt`: A `DateTime` object representing the last activity timestamp.
* `Period`: A string representing the period of the analytics validation.
* `TotalRequests`: A long integer representing the total number of requests.
* `SuccessfulRequests`: A long integer representing the number of successful requests.
* `FailedRequests`: A long integer representing the number of failed requests.
* `AverageResponseTimeMs`: An integer representing the average response time in milliseconds.
* `P95ResponseTimeMs`: An integer representing the 95th percentile response time in milliseconds.
* `P99ResponseTimeMs`: An integer representing the 99th percentile response time in milliseconds.
* `ResponseTimeMs`: An integer representing the response time in milliseconds.

## Usage
Here are two examples of using the `AnalyticsControllerValidation` type:
```csharp
// Example 1: Creating a new AnalyticsControllerValidation object
var analyticsValidation = new AnalyticsControllerValidation
{
    Status = "Valid",
    Timestamp = DateTime.UtcNow,
    Components = new Dictionary<string, ComponentHealth>
    {
        {"Component1", ComponentHealth.Healthy},
        {"Component2", ComponentHealth.Unhealthy}
    },
    Name = "Tenant1",
    TenantId = Guid.NewGuid(),
    ActiveUsers = 100,
    RequestsPerHour = 1000,
    DataProcessedGb = 10.5m,
    StorageUsedGb = 5.2m,
    LastActivityAt = DateTime.UtcNow.AddHours(-1),
    Period = "Hourly",
    TotalRequests = 10000,
    SuccessfulRequests = 9000,
    FailedRequests = 1000,
    AverageResponseTimeMs = 50,
    P95ResponseTimeMs = 100,
    P99ResponseTimeMs = 200,
    ResponseTimeMs = 50
};

// Example 2: Updating an existing AnalyticsControllerValidation object
var existingAnalyticsValidation = new AnalyticsControllerValidation
{
    Status = "Valid",
    Timestamp = DateTime.UtcNow,
    Components = new Dictionary<string, ComponentHealth>
    {
        {"Component1", ComponentHealth.Healthy},
        {"Component2", ComponentHealth.Unhealthy}
    },
    Name = "Tenant1",
    TenantId = Guid.NewGuid(),
    ActiveUsers = 100,
    RequestsPerHour = 1000,
    DataProcessedGb = 10.5m,
    StorageUsedGb = 5.2m,
    LastActivityAt = DateTime.UtcNow.AddHours(-1),
    Period = "Hourly",
    TotalRequests = 10000,
    SuccessfulRequests = 9000,
    FailedRequests = 1000,
    AverageResponseTimeMs = 50,
    P95ResponseTimeMs = 100,
    P99ResponseTimeMs = 200,
    ResponseTimeMs = 50
};

existingAnalyticsValidation.ActiveUsers += 10;
existingAnalyticsValidation.RequestsPerHour += 100;
existingAnalyticsValidation.DataProcessedGb += 1.0m;
existingAnalyticsValidation.StorageUsedGb += 0.5m;
```

## Notes
When using the `AnalyticsControllerValidation` type, note that the `Components` dictionary can contain multiple components with different health statuses. Additionally, the `Period` property can be used to determine the time period over which the analytics data is aggregated. The `ResponseTimeMs` property represents the response time in milliseconds, while the `AverageResponseTimeMs`, `P95ResponseTimeMs`, and `P99ResponseTimeMs` properties represent the average, 95th percentile, and 99th percentile response times, respectively. The `AnalyticsControllerValidation` type is not thread-safe, and concurrent access to its properties can result in inconsistent data. Therefore, it is recommended to use synchronization mechanisms, such as locks or concurrent collections, when accessing or modifying the properties of the `AnalyticsControllerValidation` type in a multi-threaded environment.
