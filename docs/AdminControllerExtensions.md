# AdminControllerExtensions
The `AdminControllerExtensions` class provides a set of extension methods for administering tenants in a multi-tenancy environment. These methods enable bulk operations, such as suspending and activating tenants, as well as retrieving tenants based on their status. Additionally, it allows for enqueuing a cleanup task. These extensions are designed to simplify the management of tenants and improve the overall efficiency of administrative tasks.

## API
* `BulkSuspendTenants`: Suspends multiple tenants in bulk. This method is asynchronous and returns an `ActionResult` containing an `ApiResponse` with an `object` payload. It does not specify any parameters, but it is expected to throw exceptions if the operation fails due to invalid input or internal errors.
* `BulkActivateTenants`: Activates multiple tenants in bulk. Similar to `BulkSuspendTenants`, this method is asynchronous and returns an `ActionResult` containing an `ApiResponse` with an `object` payload. It also does not specify any parameters and is expected to throw exceptions on failure.
* `GetTenantsByStatus`: Retrieves a paginated list of tenants based on their status. This method is asynchronous and returns an `ActionResult` containing an `ApiResponse` with a `PaginatedResponse` of `object` payload. The parameters for this method are not specified, but it is expected to throw exceptions if the operation fails due to invalid input or internal errors.
* `EnqueueCleanupTask`: Enqueues a cleanup task. This method is synchronous and returns an `ActionResult` containing an `ApiResponse` with an `object` payload. It does not specify any parameters, but it may throw exceptions if the operation fails due to internal errors.

## Usage
The following examples demonstrate how to use the `AdminControllerExtensions` methods:
```csharp
// Example 1: Bulk suspending tenants
var result = await AdminControllerExtensions.BulkSuspendTenants();
if (result.Value.Success)
{
    Console.WriteLine("Tenants suspended successfully.");
}
else
{
    Console.WriteLine("Error suspending tenants: " + result.Value.ErrorMessage);
}

// Example 2: Retrieving active tenants
var activeTenants = await AdminControllerExtensions.GetTenantsByStatus("active");
if (activeTenants.Value.Success)
{
    foreach (var tenant in activeTenants.Value.Data)
    {
        Console.WriteLine("Active Tenant: " + tenant);
    }
}
else
{
    Console.WriteLine("Error retrieving active tenants: " + activeTenants.Value.ErrorMessage);
}
```

## Notes
When using the `AdminControllerExtensions` methods, consider the following:
- These methods are designed for administrative tasks and should be used with caution, as they can impact the state of tenants and the overall system.
- The `BulkSuspendTenants` and `BulkActivateTenants` methods do not specify parameters, but it is essential to ensure that the correct tenants are targeted to avoid unintended consequences.
- The `GetTenantsByStatus` method returns a paginated response, which means that large result sets will be split into multiple pages. This should be considered when processing the results.
- The `EnqueueCleanupTask` method is synchronous, but it may still throw exceptions if the operation fails. It is crucial to handle these exceptions properly to avoid system instability.
- These methods are thread-safe, as they are designed to handle concurrent requests. However, it is still important to ensure that the calling code is properly synchronized to avoid any potential issues.
