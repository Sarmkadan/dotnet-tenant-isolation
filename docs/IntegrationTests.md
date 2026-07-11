# IntegrationTests

`IntegrationTests` is the primary integration test class for the `dotnet-tenant-isolation` project. It validates end-to-end tenant lifecycle operations, multi-tenant configuration isolation, subscription management, concurrent access patterns, and tenant querying/search functionality against a live (or test-container-based) system. The class implements `IAsyncLifetime` via xUnit, ensuring a consistent setup and teardown for each test run.

## API

### `public IntegrationTests()`
Default constructor. Instantiates the test class. The actual initialization of shared resources (e.g., database connections, service providers, test host) occurs in `InitializeAsync`.

### `public async Task InitializeAsync()`
Called automatically by the xUnit test framework before any test in the class executes. Sets up the integration environment: configures dependency injection, seeds baseline data, and prepares the tenant isolation infrastructure for the test session.

- **Returns**: A completed `Task`.
- **Throws**: May throw if the underlying infrastructure (database, service bus, etc.) is unreachable or if seed data scripts fail.

### `public async Task DisposeAsync()`
Called automatically by xUnit after all tests in the class have completed. Tears down the integration environment, disposes of containers or connections, and cleans up any residual test data.

- **Returns**: A completed `Task`.
- **Throws**: Typically does not throw; exceptions during cleanup are logged and suppressed to avoid masking test failures.

### `public async Task TenantLifecycle_CreateUpdateDeleteRestore_WorksEndToEnd()`
Verifies the full tenant lifecycle: creation of a new tenant, updating its metadata, soft-deleting it, and restoring it from the soft-deleted state. Each step asserts that the tenant’s state and configuration remain correctly isolated.

- **Returns**: A completed `Task`.
- **Throws**: Assertion failures if any lifecycle transition produces an unexpected state or if the restore operation does not reinstate original configuration.

### `public async Task MultiTenantConfiguration_IsolatedPerTenant_WorksCorrectly()`
Confirms that configuration values set for one tenant are not visible to or overwritten by another tenant. Creates two tenants, assigns distinct configuration keys, and reads them back cross-tenant to verify strict isolation.

- **Returns**: A completed `Task`.
- **Throws**: Assertion failures if a tenant can read or mutate another tenant’s configuration.

### `public async Task SubscriptionManagement_ExpiringSubscriptionDetection_WorksCorrectly()`
Tests the detection logic for subscriptions nearing expiration. Seeds tenants with subscriptions at various expiry thresholds and verifies that only those matching the “expiring soon” criteria are returned by the detection query.

- **Returns**: A completed `Task`.
- **Throws**: Assertion failures if the detection query misses expiring subscriptions or includes those not yet in the warning window.

### `public async Task TrialConversion_PromoteTrialToActive_WorksCorrectly()`
Validates the workflow that promotes a trial tenant to an active subscription. Ensures that trial-specific limitations are removed, billing status transitions correctly, and the tenant retains its existing configuration.

- **Returns**: A completed `Task`.
- **Throws**: Assertion failures if the conversion leaves trial artifacts or fails to update the subscription tier.

### `public async Task ConcurrentConfigurationUpdates_MultipleTenants_AllSucceed()`
Exercises concurrent updates to configuration across multiple tenants. Launches parallel tasks that each update a distinct tenant’s configuration, then asserts that every update persisted correctly and no write conflicts occurred.

- **Returns**: A completed `Task`.
- **Throws**: Assertion failures if any update is lost, partially applied, or if optimistic concurrency exceptions are not handled gracefully.

### `public async Task ConcurrentTenantCreation_MultipleThreads_AllSucceed()`
Creates multiple tenants simultaneously from different threads and verifies that all creations succeed without duplicate slugs, key collisions, or internal server errors.

- **Returns**: A completed `Task`.
- **Throws**: Assertion failures if any creation task faults, or if the final tenant count does not match the number of requested creations.

### `public async Task ConcurrentCacheAccess_SameConfiguration_DoesNotCauseRaceCondition()`
Targets the caching layer: multiple threads read and write the same tenant’s configuration concurrently. Asserts that the cache remains consistent, no stale values are served after an update, and no race conditions corrupt the cached data.

- **Returns**: A completed `Task`.
- **Throws**: Assertion failures if a read after a write returns the old value, or if cache internals throw synchronization exceptions.

### `public async Task TenantQuerying_VariousFilters_ReturnCorrectResults()`
Tests the tenant query API with combinations of filters (status, creation date range, subscription tier). Verifies that the result set matches the expected tenants and that pagination/sorting parameters are respected.

- **Returns**: A completed `Task`.
- **Throws**: Assertion failures if filters are ignored, results are incorrectly ordered, or pagination boundaries are off.

