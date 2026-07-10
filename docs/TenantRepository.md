# TenantRepository

The `TenantRepository` provides data‑access operations for the `Tenant` aggregate within the multi‑tenant isolation layer. It wraps an `ITenantDbContextFactory<TenantDbContext>` to create short‑lived `TenantDbContext` instances, ensuring that each query or command runs in its own scoped context and that EF Core change tracking does not leak between calls.

## API

### Constructor
```csharp
public TenantRepository(ITenantDbContextFactory<TenantDbContext> contextFactory) : base(contextFactory)
```
* **Purpose** – Initializes a new repository instance.  
* **Parameters** – `contextFactory`: Factory used to create `TenantDbContext` instances.  
* **Return** – New `TenantRepository` ready for use.  
* **Throws** – `ArgumentNullException` if `contextFactory` is `null`.

### GetBySlugAsync
```csharp
public async Task<Tenant?> GetBySlugAsync(string slug)
```
* **Purpose** – Retrieves a tenant by its unique URL‑friendly slug.  
* **Parameters** – `slug`: The slug to match; must not be `null` or whitespace.  
* **Return** – The matching `Tenant` or `null` if none exists.  
* **Throws** – `ArgumentException` if `slug` is `null`/empty; may propagate `DbUpdateException` or other EF Core exceptions from the underlying context.

### GetActiveTenantAsync
```csharp
public async Task<List<Tenant>> GetActiveTenantAsync()
```
* **Purpose** – Returns all tenants whose status is `Active`.  
* **Parameters** – None.  
* **Return** – List of active tenants (may be empty).  
* **Throws** – May propagate EF Core exceptions.

### GetByStatusAsync
```csharp
public async Task<List<Tenant>> GetByStatusAsync(TenantStatus status)
```
* **Purpose** – Retrieves tenants filtered by a specific status.  
* **Parameters** – `status`: The `TenantStatus` value to filter on.  
* **Return** – List of tenants matching the status (may be empty).  
* **Throws** – `ArgumentException` if an undefined enum value is supplied; may propagate EF Core exceptions.

### GetTrialTenantsAsync
```csharp
public async Task<List<Tenant>> GetTrialTenantsAsync()
```
* **Purpose** – Returns tenants that are currently in a trial period.  
* **Parameters** – None.  
* **Return** – List of trial tenants (may be empty).  
* **Throws** – May propagate EF Core exceptions.

### GetExpiringSubscriptionsAsync
```csharp
public async Task<List<Tenant>> GetExpiringSubscriptionsAsync()
```
* **Purpose** – Returns tenants whose subscription is approaching expiration (logic defined in the query).  
* **Parameters** – None.  
* **Return** – List of tenants with expiring subscriptions (may be empty).  
* **Throws** – May propagate EF Core exceptions.

### GetRecentlyCreatedAsync
```csharp
public async Task<List<Tenant>> GetRecentlyCreatedAsync(int count)
```
* **Purpose** – Returns the most recently created tenants, limited to `count`.  
* **Parameters** – `count`: Maximum number of tenants to return; must be greater than zero.  
* **Return** – List of the newest tenants (may be fewer than `count` if fewer exist).  
* **Throws** – `ArgumentOutOfRangeException` if `count` ≤ 0; may propagate EF Core exceptions.

### SearchAsync
```csharp
public async Task<List<Tenant>> SearchAsync(string searchTerm, int? page = null, int? pageSize = null)
```
* **Purpose** – Performs a free‑text search across tenant name, slug, and contact fields.  
* **Parameters** –  
  * `searchTerm`: Text to search for; `null` or whitespace returns all tenants.  
  * `page`: Zero‑based page index for pagination; if `null` returns all matching rows.  
  * `pageSize`: Number of items per page; required if `page` is supplied; must be > 0.  
* **Return** – List of tenants matching the search criteria, respecting pagination if provided.  
* **Throws** – `ArgumentException` if `pageSize` is supplied and ≤ 0; may propagate EF Core exceptions.

### GetWithDetailsAsync
```csharp
public async Task<Tenant?> GetWithDetailsAsync(Guid tenantId)
```
* **Purpose** – Retrieves a tenant together with related details (e.g., subscription, settings) in a single query.  
* **Parameters** – `tenantId`: Identifier of the tenant to load.  
* **Return** – The tenant with details populated, or `null` if not found.  
* **Throws** – `ArgumentException` if `tenantId` is `Guid.Empty`; may propagate EF Core exceptions.

### GetStatusCountsAsync
```csharp
public async Task<Dictionary<TenantStatus, int>> GetStatusCountsAsync()
```
* **Purpose** – Returns a count of tenants for each `TenantStatus` value.  
* **Parameters** – None.  
* **Return** – Dictionary mapping each status to its current tenant count.  
* **Throws** – May propagate EF Core exceptions.

