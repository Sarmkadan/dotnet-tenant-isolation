# ConfigurationServiceTests

The `ConfigurationServiceTests` class serves as the comprehensive test suite for validating the behavior of the configuration management service within the `dotnet-tenant-isolation` project. It verifies core functionalities including creation, retrieval, updating, and deletion of tenant-specific configuration settings, while specifically asserting correct handling of encryption flags, cache invalidation strategies, type conversion mechanisms, and error conditions for invalid inputs.

## API

### Constructors

#### `public ConfigurationServiceTests()`
Initializes a new instance of the `ConfigurationServiceTests` class. This constructor sets up the necessary test context, mocks, or dependencies required to execute the subsequent test methods against the configuration service.

### Lifecycle Methods

#### `public async Task InitializeAsync()`
Executes asynchronous initialization logic prior to running the test suite. This method typically prepares the test environment, such as seeding initial data, configuring mock services, or establishing database connections required for the tests to run in a clean state.

#### `public async Task DisposeAsync()`
Executes asynchronous cleanup logic after the test suite has completed. This method ensures that resources allocated during `InitializeAsync` or individual tests (such as database contexts, file handles, or network connections) are properly released to prevent resource leaks.

### Set Configuration Tests

#### `public async Task SetConfigurationAsync_WithNewKey_CreatesConfiguration()`
Validates that invoking the set operation with a unique key successfully creates a new configuration entry. It asserts that the data is persisted correctly and that the operation returns the expected success indicator.

#### `public async Task SetConfigurationAsync_WithExistingKey_UpdatesConfiguration()`
Verifies that calling the set operation with a key that already exists results in an update of the existing value rather than creating a duplicate. It confirms the new value overwrites the old one and timestamps or versioning metadata are updated appropriately.

#### `public async Task SetConfigurationAsync_WithEncryption_SetsEncryptionFlag()`
Ensures that when a configuration value is marked for encryption, the service correctly sets the internal encryption flag and stores the data in an encrypted format. It asserts that the raw value is not stored in plain text.

#### `public async Task SetConfigurationAsync_WithNullOrWhitespaceKey_ThrowsException()`
Confirms that the service enforces input validation by throwing an exception when the provided configuration key is `null`, empty, or consists only of whitespace. This prevents the creation of invalid configuration entries.

#### `public async Task SetConfigurationAsync_InvalidatesCacheAfterSet()`
Asserts that successfully setting a configuration value triggers an invalidation of the relevant cache entry. This ensures that subsequent read operations retrieve the fresh data from the source rather than a stale cached value.

### Get Configuration Tests

#### `public async Task GetConfigurationAsync_WithExistingConfiguration_ReturnsConfiguration()`
Validates that requesting a configuration value by an existing key returns the correct stored value. It checks that the returned object matches the data previously set.

#### `public async Task GetConfigurationAsync_CachesResultAfterFirstCall()`
Verifies that the first successful retrieval of a configuration value populates the cache. Subsequent calls within the cache validity window should be served from the cache rather than hitting the underlying data store.

#### `public async Task GetConfigurationAsync_WithNonExistentKey_ReturnsNull()`
Ensures that requesting a configuration key that does not exist results in a `null` return value rather than throwing an exception, allowing callers to handle missing configurations gracefully.

#### `public async Task GetConfigurationAsync_Generic_ConvertsStringToInt()`
Tests the generic retrieval method's ability to automatically convert a stored string representation of a number into an `int` type. It asserts that the conversion succeeds and the numeric value is accurate.

#### `public async Task GetConfigurationAsync_Generic_ConvertsToBool()`
Tests the generic retrieval method's ability to automatically convert a stored string representation of a boolean (e.g., "true", "false") into a `bool` type. It asserts that the conversion logic handles standard boolean string formats correctly.

#### `public async Task GetConfigurationAsync_Generic_ReturnsDefaultWhenNotFound()`
Validates that the generic retrieval method returns the default value for the specified type (e.g., `0` for `int`, `false` for `bool`) when the requested key does not exist, instead of returning `null` or throwing.

#### `public async Task GetConfigurationAsync_Generic_WithConversionError_ThrowsException()`
Confirms that if the stored string value cannot be converted to the requested generic type (e.g., trying to convert "abc" to `int`), the method throws a specific conversion or format exception.

### Delete Configuration Tests

#### `public async Task DeleteConfigurationAsync_WithExistingKey_DeletesConfiguration()`
Verifies that deleting an existing configuration key successfully removes the entry from the data store. It asserts that subsequent attempts to retrieve the key return `null`.

