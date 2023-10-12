# OrganizationRepository

The `OrganizationRepository` encapsulates data‑access logic for `Organization` entities in the multi‑tenant isolation system. It provides asynchronous methods to query, filter, and modify organization records while abstracting the underlying data store.

## API

### OrganizationRepository
- **Purpose**: Represents the repository class; instantiated via dependency injection (typically with a scoped `DbContext`).
- **Parameters**: None (constructor dependencies are defined elsewhere).
- **Return Value**: An instance ready to perform organization‑related operations.
- **Throws**: May throw `InvalidOperationException` if required services (e.g., database context) are not supplied.

### GetBySlugAsync
```csharp
public async Task<Organization?> GetBySlugAsync(string slug)
```
- **Purpose**: Retrieves a single organization whose slug matches the supplied value.
- **Parameters**: 
  - `slug` – The unique slug string to match.
- **Return Value**: The matching `Organization` or `null` if none exists.
- **Throws**: 
  - `ArgumentNullException` if `slug` is `null`.
  - `InvalidOperationException` if a database error occurs.

### GetActiveOrganizationsAsync
```csharp
public async Task<List<Organization>> GetActiveOrganizationsAsync()
```
- **Purpose**: Returns all organizations that are currently marked as active.
- **Parameters**: None.
- **Return Value**: A list of `Organization` instances; empty list if no active organizations.
- **Throws**: 
  - `InvalidOperationException` on data‑access failure.

### GetWithUsersAsync
```csharp
public async Task<Organization?> GetWithUsersAsync(string identifier)
```
- **Purpose**: Loads an organization together with its related user entities.
- **Parameters**: 
  - `identifier` – The slug or ID used to locate the organization.
- **Return Value**: The organization with its `Users` collection populated, or `null` if not found.
- **Throws**: 
  - `ArgumentNullException` if `identifier` is `null`.
  - `InvalidOperationException` for query execution problems.

### GetByIndustryAsync
```csharp
public async Task<List<Organization>> GetByIndustryAsync(string industry)
```
- **Purpose**: Finds organizations operating in the specified industry.
- **Parameters**: 
  - `industry` – The industry name to filter by.
- **Return Value**: List of matching organizations; empty if none.
- **Throws**: 
  - `ArgumentNullException` if `industry` is `null`.
  - `InvalidOperationException` on query error.

### GetByCountryAsync
```csharp
public async Task<List<Organization>> GetByCountryAsync(string country)
```
- **Purpose**: Retrieves organizations located in the given country.
- **Parameters**: 
  - `country` – The country name or code.
- **Return Value**: List of organizations; empty list if no matches.
- **Throws**: 
  - `ArgumentNullException` if `country` is `null`.
  - `InvalidOperationException` for data‑access issues.

### SearchAsync
```csharp
public async Task<List<Organization>> SearchAsync(string term, int? page = null, int? pageSize = null)
```
- **Purpose**: Performs a free‑text search across organization fields (e.g., name, slug, description).
- **Parameters**: 
  - `term` – Search string.
  - `page` – Optional zero‑based page number for pagination.
  - `pageSize` – Optional number of results per page.
- **Return Value**: List of organizations matching the term; respects pagination if supplied.
- **Throws**: 
  - `ArgumentNullException` if `term` is `null`.
  - `InvalidOperationException` on execution failure.

### GetOrganizationCountAsync
```csharp
public async Task<int> GetOrganizationCountAsync()
```
- **Purpose**: Returns the total number of organizations in the store.
- **Parameters**: None.
- **Return Value**: Integer count of all organizations.
- **Throws**: 
  - `InvalidOperationException` if the count cannot be retrieved.

### IsSlugUniqueAsync
```csharp
public async Task<bool> IsSlugUniqueAsync(string slug, int? organizationId = null)
```
- **Purpose**: Checks whether a slug is not already used by another organization, optionally ignoring a specific organization (useful during updates).
- **Parameters**: 
  - `slug` – Slug to test.
  - `organizationId` – Optional ID of the organization to exclude from the check.
- **Return Value**: `true` if the slug is unique; `false` otherwise.
- **Throws**: 
  - `ArgumentNullException` if `slug` is `null`.
  - `InvalidOperationException` on query error.

### GetOrganizationsWithUserCountAsync
```csharp
public async Task<List<dynamic>> GetOrganizationsWithUserCountAsync()
```
- **Purpose**: Projects each organization together with a count of its associated users.
- **Parameters**: None.
- **Return Value**: List of anonymous objects, each containing organization properties and a `UserCount` field.
- **Throws**: 
  - `InvalidOperationException` if the projection fails.

