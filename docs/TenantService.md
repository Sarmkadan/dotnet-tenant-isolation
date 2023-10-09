# TenantService

The `TenantService` class provides centralized management for tenant lifecycle operations in a multi-tenant application. It handles tenant creation, retrieval, status transitions, subscription validation, and statistical reporting, abstracting persistence and business logic for tenant-related workflows.

## API

### `TenantService`
**Purpose**
Initializes a new instance of the `TenantService`. Typically injected via dependency injection, requiring configured persistence and external service dependencies.

---

### `Task<Tenant> CreateTenantAsync`
**Purpose**
Creates a new tenant record with default or provided configuration. Validates uniqueness constraints (e.g., slug, identifier) before persistence.

**Parameters**
None (tenant details passed via request model or constructor parameters, not shown in signature).

**Returns**
A `Task<Tenant>` representing the newly created tenant entity.

**Throws**
- `ArgumentException`: If required tenant properties are missing or invalid.
- `InvalidOperationException`: If a tenant with the same identifier or slug already exists.

---

### `Task<Tenant> GetTenantAsync`
**Purpose**
Retrieves a tenant by its unique internal identifier.

**Parameters**
None (identifier passed via request context or method parameter, not shown in signature).

**Returns**
A `Task<Tenant>` representing the tenant if found; otherwise, `null`.

**Throws**
- `ArgumentNullException`: If the identifier is null or empty.

---

### `Task<Tenant> GetTenantBySlugAsync`
**Purpose**
Retrieves a tenant by its URL-friendly slug (e.g., `acme-corp`).

**Parameters**
None (slug passed via method parameter, not shown in signature).

**Returns**
A `Task<Tenant>` representing the tenant if found; otherwise, `null`.

**Throws**
- `ArgumentNullException`: If the slug is null or empty.

---

### `Task<bool> ActivateTenantAsync`
**Purpose**
Transitions a tenant from suspended or inactive status to active. Validates subscription status before allowing activation.

**Parameters**
None (tenant identifier passed via method parameter, not shown in signature).

**Returns**
A `Task<bool>` indicating success (`true`) or failure (`false`).

**Throws**
- `InvalidOperationException`: If the tenant is already active or lacks a valid subscription.

---

### `Task<bool> SuspendTenantAsync`
**Purpose**
Transitions a tenant from active to suspended status. Prevents further operations until reactivated.

**Parameters**
None (tenant identifier passed via method parameter, not shown in signature).

**Returns**
A `Task<bool>` indicating success (`true`) or failure (`false`).

**Throws**
- `InvalidOperationException`: If the tenant is already suspended or lacks a valid subscription.

---

### `Task<bool> DeleteTenantAsync`
**Purpose**
Soft-deletes a tenant, marking it as inactive and preventing future operations. Does not purge data.

**Parameters**
None (tenant identifier passed via method parameter, not shown in signature).

**Returns**
A `Task<bool>` indicating success (`true`) or failure (`false`).

**Throws**
- `InvalidOperationException`: If the tenant is already deleted or lacks permissions.

---

### `Task<Tenant> UpdateTenantAsync`
**Purpose**
Updates tenant metadata (e.g., name, contact details) while preserving immutable fields (e.g., slug, identifier).

**Parameters**
None (updated tenant model passed via method parameter, not shown in signature).

**Returns**
A `Task<Tenant>` representing the updated tenant.

**Throws**
- `ArgumentException`: If required fields are invalid.
- `InvalidOperationException`: If the tenant is deleted or suspended.

---

### `Task<bool> IsSubscriptionValidAsync`
**Purpose**
Checks if a tenant's subscription is active and within its validity period.

**Parameters**
None (tenant identifier passed via method parameter, not shown in signature).

**Returns**
A `Task<bool>` indicating subscription validity (`true` if active and within bounds).

**Throws**
- `ArgumentNullException`: If the tenant identifier is null or empty.

---

### `Task<List<Tenant>> GetActiveTenantsAsync`
**Purpose**
Retrieves all tenants with an active status.

**Parameters**
None.

**Returns**
A `Task<List<Tenant>>` containing active tenants, or an empty list if none exist.

**Throws**
None.

---

### `Task<List<Tenant>> GetTenantsByStatusAsync`
**Purpose**
Retrieves tenants filtered by a specific status (e.g., `Active`, `Suspended`, `Trial`).

**Parameters**
None (status passed via method parameter, not shown in signature).

**Returns**
A `Task<List<Tenant>>` containing matching tenants, or an empty list if none exist.

**Throws**
- `ArgumentException`: If the status is invalid.

---

### `Task<List<Tenant>> GetExpiringSubscriptionsAsync`
**Purpose**
Retrieves tenants whose subscriptions expire within a configurable threshold (e.g., 7 days).

**Parameters**
None (threshold configured via dependency injection or method parameter, not shown in signature).

**Returns**
A `Task<List<Tenant>>` containing tenants with expiring subscriptions, or an empty list.

**Throws**
None.

---

### `Task<List<Tenant>> SearchTenantsAsync`
**Purpose**
Searches tenants by partial name, slug, or contact email.

**Parameters**
None (search query passed via method parameter, not shown in signature).

**Returns**
A `Task<List<Tenant>>` containing matching tenants, or an empty list if none found.

**Throws**
- `ArgumentNullException`: If the search query is null or empty.

---

### `Task<bool> IsInTrialAsync`
**Purpose**
Checks if a tenant is in a trial period (typically time-limited or feature-restricted).

**Parameters**
None (tenant identifier passed via method parameter, not shown in signature).

**Returns**
A `Task<bool>` indicating trial status (`true` if in trial).

**Throws**
- `ArgumentNullException`: If the tenant identifier is null or empty.

---

### `Task<object> GetTenantStatisticsAsync`
**Purpose**
Aggregates statistical data for a tenant, including usage metrics, subscription details, and activity logs.

**Parameters**
None (tenant identifier passed via method parameter, not shown in signature).

**Returns**
A `Task<object>` containing a dynamic payload of statistics (structure varies by implementation).

**Throws**
- `InvalidOperationException`: If the tenant is deleted or suspended.

## Usage

### Example 1: Tenant Creation and Activation
