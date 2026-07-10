# TenantResolutionBenchmarks

`TenantResolutionBenchmarks` is a benchmarking class designed to evaluate and compare the performance of various tenant resolution strategies in a multi-tenant application. It provides methods to resolve tenants from different sources (e.g., HTTP headers, route values, claims, subdomains) and includes an in-memory tenant store for testing purposes. This class is primarily used for performance testing and validation of tenant isolation mechanisms.

## API

### `public void Setup()`
Initializes the benchmark environment, including the in-memory tenant store and any required test data. This method should be called before executing any benchmark methods.

**Throws:**
- May throw exceptions if initialization fails (e.g., data setup errors).

---

### `public async ValueTask<Tenant> ResolveTenant_FromHeader()`
Resolves the current tenant from an HTTP header (e.g., `X-Tenant-Id`).

**Returns:**
- A `ValueTask<Tenant>` representing the resolved tenant.

**Throws:**
- Throws if the tenant cannot be resolved (e.g., header missing or invalid).
- Throws if the tenant does not exist in the store.

---

### `public async ValueTask<Tenant> ResolveTenant_FromRoute()`
Resolves the current tenant from a route parameter (e.g., `{tenantSlug}` in the URL path).

**Returns:**
- A `ValueTask<Tenant>` representing the resolved tenant.

**Throws:**
- Throws if the tenant cannot be resolved (e.g., route parameter missing or invalid).
- Throws if the tenant does not exist in the store.

---

### `public async ValueTask<Tenant> ResolveTenant_FromClaims()`
Resolves the current tenant from claims in the authenticated user's identity (e.g., `tenant_id` claim).

**Returns:**
- A `ValueTask<Tenant>` representing the resolved tenant.

**Throws:**
- Throws if the tenant cannot be resolved (e.g., claims missing or invalid).
- Throws if the tenant does not exist in the store.

---

### `public async ValueTask<Tenant> ResolveTenant_FromSubdomain()`
Resolves the current tenant from a subdomain (e.g., `tenant1.example.com`).

**Returns:**
- A `ValueTask<Tenant>` representing the resolved tenant.

**Throws:**
- Throws if the tenant cannot be resolved (e.g., subdomain missing or invalid).
- Throws if the tenant does not exist in the store.

---

### `public Tenant? GetCurrentTenant()`
Retrieves the currently resolved tenant, if any.

**Returns:**
- The `Tenant` object if resolved, otherwise `null`.

---

### `public bool HasTenant()`
Indicates whether a tenant has been successfully resolved.

**Returns:**
- `true` if a tenant is resolved, otherwise `false`.

---

### `public void Cleanup()`
Resets the benchmark state, clearing any resolved tenant or temporary data. This method should be called after each benchmark iteration to ensure isolation.

---

### `public void Dispose()`
Releases resources used by the benchmark, including the in-memory tenant store. This method should be called when the benchmark is no longer needed.

---

### `public InMemoryTenantStore`
An in-memory tenant store used for benchmarking. This property provides access to the store's methods for tenant management.

---

### `public Task<Tenant?> GetTenantAsync(string tenantId)`
Retrieves a tenant by its unique identifier.

**Parameters:**
- `tenantId`: The unique identifier of the tenant.

**Returns:**
- A `Task<Tenant?>` representing the tenant if found, otherwise `null`.

---

### `public Task<Tenant?> GetTenantBySlugAsync(string slug)`
Retrieves a tenant by its slug (e.g., a URL-friendly identifier).

**Parameters:**
- `slug`: The slug of the tenant.

**Returns:**
- A `Task<Tenant?>` representing the tenant if found, otherwise `null`.

---

### `public Task<IEnumerable<Tenant>> GetAllActiveTenantsAsync()`
Retrieves all active tenants in the store.

**Returns:**
- A `Task<IEnumerable<Tenant>>` representing the collection of active tenants.

---

### `public Task<Tenant?> GetTenantByIdAsync(string tenantId)`
Alias for `GetTenantAsync`. Retrieves a tenant by its unique identifier.

**Parameters:**
- `tenantId`: The unique identifier of the tenant.

**Returns:**
- A `Task<Tenant?>` representing the tenant if found, otherwise `null`.

---

### `public Task<bool> TenantExistsAsync(string tenantId)`
Checks whether a tenant with the specified identifier exists.

**Parameters:**
- `tenantId`: The unique identifier of the tenant.

**Returns:**
- A `Task<bool>` indicating `true` if the tenant exists, otherwise `false`.

---

### `public Task<bool> TenantExistsBySlugAsync(string slug)`
Checks whether a tenant with the specified slug exists.

**Parameters:**
- `slug`: The slug of the tenant.

**Returns:**
- A `Task<bool>` indicating `true` if the tenant exists, otherwise `false`.

---

### `public Task AddTenantAsync(Tenant tenant)`
Adds a new tenant to the store.

**Parameters:**
- `tenant`: The tenant to add.

**Returns:**
- A `Task` representing the asynchronous operation.

**Throws:**
- Throws if the tenant already exists or if validation fails.

---

### `public Task UpdateTenantAsync(Tenant tenant)`
Updates an existing tenant in the store.

**Parameters:**
- `tenant`: The tenant to update.

**Returns:**
- A `Task` representing the asynchronous operation.

**Throws:**
- Throws if the tenant does not exist or if validation fails.

---

### `public Task DeleteTenantAsync(string tenantId)`
Deletes a tenant from the store.

**Parameters:**
- `tenantId`: The unique identifier of the tenant to delete.

**Returns:**
- A `Task` representing the asynchronous operation.

**Throws:**
- Throws if the tenant does not exist.

## Usage

### Example 1: Benchmarking Tenant Resolution from Headers
