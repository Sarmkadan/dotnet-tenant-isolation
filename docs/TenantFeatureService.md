# TenantFeatureService

Manages per-tenant feature flags, rollout percentages, usage tracking, and quota enforcement within the dotnet-tenant-isolation framework. This service provides the authoritative API for checking whether a feature is enabled for a given tenant, toggling features on or off, setting gradual rollout percentages, recording and resetting usage counters, and retrieving feature state and statistics. All operations are tenant-scoped and asynchronous.

## API

### `TenantFeatureService`
Constructor. Initializes a new instance of the service, typically requiring injected dependencies such as a feature store, tenant context accessor, and usage tracking infrastructure. The exact dependencies are internal; consumers obtain an instance through dependency injection.

### `async Task<bool> IsFeatureEnabledAsync`
Determines whether a specific feature is currently enabled for the current tenant. Evaluates the feature’s enabled flag, rollout percentage, and any applicable usage limits. Returns `true` if the feature is active and the tenant falls within the rollout window; otherwise `false`.

| Parameter | Type | Description |
|---|---|---|
| `featureName` | `string` | The unique name of the feature to check. |

**Returns:** `true` if the feature is enabled for the tenant; `false` otherwise.

**Throws:** `ArgumentException` when `featureName` is null or whitespace. `TenantNotFoundException` when no tenant context can be resolved.

### `async Task<TenantFeature?> GetFeatureAsync`
Retrieves the full feature definition for the current tenant, including enabled state, rollout percentage, usage limits, and metadata. Returns `null` if the feature does not exist.

| Parameter | Type | Description |
|---|---|---|
| `featureName` | `string` | The unique name of the feature. |

**Returns:** A `TenantFeature` object if found; `null` otherwise.

**Throws:** `ArgumentException` when `featureName` is null or whitespace. `TenantNotFoundException` when no tenant context can be resolved.

### `async Task<TenantFeature> EnableFeatureAsync`
Enables a feature for the current tenant. If the feature does not already exist in the tenant’s configuration, it is created with default settings (enabled, 100% rollout, no usage limits). Returns the resulting `TenantFeature`.

| Parameter | Type | Description |
|---|---|---|
| `featureName` | `string` | The unique name of the feature to enable. |

**Returns:** The `TenantFeature` instance after enabling.

**Throws:** `ArgumentException` when `featureName` is null or whitespace. `TenantNotFoundException` when no tenant context can be resolved.

### `async Task<bool> DisableFeatureAsync`
Disables a feature for the current tenant. Returns `true` if the feature was successfully disabled or was already disabled; returns `false` if the feature does not exist.

| Parameter | Type | Description |
|---|---|---|
| `featureName` | `string` | The unique name of the feature to disable. |

**Returns:** `true` if the feature is now disabled; `false` if the feature was not found.

**Throws:** `ArgumentException` when `featureName` is null or whitespace. `TenantNotFoundException` when no tenant context can be resolved.

### `async Task<bool> SetRolloutPercentageAsync`
Sets the rollout percentage for a feature on the current tenant. The percentage determines what fraction of requests (based on a consistent hash of the tenant identity) will have the feature enabled. Returns `true` if the update succeeded; `false` if the feature does not exist.

| Parameter | Type | Description |
|---|---|---|
| `featureName` | `string` | The unique name of the feature. |
| `percentage` | `double` | Rollout percentage between `0.0` and `100.0` inclusive. |

**Returns:** `true` if the percentage was updated; `false` if the feature was not found.

**Throws:** `ArgumentOutOfRangeException` when `percentage` is outside `[0.0, 100.0]`. `ArgumentException` when `featureName` is null or whitespace. `TenantNotFoundException` when no tenant context can be resolved.

### `async Task<List<TenantFeature>> GetEnabledFeaturesAsync`
Returns all features currently enabled for the tenant. A feature is considered enabled if its enabled flag is `true` and the tenant falls within its rollout percentage.

**Returns:** A list of `TenantFeature` objects that are active for the tenant. May be empty.

**Throws:** `TenantNotFoundException` when no tenant context can be resolved.

### `async Task<List<TenantFeature>> GetAllFeaturesAsync`
Returns every feature defined for the tenant, regardless of enabled state or rollout percentage.

**Returns:** A list of all `TenantFeature` objects for the tenant. May be empty.

**Throws:** `TenantNotFoundException` when no tenant context can be resolved.

### `async Task<List<TenantFeature>> GetFeaturesByCategoryAsync`
Returns all features belonging to a specified category for the current tenant. Filtering is based on the `Category` property of each `TenantFeature`.

| Parameter | Type | Description |
|---|---|---|
| `category` | `string` | The category label to filter by. |

**Returns:** A list of `TenantFeature` objects in the given category. May be empty.

**Throws:** `ArgumentException` when `category` is null or whitespace. `TenantNotFoundException` when no tenant context can be resolved.

### `async Task<bool> RecordFeatureUsageAsync`
Records a single usage event for a feature on the current tenant. Increments the usage counter and updates last-usage timestamps. Returns `true` if the usage was recorded; `false` if the feature does not exist or is not enabled.

| Parameter | Type | Description |
|---|---|---|
| `featureName` | `string` | The unique name of the feature being used. |

**Returns:** `true` if usage was recorded; `false` if the feature is missing or disabled.

**Throws:** `ArgumentException` when `featureName` is null or whitespace. `TenantNotFoundException` when no tenant context can be resolved.

