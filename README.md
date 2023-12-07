// existing content ...

## FeatureToggleBenchmarks

The `FeatureToggleBenchmarks` class provides a set of benchmarks for evaluating the performance of feature toggle operations. It includes tests for feature enablement, rollout percentage, and statistics collection.

### Example Usage

```csharp
// Create a new instance of FeatureToggleBenchmarks
var featureToggleBenchmarks = new FeatureToggleBenchmarks();

// Set up the feature toggle
await featureToggleBenchmarks.Setup();

// Check if a feature is enabled with 100% rollout
var isFeatureEnabled = await featureToggleBenchmarks.IsFeatureEnabled_100Percent();

// Check if a feature is enabled with 50% rollout
var isFeatureEnabled50 = await featureToggleBenchmarks.IsFeatureEnabled_50Percent();

// Check if a feature is enabled with 25% rollout
var isFeatureEnabled25 = await featureToggleBenchmarks.IsFeatureEnabled_25Percent();

// Enable a feature
await featureToggleBenchmarks.EnableFeature();

// Set the rollout percentage
await featureToggleBenchmarks.SetRolloutPercentage(75);

// Record feature usage
await featureToggleBenchmarks.RecordFeatureUsage();

// Get statistics
var statistics = await featureToggleBenchmarks.GetStatistics();

// Clean up
featureToggleBenchmarks.Cleanup();

// Dispose of resources
featureToggleBenchmarks.Dispose();
```

// existing content ...
