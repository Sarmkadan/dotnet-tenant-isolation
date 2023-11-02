# TenantApiController

The `TenantApiController` serves as the primary HTTP interface for managing multi-tenant lifecycle operations within the `dotnet-tenant-isolation` project. It exposes endpoints for provisioning, querying, activating, suspending, and deleting tenant records, while also providing administrative utilities for searching tenants and retrieving system-wide statistics. This controller adheres to standard ASP.NET Core conventions, returning `IActionResult` responses to indicate success, failure, or specific HTTP status codes based on the outcome of the underlying business logic.

## API

### Constructors

#### `public TenantApiController()`
Initializes a new instance of the `TenantApiController` class. This constructor is typically invoked by the dependency injection container to resolve required services before handling requests.

### Action Methods

#### `public async Task<IActionResult> CreateTenant()`
Provisions a new tenant record in the system.
*   **Purpose**: Registers a new tenant using provided configuration details.
*   **Parameters**: Accepts tenant configuration via the request body (typically bound to properties like `Name`, `Slug`, `AdminEmail`).
*   **Return Value**: Returns `OkObjectResult` containing the created tenant data on success, or `BadRequestObjectResult`/`ConflictObjectResult` if validation fails or the slug already exists.
*   **Exceptions**: May throw exceptions if the database connection fails or if unique constraint violations occur during persistence.

#### `public async Task<IActionResult> GetTenantById()`
Retrieves a specific tenant using its unique identifier.
*   **Purpose**: Fetches detailed information for a single tenant.
*   **Parameters**: Requires a tenant ID (usually passed as a route parameter).
*   **Return Value**: Returns `OkObjectResult` with the tenant data if found, or `NotFoundResult` if the ID does not exist.
*   **Exceptions**: Throws if the provided ID format is invalid or if data retrieval encounters a critical storage error.

#### `public async Task<IActionResult> GetTenantBySlug()`
Retrieves a specific tenant using its human-readable slug.
*   **Purpose**: Allows lookup of tenant resources via their URL-friendly identifier.
*   **Parameters**: Requires a slug string (usually passed as a route or query parameter).
*   **Return Value**: Returns `OkObjectResult` with the tenant data if found, or `NotFoundResult` if the slug is unassigned.
*   **Exceptions**: Throws if the slug format is invalid or if the underlying query fails.

#### `public async Task<IActionResult> GetActiveTenants()`
Lists all tenants currently in an active state.
*   **Purpose**: Provides a collection of tenants that are operational and not suspended.
*   **Parameters**: No specific parameters required; may support pagination via query string (implementation dependent).
*   **Return Value**: Returns `OkObjectResult` containing a list of active tenants. Returns an empty list if no active tenants exist.
*   **Exceptions**: Throws if the data store is unavailable.

#### `public async Task<IActionResult> ActivateTenant()`
Changes the status of a suspended tenant to active.
*   **Purpose**: Restores access and functionality for a previously suspended tenant.
*   **Parameters**: Requires the target tenant identifier.
*   **Return Value**: Returns `OkObjectResult` or `NoContentResult` upon successful activation. Returns `NotFoundResult` if the tenant does not exist or `BadRequestObjectResult` if the tenant is already active.
*   **Exceptions**: Throws if the state transition logic fails or if concurrency conflicts occur.

#### `public async Task<IActionResult> SuspendTenant()`
Temporarily disables a tenant.
*   **Purpose**: Revokes access for a tenant without deleting their data, often used for billing issues or policy violations.
*   **Parameters**: Requires the target tenant identifier and optionally a `Reason`.
*   **Return Value**: Returns `OkObjectResult` or `NoContentResult` upon successful suspension. Returns `NotFoundResult` if the tenant does not exist.
*   **Exceptions**: Throws if the tenant is already suspended or if the update operation fails.

#### `public async Task<IActionResult> DeleteTenant()`
Permanently removes a tenant and associated data.
*   **Purpose**: Executes a hard delete or soft delete (depending on configuration) of the tenant record.
*   **Parameters**: Requires the target tenant identifier.
*   **Return Value**: Returns `NoContentResult` on success, or `NotFoundResult` if the tenant is missing.
*   **Exceptions**: Throws if foreign key constraints prevent deletion or if the cleanup process encounters an error.

#### `public IActionResult GetCurrentTenant()`
Retrieves the context of the tenant associated with the current request.
*   **Purpose**: Identifies which tenant scope the current user or API client is operating within.
*   **Parameters**: Relies on current HTTP context (headers, claims, or domain mapping).
*   **Return Value**: Returns `OkObjectResult` with the current tenant details, or `UnauthorizedResult`/`NotFoundResult` if the tenant context cannot be resolved.
*   **Exceptions**: Throws if the tenant resolution middleware has not populated the context correctly.

