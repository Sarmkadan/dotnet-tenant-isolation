# Tenant
The `Tenant` type represents a single tenant in a multi-tenant system, encapsulating its core properties and status. It serves as a fundamental entity for managing and isolating tenant-specific data and configurations, providing a structured approach to handling tenant-related information.

## API
The `Tenant` type exposes the following public members:
* `Id`: A unique identifier for the tenant, represented as a `Guid`.
* `Slug`: A unique slug for the tenant, represented as a `string`.
* `Name`: The name of the tenant, represented as a `string`.
* `Description`: An optional description of the tenant, represented as a `string?`.
* `AdminEmail`: The email address of the tenant administrator, represented as a `string`.
* `PhoneNumber`: An optional phone number for the tenant, represented as a `string?`.
* `Status`: The current status of the tenant, represented as a `TenantStatus`.
* `IsolationStrategy`: The isolation strategy employed by the tenant, represented as a `TenantIsolationStrategy`.
* `PlanId`: An optional identifier for the tenant's plan, represented as a `string?`.
* `MaxUsers`: An optional maximum number of users allowed for the tenant, represented as an `int?`.
* `MaxStorageGb`: An optional maximum storage capacity allowed for the tenant, represented as a `decimal?`.
* `CreatedAt`: The date and time when the tenant was created, represented as a `DateTime`.
* `UpdatedAt`: The date and time when the tenant was last updated, represented as a `DateTime`.
* `SubscriptionExpiresAt`: An optional date and time when the tenant's subscription expires, represented as a `DateTime?`.
* `Metadata`: An optional metadata associated with the tenant, represented as a `string?`.
* `IsDeleted`: A flag indicating whether the tenant is deleted, represented as a `bool`.
* `DeletedAt`: An optional date and time when the tenant was deleted, represented as a `DateTime?`.
* `CanActivate`: A flag indicating whether the tenant can be activated, represented as a `bool`.
* `IsUserLimitExceeded`: A flag indicating whether the tenant has exceeded its user limit, represented as a `bool`.
* `IsSubscriptionValid`: A flag indicating whether the tenant's subscription is valid, represented as a `bool`.

## Usage
The following examples demonstrate how to create and utilize `Tenant` instances:
```csharp
// Create a new tenant
var tenant = new Tenant
{
    Id = Guid.NewGuid(),
    Slug = "example-tenant",
    Name = "Example Tenant",
    AdminEmail = "admin@example.com",
    Status = TenantStatus.Active,
    IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant
};

// Access and manipulate tenant properties
Console.WriteLine(tenant.Name); // Output: Example Tenant
tenant.MaxUsers = 100;
Console.WriteLine(tenant.MaxUsers); // Output: 100
```

## Notes
When working with `Tenant` instances, consider the following edge cases and thread-safety remarks:
* The `Id` property is immutable and should be initialized during object creation.
* The `Status` and `IsolationStrategy` properties are critical for determining the tenant's behavior and should be carefully managed.
* The `MaxUsers` and `MaxStorageGb` properties are optional and may be null; handle these cases accordingly.
* The `SubscriptionExpiresAt` property is optional and may be null; consider this when implementing subscription-related logic.
* The `IsDeleted` and `DeletedAt` properties are used to track the tenant's deletion status; ensure proper handling of these flags.
* The `CanActivate`, `IsUserLimitExceeded`, and `IsSubscriptionValid` properties provide important flags for tenant management; use these to inform your application's logic.
* The `Tenant` type is designed to be thread-safe, but it is still essential to follow standard concurrency guidelines when accessing and manipulating `Tenant` instances in a multi-threaded environment.
