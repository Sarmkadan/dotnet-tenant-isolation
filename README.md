// existing content ...

## FeaturesControllerExtensions

The `FeaturesControllerExtensions` class provides extension methods for managing feature flags and configurations in a tenant-isolated environment. It enables checking feature status, retrieving configuration details, and controlling feature rollout via percentage-based activation or usage-based disabling.

### Example Usage

```csharp
// Check if a feature is enabled for the current tenant
bool isEnabled = await FeaturesControllerExtensions.IsFeatureEnabledAsync("feature-flag-name");

// Get detailed configuration for a feature
var config = await FeaturesControllerExtensions.GetFeatureConfigurationAsync("feature-flag-name");
if (config?.IsEnabled)
{
    Console.WriteLine($"Feature is active with {config.RolloutPercentage}% rollout");
    Console.WriteLine($"Used {config.UsageCount}/{config.UsageLimit} times");
}

// Enable feature with 50% rollout
bool success = await FeaturesControllerExtensions.EnableFeatureWithPercentageAsync("feature-flag-name", 50);
if (success)
{
    Console.WriteLine($"Feature enabled with {config.RolloutPercentage}% rollout");
}

// Disable feature after usage threshold
bool disabled = await FeaturesControllerExtensions.DisableFeatureWithUsageAsync("feature-flag-name", 1000);
if (disabled) 
{
    Console.WriteLine($"Feature disabled due to reaching usage limits");
}
```

## TenantApiControllerExtensions
The `TenantApiControllerExtensions` class provides extension methods for managing tenants in a tenant-isolated environment. It enables retrieving tenants by IDs or status, bulk activating or suspending tenants, and getting dashboard statistics. 
Here is an example of how to use it:
```csharp
// Get tenants by IDs
var result = await TenantApiControllerExtensions.GetTenantsByIds(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

// Get tenants by status
var tenantsByStatus = await TenantApiControllerExtensions.GetTenantsByStatus("active");

// Bulk activate tenants
var activationResult = await TenantApiControllerExtensions.BulkActivateTenants(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }, "reason for activation");

// Get dashboard statistics
var statistics = await TenantApiControllerExtensions.GetDashboardStatistics();
```

// existing content ...
