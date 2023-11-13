# TenantApiControllerExtensions

Extension methods for `TenantApiController` that provide tenant management operations including retrieval by identifiers or status, bulk activation/suspension, and dashboard statistics.

## API

### `GetTenantsByIds`

Retrieves a collection of tenants by their unique identifiers.

- **Parameters**
  - `controller` (`TenantApiController`): The controller instance.
  - `tenantIds` (`IEnumerable<Guid>`): The collection of tenant identifiers to retrieve.
  - `cancellationToken` (`CancellationToken`): A token to monitor for cancellation requests.
- **Returns**
  - `Task<IActionResult>`: An `OkObjectResult` containing a list of tenants if successful; otherwise, an appropriate error result.
- **Exceptions**
  - Throws `ArgumentNullException` if `tenantIds` is `null`.
  - Throws `OperationCanceledException` if the `cancellationToken` is triggered.

### `GetTenantsByStatus`

Retrieves tenants filtered by their current operational status.

- **Parameters**
  - `controller` (`TenantApiController`): The controller instance.
  - `status` (`TenantStatus`): The status to filter tenants (e.g., active, suspended).
  - `cancellationToken` (`CancellationToken`): A token to monitor for cancellation requests.
- **Returns**
  - `Task<IActionResult>`: An `OkObjectResult` containing a list of tenants matching the status; otherwise, an appropriate error result.
- **Exceptions**
  - Throws `OperationCanceledException` if the `cancellationToken` is triggered.

### `BulkActivateTenants`

Activates multiple tenants in a single operation.

- **Parameters**
  - `controller` (`TenantApiController`): The controller instance.
  - `tenantIds` (`IEnumerable<Guid>`): The collection of tenant identifiers to activate.
  - `reason` (`string?`): An optional reason for the bulk activation.
  - `cancellationToken` (`CancellationToken`): A token to monitor for cancellation requests.
- **Returns**
  - `Task<IActionResult>`: An `OkObjectResult` indicating success or failure; otherwise, an appropriate error result.
- **Exceptions**
  - Throws `ArgumentNullException` if `tenantIds` is `null`.
  - Throws `OperationCanceledException` if the `cancellationToken` is triggered.

### `BulkSuspendTenants`

Suspends multiple tenants in a single operation.

- **Parameters**
  - `controller` (`TenantApiController`): The controller instance.
  - `tenantIds` (`IEnumerable<Guid>`): The collection of tenant identifiers to suspend.
  - `reason` (`string?`): An optional reason for the bulk suspension.
  - `cancellationToken` (`CancellationToken`): A token to monitor for cancellation requests.
- **Returns**
  - `Task<IActionResult>`: An `OkObjectResult` indicating success or failure; otherwise, an appropriate error result.
- **Exceptions**
  - Throws `ArgumentNullException` if `tenantIds` is `null`.
  - Throws `OperationCanceledException` if the `cancellationToken` is triggered.

### `GetTenantByName`

Retrieves a tenant by its name.

- **Parameters**
  - `controller` (`TenantApiController`): The controller instance.
  - `name` (`string`): The name of the tenant to retrieve.
  - `cancellationToken` (`CancellationToken`): A token to monitor for cancellation requests.
- **Returns**
  - `Task<IActionResult>`: An `OkObjectResult` containing the tenant if found; otherwise, an appropriate error result (e.g., `NotFoundObjectResult`).
- **Exceptions**
  - Throws `ArgumentNullException` if `name` is `null`.
  - Throws `OperationCanceledException` if the `cancellationToken` is triggered.

### `GetDashboardStatistics`

Retrieves aggregated statistics for the tenant dashboard.

- **Parameters**
  - `controller` (`TenantApiController`): The controller instance.
  - `cancellationToken` (`CancellationToken`): A token to monitor for cancellation requests.
- **Returns**
  - `Task<IActionResult>`: An `OkObjectResult` containing dashboard statistics; otherwise, an appropriate error result.
- **Exceptions**
  - Throws `OperationCanceledException` if the `cancellationToken` is triggered.

### `TenantIds`

Gets the list of tenant identifiers associated with the current context.

- **Type**
  - `List<Guid>`
- **Remarks**
  - This property is read-only and reflects the identifiers relevant to the current operation or request scope.

### `Reason`

Gets or sets an optional reason string associated with the current operation.

- **Type**
  - `string?`
- **Remarks**
  - Intended for audit logging or operational context; may be `null`.

## Usage

### Retrieving Tenants by IDs
