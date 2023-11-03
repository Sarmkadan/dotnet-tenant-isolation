# TenantServiceTests

Unit test class for `TenantService` that verifies tenant creation, retrieval, activation, and suspension behaviors under various input conditions and error scenarios.

## API

### `TenantServiceTests`

Public test class containing test cases for tenant operations.

### `CreateTenantAsync_WithValidInput_CreatesAndReturnsTenant`

Verifies that a tenant is successfully created and returned when valid name, slug, and email are provided. The returned tenant should match the input values.

### `CreateTenantAsync_WithCustomStrategy_UsesProvidedStrategy`

Ensures that when a custom tenant ID generation strategy is provided, the service uses it to generate the tenant ID instead of the default strategy.

### `CreateTenantAsync_WithNullOrWhitespaceName_ThrowsArgumentException`

Confirms that passing `null`, empty, or whitespace-only name results in an `ArgumentException`.

### `CreateTenantAsync_WithNullOrWhitespaceSlug_ThrowsArgumentException`

Confirms that passing `null`, empty, or whitespace-only slug results in an `ArgumentException`.

### `CreateTenantAsync_WithNullOrWhitespaceEmail_ThrowsArgumentNullException`

Confirms that passing `null`, empty, or whitespace-only email results in an `ArgumentNullException`.

### `CreateTenantAsync_WithDuplicateSlug_ThrowsTenantIsolationException`

Ensures that attempting to create a tenant with a slug that already exists throws a `TenantIsolationException`.

### `CreateTenantAsync_WhenRepositoryThrows_WrapsInTenantIsolationException`

Validates that if the underlying repository throws during tenant creation, the exception is wrapped in a `TenantIsolationException`.

### `CreateTenantAsync_WithMixedCaseSlug_ConvertsToLowercase`

Checks that slugs with mixed case are normalized to lowercase during tenant creation.

### `GetTenantAsync_WithValidId_ReturnsTenant`

Verifies that retrieving a tenant by a valid ID returns the corresponding tenant.

### `GetTenantAsync_WithNonExistentId_ThrowsTenantNotResolvedException`

Ensures that attempting to retrieve a tenant with a non-existent ID throws a `TenantNotResolvedException`.

### `GetTenantBySlugAsync_WithValidSlug_ReturnsTenant`

Confirms that retrieving a tenant by a valid slug returns the correct tenant.

### `GetTenantBySlugAsync_WithCaseMismatch_ReturnsTenant`

Validates that slug lookups are case-insensitive and return the correct tenant even if the provided slug differs in case.

### `GetTenantBySlugAsync_WithNullOrWhitespaceSlug_ThrowsArgumentException`

Ensures that passing `null`, empty, or whitespace-only slug results in an `ArgumentException`.

### `GetTenantBySlugAsync_WithNonExistentSlug_ThrowsTenantNotResolvedException`

Confirms that attempting to retrieve a tenant with a non-existent slug throws a `TenantNotResolvedException`.

### `ActivateTenantAsync_WithActiveTenant_ReturnsTrue`

Verifies that activating an already active tenant returns `true` and does not alter state.

### `ActivateTenantAsync_WithDeletedTenant_ThrowsTenantNotActiveException`

Ensures that attempting to activate a deleted tenant throws a `TenantNotActiveException`.

### `ActivateTenantAsync_WithArchivedStatus_ThrowsTenantNotActiveException`

Validates that attempting to activate a tenant with archived status throws a `TenantNotActiveException`.

### `SuspendTenantAsync_WithValidTenant_ReturnsTrueAndCallsRepository`

Confirms that suspending a valid tenant returns `true` and invokes the repository to persist the change.

### `SuspendTenantAsync_WithReason_PassesReasonToRepository`

Ensures that when a reason is provided during suspension, it is passed through to the repository.
