# AdminController

Provides administrative operations for managing tenants, subscriptions, and background tasks in a multi-tenant application. Exposes endpoints for querying system statistics, suspending or activating tenants, managing task queues, and monitoring expiring subscriptions.

## API

### `AdminController()`

Initializes a new instance of the `AdminController` class.

### `async Task<ActionResult<ApiResponse<object>>> GetStatistics()`

Retrieves high-level system statistics including tenant counts, active subscriptions, and queue status.

- **Returns**: `ActionResult<ApiResponse<object>>` – A response containing aggregated system metrics.
- **Throws**: May throw if the underlying statistics service is unavailable.

### `async Task<ActionResult<ApiResponse<PaginatedResponse<object>>>> GetAllTenants(int page, int pageSize)`

Retrieves a paginated list of all tenants in the system.

- **Parameters**:
  - `page` – The page number to retrieve.
  - `pageSize` – The number of items per page.
- **Returns**: `ActionResult<ApiResponse<PaginatedResponse<object>>>` – A paginated response of tenant data.
- **Throws**: May throw if pagination parameters are invalid or the tenant store is inaccessible.

### `async Task<ActionResult<ApiResponse<object>>> SuspendTenant(string tenantId, string? reason)`

Suspends a tenant, preventing further access to the application.

- **Parameters**:
  - `tenantId` – The unique identifier of the tenant to suspend.
  - `reason` – Optional reason for suspension.
- **Returns**: `ActionResult<ApiResponse<object>>` – A confirmation response upon successful suspension.
- **Throws**: May throw if the tenant does not exist or the operation fails due to concurrency conflicts.

### `async Task<ActionResult<ApiResponse<object>>> ActivateTenant(string tenantId)`

Reactivates a previously suspended tenant, restoring access to the application.

- **Parameters**:
  - `tenantId` – The unique identifier of the tenant to activate.
- **Returns**: `ActionResult<ApiResponse<object>>` – A confirmation response upon successful activation.
- **Throws**: May throw if the tenant does not exist or is already active.

### `ActionResult<ApiResponse<QueueStatistics>> GetQueueStatistics()`

Retrieves current statistics about the background task queue, including pending, processing, and completed tasks.

- **Returns**: `ActionResult<ApiResponse<QueueStatistics>>` – A response containing queue metrics.
- **Throws**: May throw if the queue monitoring service is unavailable.

### `ActionResult<ApiResponse<object>> EnqueueTask(string taskName, int priority)`

Adds a new background task to the queue with specified priority.

- **Parameters**:
  - `taskName` – The name of the task to enqueue.
  - `priority` – The priority level of the task (higher values indicate higher priority).
- **Returns**: `ActionResult<ApiResponse<object>>` – A confirmation response upon successful enqueueing.
- **Throws**: May throw if the task name is invalid or the queue is in an invalid state.

### `async Task<ActionResult<ApiResponse<List<object>>>> GetExpiringSubscriptions(DateTime threshold)`

Retrieves a list of subscriptions that are set to expire on or before the specified threshold date.

- **Parameters**:
  - `threshold` – The cutoff date for subscription expiration.
- **Returns**: `ActionResult<ApiResponse<List<object>>>` – A list of expiring subscription records.
- **Throws**: May throw if the subscription store is inaccessible.

### `string? Reason`

Gets or sets the reason associated with the current administrative operation (e.g., suspension or activation). Used as a field in requests.

### `string TaskName`

Gets or sets the name of the task to be enqueued. Used as a field in requests.

### `int Priority`

Gets or sets the priority level for the task to be enqueued. Higher values indicate higher priority.

## Usage

### Example 1: Suspending a Tenant