### GetByRegistrationNumberAsync
```csharp
public async Task<Organization?> GetByRegistrationNumberAsync(string registrationNumber)
```
- **Purpose**: Finds an organization by its official registration number.
- **Parameters**: 
  - `registrationNumber` – The registration number to match.
- **Return Value**: Matching organization or `null`.
- **Throws**: 
  - `ArgumentNullException` if `registrationNumber` is `null`.
  - `InvalidOperationException` on data‑access error.

### DeactivateAsync
```csharp
public async Task<bool> DeactivateAsync(int organizationId)
```
- **Purpose**: Marks the specified organization as inactive.
- **Parameters**: 
  - `organizationId` – ID of the organization to deactivate.
- **Return Value**: `true` if the operation succeeded; `false` if the organization was not found or already inactive.
- **Throws**: 
  - `InvalidOperationException` if the update cannot be persisted.

### GetStatisticsAsync
```csharp
public async Task<object> GetStatisticsAsync()
```
- **Purpose**: Retrieves a set of aggregate statistics about the organization collection (e.g., totals, active/inactive counts, industry distribution).
- **Parameters**: None.
- **Return Value**: An anonymous object containing statistical properties; shape defined by the implementation.
- **Throws**: 
  - `InvalidOperationException` if the statistics query fails.

### BulkActivateAsync
```csharp
public async Task<int> BulkActivateAsync(IEnumerable<int> organizationIds)
```
- **Purpose**: Activates multiple organizations in a single operation.
- **Parameters**: 
  - `organizationIds` – Collection of organization IDs to activate.
- **Return Value**: Number of organizations successfully activated.
- **Throws**: 
  - `ArgumentNullException`ArgumentNullException` if `organizationIds` is `null`.
  - `InvalidOperationException` on bulk update failure.

### GetRecentAsync
```csharp
public async Task<List<Organization>> GetRecentAsync(int count)
```
- **Purpose**: Returns the most recently created organizations, limited to the specified count.
- **Parameters**: 
  - `count` – Maximum number of recent organizations to return.
- **Return Value**: List of organizations ordered by creation date descending; may contain fewer than `count` items if insufficient data.
- **Throws**: 
  - `ArgumentOutOfRangeException` if `count` is less than zero.
  - `InvalidOperationException` on query error.

## Usage

### Example 1: Retrieve an organization by slug and load its users
```csharp
public async Task<IActionResult> Details(string slug)
{
    var org = await _organizationRepository.GetBySlugAsync(slug);
    if (org == null)
        return NotFound();

    // Load users associated with the organization
    org = await _organizationRepository.GetWithUsersAsync(slug);
    return View(org);
}
```

### Example 2: Search for organizations and obtain overall statistics
```csharp
public async Task<IActionResult> Index(string searchTerm, int page = 0, int pageSize = 20)
{
    var organizations = await _organizationRepository.SearchAsync(searchTerm, page, pageSize);
    var stats = await _organizationRepository.GetStatisticsAsync();

    var model = new OrganizationIndexViewModel
    {
        Organizations = organizations,
        Statistics = stats
    };

    return View(model);
}
```

## Notes

- **Slug uniqueness race**: `IsSlugUniqueAsync` only reflects the state at the moment of execution. Concurrent inserts may cause a duplicate slug despite a prior unique check; enforce uniqueness with a database unique constraint and handle potential update exceptions.
- **Return types**: Methods returning `Task<List<dynamic>>` or `Task<object>` produce anonymous shapes; consumers should rely on known property names or use reflection/dynamic access with caution.
- **Transaction scope**: Operations that modify state (`DeactivateAsync`, `BulkActivateAsync`) are executed within the repository’s current `DbContext` scope. For atomicity across multiple repository calls, wrap them in a transaction scope or use a strategy that shares the same context lifetime.
- **Thread‑safety**: The repository assumes a scoped lifetime (e.g., per‑request in ASP.NET Core). It is not safe for concurrent use across threads without external synchronization; each thread should obtain its own repository instance.
- **Null handling**: All methods that accept string or identifier arguments throw `ArgumentNullException` for null inputs; callers should validate or guard against null values before invocation.
- **Error propagation**: Data‑access failures surface as `InvalidOperationException` (or more specific EF Core exceptions). Callers may catch these to implement fallback logic or error reporting.