### IsSlugUniqueAsync
```csharp
public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeTenantId = null)
```
* **Purpose** – Checks whether a slug is not already used by another tenant.  
* **Parameters** –  
  * `slug`: Slug to test; must not be `null` or whitespace.  
  * `excludeTenantId`: Optional tenant ID to ignore during the check (useful when updating an existing tenant).  
* **Return** – `true` if the slug is unique (or only used by the excluded tenant); otherwise `false`.  
* **Throws** – `ArgumentException` if `slug` is `null`/empty; may propagate EF Core exceptions.

### GetInactiveTenantsAsync
```csharp
public async Task<List<Tenant>> GetInactiveTenantsAsync()
```
* **Purpose** – Returns tenants whose status is `Inactive`.  
* **Parameters** – None.  
* **Return** – List of inactive tenants (may be empty).  
* **Throws** – May propagate EF Core exceptions.

### ActivateTenantAsync
```csharp
public async Task<bool> ActivateTenantAsync(Guid tenantId)
```
* **Purpose** – Sets a tenant’s status to `Active`.  
* **Parameters** – `tenantId`: Identifier of the tenant to activate.  
* **Return** – `true` if the tenant was found and updated; `false` if no tenant with the given ID exists.  
* **Throws** – `ArgumentException` if `tenantId` is `Guid.Empty`; may propagate `DbUpdateException` or other EF Core exceptions.

### SuspendTenantAsync
```csharp
public async Task<bool> SuspendTenantAsync(Guid tenantId)
```
* **Purpose** – Sets a tenant’s status to `Suspended`.  
* **Parameters** – `tenantId`: Identifier of the tenant to suspend.  
* **Return** – `true` if the tenant was found and updated; `false` if no tenant with the given ID exists.  
* **Throws** – `ArgumentException` if `tenantId` is `Guid.Empty`; may propagate `DbUpdateException` or other EF Core exceptions.

### GetBillingSummaryAsync
```csharp
public async Task<object> GetBillingSummaryAsync(Guid tenantId)
```
* **Purpose** – Retrieves a summary of billing information for a tenant (shape of the returned object is defined by the implementation).  
* **Parameters** – `tenantId`: Identifier of the tenant whose billing summary is required.  
* **Return** – An object containing billing summary data; `null` if the tenant does not exist.  
* **Throws** – `ArgumentException` if `tenantId` is `Guid.Empty`; may propagate EF Core exceptions.

## Usage

### Example 1: Retrieving a tenant by slug and activating it if found
```csharp
public async Task<IActionResult> HandleSlugRequest(string slug, TenantRepository repo)
{
    var tenant = await repo.GetBySlugAsync(slug);
    if (tenant == null)
        return NotFound();

    if (!tenant.IsActive)
    {
        var activated = await repo.ActivateTenantAsync(tenant.Id);
        if (!activated)
            return StatusCode(500, "Failed to activate tenant.");
    }

    return Ok(tenant);
}
```

### Example 2: Obtaining a count of tenants per status for a dashboard widget
```csharp
public async Task<DashboardModel> GetDashboardData(TenantRepository repo)
{
    var statusCounts = await repo.GetStatusCountsAsync();

    var model = new DashboardModel
    {
        ActiveTenants   = statusCounts.GetValueOrDefault(TenantStatus.Active),
        TrialTenants    = statusCounts.GetValueOrDefault(TenantStatus.Trial),
        SuspendedTenants= statusCounts.GetValueOrDefault(TenantStatus.Suspended),
        InactiveTenants = statusCounts.GetValueOrDefault(TenantStatus.Inactive)
    };

    return model;
}
```

## Notes

* **Thread safety** – The repository itself holds no mutable state; each method creates its own `TenantDbContext` via the supplied factory. Consequently, concurrent calls to different repository methods are safe as long as the `ITenantDbContextFactory<TenantDbContext>` implementation is thread‑safe (the typical scoped factory registrations in ASP.NET Core satisfy this). Sharing a single repository instance across threads does not introduce race conditions.

* **Null arguments** – All methods that accept a string, `Guid`, or enum parameter validate those arguments and throw `ArgumentException` or `ArgumentNullException` when the value is invalid. Callers should guard against passing default or unspecified values.

* **Exception propagation** – Errors originating from EF Core (e.g., connection failures, constraint violations, timeout) are not caught by the repository; they bubble up to the caller. Callers needing to translate these into application‑specific results should wrap calls in `try/catch` blocks as appropriate.

* **Return values** – Methods that may find no matching record return `null` (for single‑entity queries) or an empty collection (for list queries). Consumers should check for these cases rather than assuming a non‑null result.

* **Pagination** – `SearchAsync` supports optional pagination via `page` and `pageSize`. If either is supplied, both must be provided and `pageSize` must be positive; otherwise the method returns all matching rows without paging.

* **Exclusion in uniqueness checks** – `IsSlugUniqueAsync` accepts an optional `excludeTenantId` to allow validation during tenant updates where the current tenant’s own slug should not cause a conflict. Supplying a non‑empty `Guid` that does not correspond to an existing tenant has no effect on the result.
