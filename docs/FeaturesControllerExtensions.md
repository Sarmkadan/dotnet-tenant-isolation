# FeaturesControllerExtensions

`FeaturesControllerExtensions` provides a set of asynchronous extension methods and instance properties for managing feature flags with rollout percentages and usage-based limits. It supports querying feature state, retrieving configuration, enabling features with percentage-based rollouts, and disabling features when usage thresholds are exceeded. The type is designed for multi-tenant environments where feature availability must be controlled dynamically based on both probabilistic distribution and consumption tracking.

## API

### IsFeatureEnabledAsync

```csharp
public static async Task<bool> IsFeatureEnabledAsync(/* parameters omitted */)
```

Determines whether a feature is currently enabled for the calling context. The method evaluates both the rollout percentage and any usage limits that may apply. Returns `true` if the feature is active and within its usage constraints; otherwise `false`. This method may throw if the underlying feature configuration store is unreachable or if the feature identifier is invalid.

### GetFeatureConfigurationAsync

```csharp
public static async Task<FeatureConfiguration?> GetFeatureConfigurationAsync(/* parameters omitted */)
```

Retrieves the full configuration object for a specified feature. Returns a `FeatureConfiguration` instance if the feature exists in the store, or `null` if no configuration is found. The returned object includes rollout percentage, usage count, usage limit, and last-used timestamp. Throws when the configuration store cannot be accessed or the request is malformed.

### EnableFeatureWithPercentageAsync

```csharp
public static async Task<bool> EnableFeatureWithPercentageAsync(/* parameters omitted */)
```

Enables a feature and sets its rollout percentage. The percentage determines the proportion of requests that will see the feature as enabled. Returns `true` if the feature was successfully enabled or updated; returns `false` if the operation could not be committed. Throws when the percentage value is outside the valid range (typically 0–100) or when the configuration store rejects the update.

### DisableFeatureWithUsageAsync

```csharp
public static async Task<bool> DisableFeatureWithUsageAsync(/* parameters omitted */)
```

Disables a feature based on usage exhaustion. This method is typically invoked when the usage count meets or exceeds the usage limit. Returns `true` if the feature was successfully disabled; returns `false` if the feature was already disabled or the update failed. Throws when the configuration store is unavailable or the feature identifier is unrecognized.

### IsEnabled

```csharp
public bool IsEnabled { get; }
```

Indicates whether the feature is currently enabled. This property reflects the evaluated state after considering rollout percentage and usage constraints. Read-only.

### RolloutPercentage

```csharp
public int RolloutPercentage { get; }
```

The percentage of traffic or requests that should receive the enabled feature. A value of 0 means the feature is effectively disabled for all users; 100 means it is enabled for all eligible users. Read-only.

### UsageCount

```csharp
public long UsageCount { get; }
```

The number of times the feature has been consumed or invoked since tracking began. This counter is incremented atomically on each usage. Read-only.

### UsageLimit

```csharp
public long UsageLimit { get; }
```

The maximum number of usages allowed before the feature is automatically disabled. When `UsageCount` reaches or exceeds this value, `IsEnabled` returns `false`. Read-only.

### LastUsed

```csharp
public DateTime? LastUsed { get; }
```

The timestamp of the most recent feature usage, or `null` if the feature has never been used. Read-only.

## Usage

### Example 1: Checking and consuming a feature with usage limits

```csharp
var controller = new FeaturesController();

// Check if the "PremiumExport" feature is available
bool isEnabled = await controller.IsFeatureEnabledAsync("PremiumExport");
if (isEnabled)
{
    // Perform the premium export operation
    await ExportDataAsync();

    // The usage count is incremented internally; check if limit is approaching
    var config = await controller.GetFeatureConfigurationAsync("PremiumExport");
    if (config != null && config.UsageCount >= config.UsageLimit - 10)
    {
        // Notify admin that usage limit is nearly exhausted
        await NotifyAdminAsync("PremiumExport usage nearing limit.");
    }
}
```

### Example 2: Enabling a feature with percentage rollout and disabling on exhaustion

```csharp
var controller = new FeaturesController();

// Enable "DarkMode" for 25% of tenants
bool enabled = await controller.EnableFeatureWithPercentageAsync("DarkMode", 25);
if (enabled)
{
    Console.WriteLine("DarkMode feature enabled with 25% rollout.");
}

// Later, when usage limit is reached, disable the feature
var config = await controller.GetFeatureConfigurationAsync("DarkMode");
if (config != null && config.UsageCount >= config.UsageLimit)
{
    bool disabled = await controller.DisableFeatureWithUsageAsync("DarkMode");
    if (disabled)
    {
        Console.WriteLine("DarkMode disabled due to usage exhaustion.");
    }
}
```

## Notes

- **Percentage evaluation**: The `RolloutPercentage` is typically evaluated using a hash of the tenant or user identifier modulo 100. This ensures deterministic assignment per tenant rather than random per-request behavior.
- **Usage count atomicity**: `UsageCount` is incremented atomically. In high-concurrency scenarios, the count may briefly exceed `UsageLimit` before the disabling logic takes effect. Callers should tolerate transient overshoot.
- **Null configuration**: `GetFeatureConfigurationAsync` returns `null` for features that have never been configured. Callers must guard against null before accessing properties like `UsageLimit` or `RolloutPercentage`.
- **Thread safety**: The static async methods are safe to call concurrently. Instance properties reflect a snapshot of state at the time of reading and are not synchronized with ongoing updates. Do not rely on property values remaining consistent across multiple reads without re-fetching the configuration.
- **LastUsed tracking**: `LastUsed` is updated only when the feature is actually consumed, not when `IsFeatureEnabledAsync` is called. A feature may be enabled but have a `null` `LastUsed` if it has never been invoked.
- **Disable on exhaustion**: `DisableFeatureWithUsageAsync` is idempotent; calling it when the feature is already disabled returns `false` and does not throw.
