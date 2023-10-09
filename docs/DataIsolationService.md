# DataIsolationService
The `DataIsolationService` class provides a set of methods for managing data isolation policies in a multi-tenant environment. It allows for the creation, retrieval, update, and deletion of policies, as well as checking for policy violations and exporting/importing policies. This service is designed to help ensure that sensitive data is properly isolated and protected across different tenants.

## API
### Constructors
* `public DataIsolationService`: Initializes a new instance of the `DataIsolationService` class.

### Methods
* `public async Task<DataIsolationPolicy> CreatePolicyAsync`: Creates a new data isolation policy. Returns the newly created policy.
* `public async Task<DataIsolationPolicy?> GetPolicyAsync`: Retrieves a data isolation policy. Returns the policy if found, or `null` if not found.
* `public async Task<bool> IsFieldAccessAllowedAsync`: Checks if access to a specific field is allowed. Returns `true` if access is allowed, `false` otherwise.
* `public async Task VerifyFieldAccessAsync`: Verifies if access to a specific field is allowed. Throws an exception if access is not allowed.
* `public async Task<bool> CanAccessCrossTenantAsync`: Checks if cross-tenant access is allowed. Returns `true` if access is allowed, `false` otherwise.
* `public async Task<DataIsolationPolicy> UpdatePolicyAsync`: Updates an existing data isolation policy. Returns the updated policy.
* `public async Task<bool> DeletePolicyAsync`: Deletes a data isolation policy. Returns `true` if the policy was deleted, `false` otherwise.
* `public async Task<List<DataIsolationPolicy>> GetActivePoliciesAsync`: Retrieves a list of active data isolation policies. Returns the list of policies.
* `public async Task<bool> SetPolicyActiveAsync`: Sets a data isolation policy as active. Returns `true` if the policy was set as active, `false` otherwise.
* `public async Task<bool> SetPolicyPriorityAsync`: Sets the priority of a data isolation policy. Returns `true` if the priority was set, `false` otherwise.
* `public async Task<List<string>> CheckPolicyViolationsAsync`: Checks for policy violations. Returns a list of violations.
* `public async Task<string> ExportPolicyAsync`: Exports a data isolation policy. Returns the exported policy as a string.
* `public async Task<DataIsolationPolicy> ImportPolicyAsync`: Imports a data isolation policy. Returns the imported policy.

## Usage
The following examples demonstrate how to use the `DataIsolationService` class:
```csharp
// Create a new policy
var service = new DataIsolationService();
var policy = await service.CreatePolicyAsync();

// Check if field access is allowed
var isAllowed = await service.IsFieldAccessAllowedAsync("fieldName");
if (isAllowed)
{
    Console.WriteLine("Field access is allowed");
}
else
{
    Console.WriteLine("Field access is not allowed");
}
```

## Notes
* The `DataIsolationService` class is designed to be thread-safe, allowing for concurrent access and modification of policies.
* When using the `VerifyFieldAccessAsync` method, be prepared to handle exceptions that may be thrown if access is not allowed.
* The `ExportPolicyAsync` and `ImportPolicyAsync` methods can be used to transfer policies between different environments or systems.
* The `CheckPolicyViolationsAsync` method can be used to identify and address potential security issues related to data isolation policies.