### `public async Task TenantSearch_ByNameSlugAndEmail_FindsCorrectTenants()`
Validates the search endpoint using name, slug, and email address criteria. Seeds multiple tenants with overlapping substrings and confirms that only exact or prefix matches (according to the search contract) are returned.

- **Returns**: A completed `Task`.
- **Throws**: Assertion failures if the search returns false positives or misses tenants that should match.

### `public async Task ConfigurationImportExport_RoundTrip_PreservesData()`
Exports a tenant’s configuration to a portable format, imports it into a new tenant, and asserts that the resulting configuration is identical to the original. Covers nested keys, sensitive value masking, and metadata preservation.

- **Returns**: A completed `Task`.
- **Throws**: Assertion failures if any configuration value differs after the round-trip, or if the import rejects a valid export payload.

### `public async Task TenantStatusTransitions_SuspendAndReactivate_WorksCorrectly()`
Suspends an active tenant, verifies that access is blocked and status flags are set, then reactivates the tenant and confirms full access restoration and correct status reversal.

- **Returns**: A completed `Task`.
- **Throws**: Assertion failures if a suspended tenant can still perform restricted operations, or if reactivation does not clear the suspended state.

### `public async Task InactiveTenantDetection_FindsTenantsNotUpdatedRecently()`
Seeds tenants with varying last-activity timestamps and invokes the inactive-tenant detection job. Asserts that only tenants whose last update falls outside the configured inactivity threshold are identified.

- **Returns**: A completed `Task`.
- **Throws**: Assertion failures if the detection job includes recently active tenants or misses those that are clearly inactive.

## Usage

### Example 1: Running the full tenant lifecycle test in a CI pipeline
```csharp
// Typically executed via `dotnet test` with a test-containerized environment.
// The test class is instantiated by the xUnit runner; no manual instantiation needed.
// Inside the CI pipeline script (e.g., Azure DevOps YAML):
//
// - task: DotNetCoreCLI@2
//   displayName: 'Run integration tests'
//   inputs:
//     command: test
//     projects: 'tests/**/*IntegrationTests.csproj'
//     arguments: '--filter "FullyQualifiedName~TenantLifecycle_CreateUpdateDeleteRestore_WorksEndToEnd"'
//
// The test itself relies on InitializeAsync to spin up a PostgreSQL container
// and apply migrations before exercising the lifecycle.
```

### Example 2: Debugging a specific concurrency test locally
```csharp
// When debugging locally, you can run a single test from the IDE or CLI:
//
// dotnet test --filter "FullyQualifiedName~ConcurrentConfigurationUpdates_MultipleTenants_AllSucceed"
//
// The test method:
// 1. Awaits InitializeAsync (called automatically by the framework).
// 2. Creates 5 tenants sequentially to set up isolated baselines.
// 3. Uses Task.WhenAll to fire 5 parallel configuration updates.
// 4. Reads back each tenant’s configuration and asserts the updated value.
// 5. DisposeAsync cleans up the database rows.
//
// This pattern is useful for verifying that optimistic concurrency controls
// (e.g., row versioning) work correctly under load.
```

## Notes

- **Thread safety**: The test methods themselves are asynchronous but do not share mutable state across tests; xUnit creates a new instance of `IntegrationTests` for each test method when using `IClassFixture` or collection fixtures. Concurrent tests (`ConcurrentConfigurationUpdates_*`, `ConcurrentTenantCreation_*`, `ConcurrentCacheAccess_*`) intentionally spawn multiple tasks within a single test to exercise the system-under-test’s synchronization mechanisms, not the test class’s own state.
- **Edge cases**: Tests that involve soft-delete and restore (`TenantLifecycle_*`, `TenantStatusTransitions_*`) must account for unique constraint violations on slugs or emails if a restored tenant collides with a newly created one. The test data seeding avoids such collisions by using deterministic, timestamp-salted identifiers.
- **Data isolation**: `MultiTenantConfiguration_IsolatedPerTenant_WorksCorrectly` and `ConfigurationImportExport_RoundTrip_PreservesData` assume that the underlying store enforces tenant-key scoping. If the isolation is implemented purely in application code, these tests serve as regression guards against accidental cross-tenant leakage.
- **Subscription detection timing**: `SubscriptionManagement_ExpiringSubscriptionDetection_WorksCorrectly` and `InactiveTenantDetection_FindsTenantsNotUpdatedRecently` rely on clock-based thresholds. The test environment freezes or controls the clock (e.g., via `TimeProvider` abstraction) to avoid flakiness from wall-clock drift.
- **Cleanup**: `DisposeAsync` uses a resilient cleanup pattern—catching and logging per-resource teardown exceptions—so that a failure in one cleanup step does not orphan other resources or prevent subsequent test runs.