#### `public async Task DeleteConfigurationAsync_WithNonExistentKey_ReturnsFalse()`
Ensures that attempting to delete a key that does not exist returns `false` to indicate no action was taken, rather than throwing an exception.

#### `public async Task DeleteConfigurationAsync_InvalidatesCacheAfterDelete()`
Asserts that successfully deleting a configuration entry triggers an invalidation of the cache for that specific key. This prevents the system from serving deleted configuration data from the cache.

### Bulk Retrieval Tests

#### `public async Task GetAllConfigurationsAsync_ReturnsAllConfigurationsForTenant()`
Validates that the method retrieves a complete collection of all configuration settings associated with the current tenant. It ensures no entries are missing and that tenant isolation is respected.

#### `public async Task GetAllConfigurationsAsync_CachesResult()`
Verifies that the result of fetching all configurations is cached. Subsequent calls to retrieve all configurations should utilize the cached collection to improve performance.

## Usage

### Example 1: Verifying Cache Invalidation on Update
This example demonstrates how the test suite validates that updating a configuration forces a cache refresh, ensuring data consistency.

```csharp
[TestFixture]
public class ConfigurationCacheBehavior
{
    [Test]
    public async Task UpdateShouldInvalidateCache()
    {
        var testSuite = new ConfigurationServiceTests();
        await testSuite.InitializeAsync();

        // Initial set and retrieval to populate cache
        await testSuite.SetConfigurationAsync_WithNewKey_CreatesConfiguration();
        await testSuite.GetConfigurationAsync_CachesResultAfterFirstCall();

        // Update the value
        await testSuite.SetConfigurationAsync_WithExistingKey_UpdatesConfiguration();

        // The test 'SetConfigurationAsync_InvalidatesCacheAfterSet' internally verifies 
        // that the next read bypasses the old cache.
        await testSuite.SetConfigurationAsync_InvalidatesCacheAfterSet();

        await testSuite.DisposeAsync();
    }
}
```

### Example 2: Validating Type Conversion and Error Handling
This example illustrates testing the generic retrieval capabilities, including successful type conversion and handling of conversion failures.

```csharp
[TestFixture]
public class ConfigurationTypeConversionTests
{
    [Test]
    public async Task GenericRetrievalHandlesConversionsAndErrors()
    {
        var testSuite = new ConfigurationServiceTests();
        await testSuite.InitializeAsync();

        // Test successful integer conversion
        await testSuite.GetConfigurationAsync_Generic_ConvertsStringToInt();

        // Test successful boolean conversion
        await testSuite.GetConfigurationAsync_Generic_ConvertsToBool();

        // Test default value behavior for missing keys
        await testSuite.GetConfigurationAsync_Generic_ReturnsDefaultWhenNotFound();

        // Verify that invalid format strings throw appropriate exceptions
        try 
        {
            await testSuite.GetConfigurationAsync_Generic_WithConversionError_ThrowsException();
        }
        catch (Exception ex)
        {
            // Expected behavior: Exception thrown due to conversion failure
            Assert.IsNotNull(ex);
        }

        await testSuite.DisposeAsync();
    }
}
```

## Notes

*   **Input Validation**: The service strictly enforces key validity. Any attempt to set a configuration with a `null`, empty, or whitespace-only key will result in an immediate exception, preventing corrupt data entry.
*   **Cache Consistency**: The implementation relies on explicit cache invalidation. Both `SetConfigurationAsync` and `DeleteConfigurationAsync` are designed to purge specific cache entries immediately upon successful completion. Tests verifying read-after-write or read-after-delete scenarios depend on this invalidation logic functioning synchronously before the test assertion runs.
*   **Type Safety**: The generic `GetConfigurationAsync` methods perform runtime type conversion. While convenient, they introduce a failure mode where malformed data in the store (e.g., non-numeric strings in an integer field) will cause runtime exceptions rather than returning null. Callers must ensure data integrity or handle `FormatException` derivatives.
*   **Thread Safety**: As a test class, `ConfigurationServiceTests` is typically instantiated per test run or context. However, the underlying service being tested implies that cache invalidation and concurrent access patterns should be considered. The tests assume that `InitializeAsync` provides an isolated environment, but in a production multi-threaded scenario, the cache invalidation mechanism must be thread-safe to prevent race conditions where a stale value is read between a write and the cache clear.
*   **Tenant Isolation**: The `GetAllConfigurationsAsync` method is scoped to the current tenant context established during initialization. Tests verify that data from other tenants is never exposed, reinforcing the isolation boundaries of the system.