#### `public async Task<IActionResult> GetStatistics()`
Aggregates and returns high-level metrics regarding tenant usage.
*   **Purpose**: Provides administrative insights such as total tenant count, active vs. suspended ratios, or resource consumption.
*   **Parameters**: No parameters required.
*   **Return Value**: Returns `OkObjectResult` containing statistical data objects.
*   **Exceptions**: Throws if the aggregation query times out or fails.

#### `public async Task<IActionResult> SearchTenants()`
Performs a filtered search across tenant records.
*   **Purpose**: Allows administrators to find tenants based on partial matches of name, slug, or email.
*   **Parameters**: Accepts search criteria via query parameters.
*   **Return Value**: Returns `OkObjectResult` with a collection of matching tenants.
*   **Exceptions**: Throws if the search query is malformed or the index is unavailable.

### Properties

#### `public string Name`
Gets or sets the display name of the tenant. This property is typically used during the creation or update of a tenant record to define the human-readable label.

#### `public string Slug`
Gets or sets the unique URL-friendly identifier for the tenant. This value must be unique across the system and is often used in routing and subdomain resolution.

#### `public string AdminEmail`
Gets or sets the email address of the primary administrator for the tenant. This is used for notification delivery and account recovery processes.

#### `public string? Reason`
Gets or sets an optional explanation for state changes, specifically utilized when suspending a tenant or rejecting an activation request. This property is nullable.

## Usage

### Example 1: Creating a New Tenant
The following example demonstrates how to instantiate the controller dependencies (simulated) and invoke the `CreateTenant` method to provision a new customer environment.

```csharp
// Assuming dependency injection has resolved the controller instance
var controller = new TenantApiController();

// In a real scenario, these properties would be bound from an HTTP POST body
// For demonstration, we assume the controller exposes a model binding mechanism 
// or these are set via a dedicated DTO passed to the action (implicit in signature).
// Note: Direct property setting on the controller itself is atypical for actions 
// unless the controller acts as a model holder in this specific architecture.
// The standard pattern implies passing a DTO to the action method.

var newTenantRequest = new 
{
    Name = "Acme Corporation",
    Slug = "acme-corp",
    AdminEmail = "admin@acme.com"
};

// Simulating the call; in actual ASP.NET Core, this is triggered via HTTP POST
// var result = await controller.CreateTenant(newTenantRequest); 

if (result is OkObjectResult okResult)
{
    Console.WriteLine($"Tenant created successfully: {okResult.Value}");
}
else if (result is BadRequestObjectResult badRequest)
{
    Console.WriteLine($"Creation failed: {badRequest.Value}");
}
```

### Example 2: Suspending a Tenant with a Reason
This example illustrates the workflow for an administrator to suspend a non-compliant tenant, providing a mandatory reason for the audit log.

```csharp
var controller = new TenantApiController();
string targetTenantId = "t_9876543210";
string suspensionReason = "Violation of Terms of Service: Section 4.2";

// The Reason property might be utilized internally by the action if bound to the model
// or passed as a parameter depending on the specific action signature implementation.
// Assuming the action accepts a model containing the Reason property.

var suspendRequest = new 
{
    Id = targetTenantId,
    Reason = suspensionReason
};

// var result = await controller.SuspendTenant(suspendRequest);

if (result is NoContentResult)
{
    Console.WriteLine($"Tenant {targetTenantId} suspended successfully.");
}
else if (result is NotFoundResult)
{
    Console.WriteLine($"Tenant {targetTenantId} not found.");
}
```

## Notes

*   **Thread Safety**: As with standard ASP.NET Core controllers, `TenantApiController` is instantiated per request. Therefore, instance members (properties like `Name`, `Slug`, `Reason`) are not shared across concurrent requests, ensuring inherent thread safety for request-specific data. However, any static members or singleton services injected into the controller must be thread-safe.
*   **Concurrency Conflicts**: Operations such as `ActivateTenant`, `SuspendTenant`, and `DeleteTenant` are susceptible to race conditions if multiple administrators attempt to modify the same tenant state simultaneously. Implementations should utilize optimistic concurrency control (e.g., ETags or row versions) to prevent data loss, potentially returning `409 Conflict` in such scenarios.
*   **Nullable Reason**: The `Reason` property is defined as nullable (`string?`). Consumers must ensure that when suspending a tenant, a reason is provided if business policies require it, as the API may accept a null value but downstream logic might enforce validation.
*   **Slug Uniqueness**: The `Slug` property acts as a natural key. Attempts to `CreateTenant` with a duplicate slug will result in a conflict error. Slugs should be normalized (lowercase, hyphenated) before submission to ensure consistency.
*   **Asynchronous Execution**: All data-modifying and retrieval methods are asynchronous (`async Task`). Callers must await these operations to prevent thread pool starvation and to correctly handle exceptions propagated from the data layer.
