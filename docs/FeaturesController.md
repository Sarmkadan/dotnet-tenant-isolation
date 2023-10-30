# FeaturesController

ASP.NET Core controller exposing HTTP endpoints for managing feature flags within a multi-tenant isolation context. Provides operations to query, enable, disable, and configure feature rollout percentages, record usage metrics, enforce usage limits, and initialize default feature configurations per tenant.

## API

### `public FeaturesController()`
Initializes a new instance of the `FeaturesController` class. Dependencies are resolved via constructor injection.

### `public async Task<IActionResult> IsFeatureEnabled`
Checks whether a specific feature is enabled for the current tenant context.

**Parameters**  
- `featureName` (string, from route or query): The name of the feature to check.  
- `tenantId` (string, from header or claim): The tenant identifier resolved from the request context.

**Returns**  
- `200 OK` with `bool` indicating enabled state.  
- `404 NotFound` if the feature does not exist.  
- `400 BadRequest` if `featureName` is missing or invalid.

**Throws**  
- `ArgumentNullException` if `featureName` is null.  
- `InvalidOperationException` if tenant context cannot be resolved.

---

### `public async Task<IActionResult> GetFeature`
Retrieves the full configuration of a single feature for the current tenant.

**Parameters**  
- `featureName` (string, from route): The name of the feature to retrieve.  
- `tenantId` (string, from header or claim): The tenant identifier.

**Returns**  
- `200 OK` with feature definition object (name, enabled state, rollout percentage, usage limits).  
- `404 NotFound` if the feature does not exist.

**Throws**  
- `ArgumentNullException` if `featureName` is null.  
- `KeyNotFoundException` if feature is not registered.

---

### `public async Task<IActionResult> GetEnabledFeatures`
Lists all features currently enabled for the current tenant.

**Parameters**  
- `tenantId` (string, from header or claim): The tenant identifier.

**Returns**  
- `200 OK` with array of enabled feature definitions.  
- `204 NoContent` if no features are enabled.

**Throws**  
- `InvalidOperationException` if tenant context cannot be resolved.

---

### `public async Task<IActionResult> GetAllFeatures`
Retrieves all registered features regardless of enabled state for the current tenant.

**Parameters**  
- `tenantId` (string, from header or claim): The tenant identifier.

**Returns**  
- `200 OK` with array of all feature definitions.  
- `204 NoContent` if no features are registered.

**Throws**  
- `InvalidOperationException` if tenant context cannot be resolved.

---

### `public async Task<IActionResult> EnableFeature`
Enables a feature for the current tenant, optionally with a rollout percentage.

**Parameters**  
- `featureName` (string, from route): The name of the feature to enable.  
- `tenantId` (string, from header or claim): The tenant identifier.  
- `percentage` (int, from body or query, optional): Rollout percentage (0-100). Defaults to 100.

**Returns**  
- `200 OK` with updated feature definition.  
- `404 NotFound` if the feature does not exist.  
- `400 BadRequest` if `percentage` is outside 0-100 range.

**Throws**  
- `ArgumentNullException` if `featureName` is null.  
- `ArgumentOutOfRangeException` if `percentage` is invalid.

---

### `public async Task<IActionResult> DisableFeature`
Disables a feature for the current tenant.

**Parameters**  
- `featureName` (string, from route): The name of the feature to disable.  
- `tenantId` (string, from header or claim): The tenant identifier.

**Returns**  
- `200 OK` with updated feature definition.  
- `404 NotFound` if the feature does not exist.

**Throws**  
- `ArgumentNullException` if `featureName` is null.

---

### `public async Task<IActionResult> SetRolloutPercentage`
Updates the rollout percentage for an already-enabled feature.

**Parameters**  
- `featureName` (string, from route): The name of the feature.  
- `tenantId` (string, from header or claim): The tenant identifier.  
- `percentage` (int, from body): New rollout percentage (0-100).

**Returns**  
- `200 OK` with updated feature definition.  
- `404 NotFound` if the feature does not exist or is not enabled.  
- `400 BadRequest` if `percentage` is outside 0-100 range.

**Throws**  
- `ArgumentNullException` if `featureName` is null.  
- `ArgumentOutOfRangeException` if `percentage` is invalid.  
- `InvalidOperationException` if feature is not currently enabled.

---

### `public async Task<IActionResult> RecordUsage`
Records a usage event for a metered feature.

**Parameters**  
- `featureName` (string, from route): The name of the feature.  
- `tenantId` (string, from header or claim): The tenant identifier.  
- `amount` (long, from body): The usage quantity to record (must be positive).

**Returns**  
- `200 OK` with updated usage statistics.  
- `404 NotFound` if the feature does not exist.  
- `400 BadRequest` if `amount` is not positive.

**Throws**  
- `ArgumentNullException` if `featureName` is null.  
- `ArgumentOutOfRangeException` if `amount` <= 0.

---

### `public async Task<IActionResult> CheckUsageLimit`
Verifies whether a proposed usage amount would exceed the configured limit for a feature.

**Parameters**  
- `featureName` (string, from route): The name of the feature.  
- `tenantId` (string, from header or claim): The tenant identifier.  
- `amount` (long, from query or body): The proposed usage amount to check.

**Returns**  
- `200 OK` with `{ allowed: bool, currentUsage: long, limit: long, remaining: long }`.  
- `404 NotFound` if the feature does not exist or has no limit configured.

