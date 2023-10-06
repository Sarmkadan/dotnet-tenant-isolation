# TenantFeature
The `TenantFeature` type represents a feature that can be enabled or disabled for a specific tenant in a multi-tenant system. It provides information about the feature, such as its key, display name, description, and availability. This type is used to manage feature toggles and rollouts in a tenant-isolated environment.

## API
The `TenantFeature` type has the following public members:
* `Id`: A unique identifier for the feature.
* `TenantId`: The identifier of the tenant that this feature belongs to.
* `FeatureKey`: A unique key that identifies the feature.
* `DisplayName`: The display name of the feature.
* `Description`: A description of the feature.
* `IsEnabled`: A boolean indicating whether the feature is enabled.
* `Category`: The category of the feature.
* `RolloutPercentage`: The percentage of rollout for the feature.
* `AvailabilityLevel`: The availability level of the feature.
* `AvailableFrom`: The date and time when the feature becomes available.
* `DeprecatedAt`: The date and time when the feature is deprecated.
* `UsageLimit`: The usage limit for the feature.
* `CurrentUsage`: The current usage of the feature.
* `Metadata`: Additional metadata for the feature.
* `CreatedAt`: The date and time when the feature was created.
* `UpdatedAt`: The date and time when the feature was last updated.
* `Tenant`: The tenant that this feature belongs to.
* `IsAvailable`: A boolean indicating whether the feature is available.
* `IsUsageLimitExceeded`: A boolean indicating whether the usage limit is exceeded.
* `CanUseFeature`: A boolean indicating whether the feature can be used.

## Usage
Here are two examples of using the `TenantFeature` type:
```csharp
// Example 1: Create a new TenantFeature
var feature = new TenantFeature
{
    Id = Guid.NewGuid(),
    TenantId = Guid.NewGuid(),
    FeatureKey = "my-feature",
    DisplayName = "My Feature",
    Description = "This is my feature",
    IsEnabled = true,
    Category = "My Category",
    RolloutPercentage = 100,
    AvailabilityLevel = "GA",
    AvailableFrom = DateTime.UtcNow,
    UsageLimit = 1000,
    CurrentUsage = 0,
    Metadata = "{\"key\":\"value\"}",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// Example 2: Check if a feature is available and can be used
var feature = GetTenantFeature("my-feature");
if (feature.IsAvailable && feature.CanUseFeature)
{
    Console.WriteLine("The feature is available and can be used");
}
else
{
    Console.WriteLine("The feature is not available or cannot be used");
}
```

## Notes
Note that the `TenantFeature` type does not provide any thread-safety guarantees. If multiple threads are accessing the same `TenantFeature` instance, it is the responsibility of the caller to ensure thread safety. Additionally, the `UsageLimit` and `CurrentUsage` properties are not automatically updated when the feature is used. It is the responsibility of the caller to update these properties accordingly. The `IsAvailable` and `CanUseFeature` properties are computed based on the `AvailableFrom`, `DeprecatedAt`, `UsageLimit`, and `CurrentUsage` properties, and may not reflect the actual availability of the feature in all cases.
