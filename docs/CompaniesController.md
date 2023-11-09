# CompaniesController

The `CompaniesController` is an ASP.NET Core API controller responsible for managing tenant‑isolated company (organization) resources. It exposes endpoints to list, retrieve, create, update, and delete organizations, as well as to manage per‑company settings and check feature availability. The controller enforces multi‑tenant boundaries, ensuring that all operations are scoped to the current tenant context.

## API

### Constructor

`public CompaniesController()`

Initializes a new instance of the controller. In a typical dependency‑injection setup, the constructor would accept services such as a tenant context provider, a repository, and a settings manager; the exact parameters depend on the application’s composition root.

### Methods

#### `public async Task<ActionResult<IEnumerable<Organization>>> ListCompanies()`

Returns a list of all organizations visible to the current tenant.

- **Returns**: `ActionResult<IEnumerable<Organization>>` – a collection of `Organization` objects.
- **Throws**: `UnauthorizedResult` if the tenant context is invalid or missing.

#### `public async Task<ActionResult<Organization>> GetCompany(Guid id)`

Retrieves a single organization by its unique identifier.

- **Parameters**:
  - `id` (`Guid`) – The identifier of the organization.
- **Returns**: `ActionResult<Organization>` – the matching `Organization` object.
- **Throws**: `NotFoundResult` if no organization with the given `id` exists within the tenant scope.

#### `public async Task<ActionResult<Organization>> CreateCompany(Organization organization)`

Creates a new organization under the current tenant.

- **Parameters**:
  - `organization` (`Organization`) – The organization data to create.
- **Returns**: `ActionResult<Organization>` – the created `Organization` with its assigned `Id` and `CreatedAt` timestamp.
- **Throws**: `BadRequestResult` if the provided `organization` is null or fails validation.

#### `public async Task<IActionResult> UpdateCompany(Guid id, Organization organization)`

Updates an existing organization.

- **Parameters**:
  - `id` (`Guid`) – The identifier of the organization to update.
  - `organization` (`Organization`) – The updated organization data.
- **Returns**: `IActionResult` – `NoContentResult` on success.
- **Throws**: `NotFoundResult` if the organization does not exist; `BadRequestResult` if the `id` does not match the `organization.Id` or if validation fails.

#### `public async Task<IActionResult> DeleteCompany(Guid id)`

Deletes an organization.

- **Parameters**:
  - `id` (`Guid`) – The identifier of the organization to delete.
- **Returns**: `IActionResult` – `NoContentResult` on success.
- **Throws**: `NotFoundResult` if the organization does not exist.

#### `public async Task<ActionResult<object>> GetCompanySettings(Guid id)`

Retrieves the settings associated with a specific organization.

- **Parameters**:
  - `id` (`Guid`) – The identifier of the organization.
- **Returns**: `ActionResult<object>` – a dynamic object containing the settings.
- **Throws**: `NotFoundResult` if the organization is not found.

#### `public async Task<IActionResult> UpdateCompanySettings(Guid id, object settings)`

Updates the settings for a specific organization.

- **Parameters**:
  - `id` (`Guid`) – The identifier of the organization.
  - `settings` (`object`) – The new settings data.
- **Returns**: `IActionResult` – `NoContentResult` on success.
- **Throws**: `NotFoundResult` if the organization does not exist; `BadRequestResult` if the settings payload is invalid.

#### `public async Task<ActionResult<bool>> IsFeatureAvailable(Guid id, string featureName)`

Checks whether a given feature is enabled for a specific organization.

- **Parameters**:
  - `id` (`Guid`) – The identifier of the organization.
  - `featureName` (`string`) – The name of the feature to check.
- **Returns**: `ActionResult<bool>` – `true` if the feature is available; otherwise `false`.
- **Throws**: `NotFoundResult` if the organization is not found.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Gets or sets the name of the current company context (if applicable). |
| `Slug` | `string` | Gets or sets the URL‑friendly slug for the current company. |
| `Description` | `string` | Gets or sets a description of the current company. |
| `Id` | `Guid` | Gets the unique identifier of the current company context. |
| `CreatedAt` | `DateTime` | Gets the creation timestamp of the current company context. |

> **Note:** The properties `Name`, `Slug`, and `Description` appear multiple times in the member list, likely reflecting both controller‑level context properties and model properties. The table above consolidates the unique property definitions.

## Usage

### Example 1: Creating and retrieving a company

```csharp
// Assume controller is injected via DI
var controller = new CompaniesController();

// Create a new organization
var newOrg = new Organization
{
    Name = "Acme Corp",
    Slug = "acme-corp",
    Description = "A sample organization"
};

var createResult = await controller.CreateCompany(newOrg);
if (createResult.Result is CreatedAtActionResult created)
{
    var createdOrg = created.Value as Organization;
    Console.WriteLine($"Created organization with ID: {createdOrg.Id}");

    // Retrieve the created organization
    var getResult = await controller.GetCompany(createdOrg.Id);
    if (getResult.Value is Organization retrieved)
    {
        Console.WriteLine($"Retrieved: {retrieved.Name}");
    }
}
```

### Example 2: Updating company settings and checking feature availability

```csharp
var controller = new CompaniesController();
Guid companyId = Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301");

// Update settings
var newSettings = new { Theme = "dark", MaxUsers = 50 };
var updateSettingsResult = await controller.UpdateCompanySettings(companyId, newSettings);
if (updateSettingsResult is NoContentResult)
{
    Console.WriteLine("Settings updated successfully.");
}

// Check if a feature is available
var featureResult = await controller.IsFeatureAvailable(companyId, "PremiumReports");
if (featureResult.Value)
{
    Console.WriteLine("PremiumReports feature is enabled.");
}
```

## Notes

- **Edge Cases**:  
  - All methods that accept an `id` parameter return `NotFoundResult` if the organization does not exist within the tenant’s scope.  
  - `CreateCompany` returns `BadRequestResult` when the provided `Organization` is `null` or fails validation (e.g., missing required fields).  
  - `UpdateCompany` validates that the route `id` matches the `organization.Id`; a mismatch yields `BadRequestResult`.  
  - `IsFeatureAvailable` returns `false` for unknown feature names; it does not throw for invalid feature names.

- **Thread Safety**:  
  Instances of `CompaniesController` are typically scoped per request in ASP.NET Core. The controller itself is not designed for concurrent access from multiple threads. All mutable properties (`Name`, `Slug`, `Description`) are intended for single‑request context and should not be shared across threads. The underlying data access layer (e.g., repositories) must handle concurrent database operations appropriately, but the controller does not provide additional synchronization.

- **Tenant Isolation**:  
  The controller assumes that a tenant context has been established before any method is called (e.g., via middleware or a scoped service). Operations are automatically filtered to the current tenant; attempting to access an organization belonging to a different tenant results in a `NotFoundResult` rather than exposing data across tenants.
