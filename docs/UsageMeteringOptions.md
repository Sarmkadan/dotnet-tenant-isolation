# UsageMeteringOptions

Configuration options for tenant usage metering in multi-tenant applications. Controls quota thresholds, measurement periods, and behavior when limits are exceeded.

## API

### `WarningThresholdPercent`
- **Purpose**: Specifies the percentage of the quota at which a warning event is triggered (e.g., 80 means warn at 80% of quota).
- **Type**: `int`
- **Range**: Valid values are between `0` and `100` (inclusive).
- **Default**: `80`
- **Throws**: `ArgumentOutOfRangeException` if set outside the valid range.

### `DefaultPeriod`
- **Purpose**: Defines the default time window for usage aggregation (e.g., daily, weekly).
- **Type**: `UsagePeriod`
- **Default**: `UsagePeriod.Daily`
- **Remarks**: Affects how frequently usage metrics are reset and evaluated.

### `ThrowOnQuotaExceeded`
- **Purpose**: Determines whether exceeding the quota throws an exception or logs a warning.
- **Type**: `bool`
- **Default**: `true`
- **Remarks**: When `false`, exceeding the quota only logs a warning; operations continue.

### `MaxMetricsPerTenant`
- **Purpose**: Limits the number of distinct metrics tracked per tenant to prevent unbounded growth.
- **Type**: `int`
- **Default**: `1000`
- **Throws**: `ArgumentOutOfRangeException` if set to a negative value.
- **Remarks**: Older metrics are evicted when the limit is reached.

### `AddTenantUsageMetering(IServiceCollection)`
- **Purpose**: Registers tenant usage metering services with the dependency injection container using default options.
- **Parameters**:
  - `services`: The `IServiceCollection` to configure.
- **Returns**: The configured `IServiceCollection` for method chaining.
- **Remarks**: Uses `WarningThresholdPercent = 80`, `DefaultPeriod = UsagePeriod.Daily`, `ThrowOnQuotaExceeded = true`, and `MaxMetricsPerTenant = 1000`.

### `AddTenantUsageMetering(IServiceCollection, Action<UsageMeteringOptions>)`
- **Purpose**: Registers tenant usage metering services with custom configuration.
- **Parameters**:
  - `services`: The `IServiceCollection` to configure.
  - `configure`: An `Action<UsageMeteringOptions>` to customize options.
- **Returns**: The configured `IServiceCollection` for method chaining.
- **Remarks**: Throws `ArgumentNullException` if `configure` is `null`.

### `AddTenantUsageMetering<TImplementation>(IServiceCollection)`
- **Purpose**: Registers tenant usage metering services with a custom implementation of the metering logic.
- **Parameters**:
  - `services`: The `IServiceCollection` to configure.
- **Type Parameter**: `TImplementation` must implement `ITenantUsageMetering`.
- **Returns**: The configured `IServiceCollection` for method chaining.
- **Throws**: `InvalidOperationException` if `TImplementation` does not have a public parameterless constructor.
- **Remarks**: Overrides the default metering implementation with `TImplementation`.

## Usage

### Basic Setup
