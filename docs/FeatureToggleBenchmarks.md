# FeatureToggleBenchmarks

`FeatureToggleBenchmarks` is a benchmarking utility class designed to measure and evaluate the performance characteristics of feature toggle operations within the `dotnet-tenant-isolation` project. It provides methods to simulate feature enablement, rollout percentage adjustments, usage recording, and statistical analysis under controlled conditions. This class is intended for use in performance testing scenarios to assess the efficiency and scalability of feature toggle mechanisms.

## API

### `public void Setup()`
Initializes the benchmarking environment. This method prepares the necessary state for subsequent benchmark operations, including resetting any internal counters or configurations.

**Throws:**
- May throw exceptions if initialization fails due to resource constraints or misconfiguration.

---

### `public async ValueTask<bool> IsFeatureEnabled_100Percent()`
Determines whether a feature is enabled with a 100% rollout percentage. This simulates a scenario where the feature is universally enabled for all tenants or users.

**Returns:**
- `ValueTask<bool>`: `true` if the feature is enabled, `false` otherwise.

**Throws:**
- May throw exceptions if the underlying feature toggle service encounters an error.

---

### `public async ValueTask<bool> IsFeatureEnabled_50Percent()`
Determines whether a feature is enabled with a 50% rollout percentage. This simulates a scenario where the feature is enabled for approximately half of the target audience.

**Returns:**
- `ValueTask<bool>`: `true` if the feature is enabled, `false` otherwise.

**Throws:**
- May throw exceptions if the underlying feature toggle service encounters an error.

---

### `public async ValueTask<bool> IsFeatureEnabled_25Percent()`
Determines whether a feature is enabled with a 25% rollout percentage. This simulates a scenario where the feature is enabled for a quarter of the target audience.

**Returns:**
- `ValueTask<bool>`: `true` if the feature is enabled, `false` otherwise.

**Throws:**
- May throw exceptions if the underlying feature toggle service encounters an error.

---

### `public async ValueTask EnableFeature()`
Enables the feature unconditionally, bypassing any rollout percentage constraints. This method is used to simulate a scenario where the feature is forcefully enabled for benchmarking purposes.

**Throws:**
- May throw exceptions if the feature toggle service fails to enable the feature.

---

### `public async ValueTask SetRolloutPercentage()`
Sets the rollout percentage for the feature to a predefined value (context-dependent). This method simulates adjusting the feature's availability dynamically during benchmarking.

**Throws:**
- May throw exceptions if the rollout percentage cannot be updated due to service errors.

---

### `public async ValueTask RecordFeatureUsage()`
Records a usage event for the feature. This method simulates tracking feature adoption or interaction metrics during benchmarking.

**Throws:**
- May throw exceptions if the usage recording fails.

---

### `public async ValueTask<int> GetStatistics()`
Retrieves statistical data about feature usage, such as the number of times the feature was enabled, disabled, or interacted with during the benchmark.

**Returns:**
- `ValueTask<int>`: An integer representing a statistical metric (e.g., count of feature checks or usage events).

**Throws:**
- May throw exceptions if statistical data cannot be retrieved.

---

### `public void Cleanup()`
Resets or releases resources allocated during benchmarking. This method ensures a clean state for subsequent benchmark runs.

**Throws:**
- May throw exceptions if cleanup operations fail (e.g., resource disposal errors).

---

### `public void Dispose()`
Releases all resources used by the `FeatureToggleBenchmarks` instance. This method should be called when the instance is no longer needed to prevent resource leaks.

**Throws:**
- May throw exceptions if disposal fails (e.g., pending asynchronous operations).

## Usage

### Example 1: Benchmarking Feature Toggle Performance