### `async Task<bool> ResetFeatureUsageAsync`
Resets the usage counter for a feature on the current tenant to zero. Returns `true` if the reset succeeded; `false` if the feature does not exist.

| Parameter | Type | Description |
|---|---|---|
| `featureName` | `string` | The unique name of the feature. |

**Returns:** `true` if usage was reset; `false` if the feature was not found.

**Throws:** `ArgumentException` when `featureName` is null or whitespace. `TenantNotFoundException` when no tenant context can be resolved.

### `async Task<bool> CheckUsageLimitAsync`
Evaluates whether the current tenant has exceeded a defined usage limit for a feature. Compares the recorded usage count against the feature’s configured limit. Returns `true` if the limit has been reached or exceeded; `false` if usage is still within bounds or no limit is defined.

| Parameter | Type | Description |
|---|---|---|
| `featureName` | `string` | The unique name of the feature. |

**Returns:** `true` if the usage limit is exceeded; `false` otherwise.

**Throws:** `ArgumentException` when `featureName` is null or whitespace. `TenantNotFoundException` when no tenant context can be resolved.

### `async Task InitializeDefaultFeaturesAsync`
Seeds the tenant’s feature configuration with a predefined set of default features. Typically called during tenant provisioning. Existing features are not overwritten; only missing defaults are added.

**Throws:** `TenantNotFoundException` when no tenant context can be resolved.

### `async Task<object> GetStatisticsAsync`
Returns an opaque statistics object containing aggregated data about the tenant’s feature usage, such as total usage counts, feature adoption rates, and limit-exceeded events. The exact shape of the returned object is implementation-defined and intended for administrative dashboards.

**Returns:** An `object` representing tenant feature statistics.

**Throws:** `TenantNotFoundException` when no tenant context can be resolved.

### `async Task<(bool canUse, string? errorMessage)> CanUseFeatureAsync`
Performs a comprehensive pre-use check for a feature. Evaluates whether the feature is enabled, the tenant is within the rollout percentage, and no usage limit has been exceeded. Returns a tuple indicating whether the feature can be used and, if not, a human-readable reason.

| Parameter | Type | Description |
|---|---|---|
| `featureName` | `string` | The unique name of the feature. |

**Returns:** A tuple where `canUse` is `true` if the feature is available, and `errorMessage` contains the reason when `canUse` is `false` (e.g., `"Feature is disabled"`, `"Usage limit exceeded"`, `"Not in rollout group"`). `errorMessage` is `null` when `canUse` is `true`.

**Throws:** `ArgumentException` when `featureName` is null or whitespace. `TenantNotFoundException` when no tenant context can be resolved.

## Usage

### Example 1: Guarding a feature with rollout and usage limits

```csharp
public async Task<IActionResult> ExportReportAsync()
{
    var (canUse, error) = await _featureService.CanUseFeatureAsync("AdvancedExport");

    if (!canUse)
    {
        return BadRequest(new { reason = error });
    }

    bool recorded = await _featureService.RecordFeatureUsageAsync("AdvancedExport");
    if (!recorded)
    {
        // Feature state changed between check and usage; abort gracefully.
        return StatusCode(409, "Feature state changed; please retry.");
    }

    // Proceed with export logic.
    var report = await GenerateReportAsync();
    return File(report, "application/pdf");
}
```

### Example 2: Tenant onboarding with default features and gradual rollout

```csharp
public async Task ProvisionTenantFeaturesAsync()
{
    // Seed defaults for a newly provisioned tenant.
    await _featureService.InitializeDefaultFeaturesAsync();

    // Enable a beta feature with a 10% rollout for early testing.
    await _featureService.EnableFeatureAsync("BetaDashboard");
    await _featureService.SetRolloutPercentageAsync("BetaDashboard", 10.0);

    // Verify the tenant's active feature set.
    var enabled = await _featureService.GetEnabledFeaturesAsync();
    foreach (var feature in enabled)
    {
        _logger.LogInformation("Tenant feature active: {Name} (Category: {Category})",
            feature.Name, feature.Category);
    }
}
```

## Notes

- **Tenant context:** Every method that interacts with tenant data relies on an ambient tenant context (typically resolved from the current HTTP request or a background job scope). Calling any method without an established tenant context results in a `TenantNotFoundException`.
- **Rollout consistency:** Rollout decisions use a consistent hash derived from the tenant identifier. A given tenant will consistently fall inside or outside the rollout window for a given percentage, avoiding flip-flopping between requests.
- **Usage tracking atomicity:** `RecordFeatureUsageAsync` and `CheckUsageLimitAsync` are designed to be called in sequence. However, concurrent requests may increment usage between a `CanUseFeatureAsync` check and the subsequent `RecordFeatureUsageAsync` call. The `CanUseFeatureAsync` method provides a snapshot evaluation; callers should handle the edge case where recording fails because the limit was reached by a parallel request.
- **Default initialization idempotency:** `InitializeDefaultFeaturesAsync` only inserts features that do not already exist for the tenant. Repeated calls are safe and will not duplicate or overwrite existing configurations.
- **Statistics object:** The return type of `GetStatisticsAsync` is `object` to allow the underlying implementation to evolve the statistics shape without breaking the public API. Consumers should cast or deserialize according to the documented schema of the specific storage provider in use.
- **Thread safety:** The service itself is stateless and relies on thread-safe backing stores. Concurrent calls from multiple threads or requests for the same tenant are safe at the service level; consistency guarantees depend on the underlying data store’s concurrency model.