**Throws**  
- `ArgumentNullException` if `featureName` is null.  
- `ArgumentOutOfRangeException` if `amount` < 0.

---

### `public async Task<IActionResult> GetStatistics`
Retrieves aggregated usage statistics for a feature within the current tenant.

**Parameters**  
- `featureName` (string, from route): The name of the feature.  
- `tenantId` (string, from header or claim): The tenant identifier.  
- `from` (DateTime, from query, optional): Start of time range. Defaults to 30 days ago.  
- `to` (DateTime, from query, optional): End of time range. Defaults to now.

**Returns**  
- `200 OK` with statistics object (total usage, daily breakdown, limit utilization).  
- `404 NotFound` if the feature does not exist.

**Throws**  
- `ArgumentNullException` if `featureName` is null.  
- `ArgumentException` if `from` > `to`.

---

### `public async Task<IActionResult> InitializeDefaults`
Creates or resets the default feature set for the current tenant.

**Parameters**  
- `tenantId` (string, from header or claim): The tenant identifier.  
- `overwrite` (bool, from query, optional): Whether to overwrite existing features. Defaults to false.

**Returns**  
- `200 OK` with list of initialized feature definitions.  
- `409 Conflict` if `overwrite` is false and features already exist.

**Throws**  
- `InvalidOperationException` if tenant context cannot be resolved.  
- `InvalidOperationException` if defaults cannot be determined.

---

### `public int Percentage`
Gets or sets the rollout percentage value used in feature enable/rollout operations. Valid range is 0 to 100 inclusive. Used as a shorthand parameter binding target for `EnableFeature` and `SetRolloutPercentage`.

**Remarks**  
Model binding populates this property from request body or query string. Validation occurs in the action methods.

---

### `public long Amount`
Gets or sets the usage amount value used in `RecordUsage` and `CheckUsageLimit` operations. Must be a positive integer for recording; non-negative for limit checks.

**Remarks**  
Model binding populates this property from request body or query string. Validation occurs in the action methods.

## Usage

### Example 1: Enable a feature with gradual rollout
```csharp
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

public async Task EnableFeatureForTenantAsync(HttpClient client, string tenantId, string featureName, int rolloutPercent)
{
    client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);

    var response = await client.PostAsJsonAsync(
        $"/api/features/{featureName}/enable",
        new { percentage = rolloutPercent });

    response.EnsureSuccessStatusCode();

    var feature = await response.Content.ReadFromJsonAsync<FeatureDefinition>();
    Console.WriteLine($"Feature '{feature.Name}' enabled at {feature.RolloutPercentage}% for tenant {tenantId}");
}

public record FeatureDefinition(string Name, bool Enabled, int RolloutPercentage, long? UsageLimit);
```

### Example 2: Record usage and check limit before allowing an operation
```csharp
public async Task<bool> TryConsumeFeatureAsync(HttpClient client, string tenantId, string featureName, long units)
{
    client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);

    var checkResponse = await client.GetAsync($"/api/features/{featureName}/usage/check?amount={units}");
    if (!checkResponse.IsSuccessStatusCode)
    {
        return false; // Feature not found or no limit configured
    }

    var checkResult = await checkResponse.Content.ReadFromJsonAsync<UsageCheckResult>();
    if (!checkResult.Allowed)
    {
        return false; // Would exceed limit
    }

    var recordResponse = await client.PostAsJsonAsync(
        $"/api/features/{featureName}/usage/record",
        new { amount = units });

    return recordResponse.IsSuccessStatusCode;
}

public record UsageCheckResult(bool Allowed, long CurrentUsage, long Limit, long Remaining);
```

## Notes

- **Tenant resolution**: All endpoints require a valid tenant context, typically supplied via `X-Tenant-Id` header or JWT claim. Missing or invalid tenant context results in `400 BadRequest` or `401 Unauthorized` depending on middleware configuration.
- **Thread safety**: Controller instances are created per request (transient). Shared feature state is managed by injected services (e.g., `IFeatureStore`, `IUsageTracker`) which must implement their own synchronization. The `Percentage` and `Amount` properties are request-scoped and not shared across threads.
- **Idempotency**: `EnableFeature`, `DisableFeature`, and `SetRolloutPercentage` are idempotent—repeated calls with the same parameters produce the same state. `RecordUsage` is not idempotent; each call increments the counter.
- **Rollout semantics**: A rollout percentage of 0 means disabled for all users; 100 means enabled for all. The evaluation algorithm is deterministic per user/tenant (typically hash-based) and implemented in the feature evaluation service, not in this controller.
- **Usage limits**: Limits are enforced at the application layer. `CheckUsageLimit` performs a read-only check; race conditions between check and record are possible under high concurrency. Consider using `RecordUsage` with a conditional limit check in the storage layer for strict enforcement.
- **Default initialization**: `InitializeDefaults` is intended for tenant onboarding. The default feature set is defined by the `IFeatureDefaultsProvider` implementation. Calling with `overwrite=false` on an already-initialized tenant returns `409 Conflict`.
- **Validation**: Input validation for `Percentage` (0-100) and `Amount` (>0 for recording, >=0 for checks) occurs in action methods. Model binding errors return `400 BadRequest` with validation details.
- **Caching**: `GetEnabledFeatures` and `GetAllFeatures` may return cached data depending on `IFeatureStore` implementation. Cache invalidation occurs on enable/disable/rollout changes.
