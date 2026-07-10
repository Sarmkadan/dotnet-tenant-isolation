# TenantIsolationException
The `TenantIsolationException` is a custom exception type designed to handle errors related to tenant isolation in a multi-tenant application. It provides a way to encapsulate error information, including an error code and additional error details, allowing for more informative error handling and logging.

## API
* `public string? ErrorCode`: Gets the error code associated with the exception, if any.
* `public Dictionary<string, object?>? ErrorDetails`: Gets additional error details, if any.
* `public TenantIsolationException()`: Initializes a new instance of the `TenantIsolationException` class with a default error message.
* `public TenantIsolationException(string message)`: Initializes a new instance of the `TenantIsolationException` class with a specified error message.
* `public TenantIsolationException(string message, string errorCode)`: Initializes a new instance of the `TenantIsolationException` class with a specified error message and error code.
* `public override string ToString()`: Returns a string representation of the exception, including the error message, error code, and error details.
* `public Guid TenantId`: Gets the ID of the tenant associated with the exception, if any.
* `public string? EntityType`: Gets the type of entity associated with the exception, if any.

## Usage
The following examples demonstrate how to use the `TenantIsolationException` class:
```csharp
try
{
    // Attempt to access a tenant's data
    var tenantData = GetTenantData(tenantId);
}
catch (TenantIsolationException ex)
{
    // Log the exception with error code and details
    LogException(ex, ex.ErrorCode, ex.ErrorDetails);
}

try
{
    // Attempt to configure a tenant's settings
    ConfigureTenantSettings(tenantId, settings);
}
catch (TenantIsolationException ex)
{
    // Handle the exception based on the error code and entity type
    if (ex.ErrorCode == "CONFIGURATION_ERROR" && ex.EntityType == "TENANT_SETTINGS")
    {
        // Handle configuration error for tenant settings
    }
    else
    {
        // Handle other errors
    }
}
```

## Notes
When using the `TenantIsolationException` class, consider the following:
* Error codes and details are optional, so be prepared to handle cases where they are not provided.
* The `TenantId` and `EntityType` properties are only populated when the exception is related to a specific tenant or entity.
* The `ToString()` method returns a string representation of the exception, which can be useful for logging and debugging purposes.
* The `TenantIsolationException` class is designed to be thread-safe, as it only contains immutable properties and does not rely on any external state. However, when using the class in a multi-threaded environment, ensure that any logging or error handling mechanisms are also thread-safe to avoid concurrency issues.
