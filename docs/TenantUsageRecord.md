# TenantUsageRecord

Represents a tenant's current usage of a specific metric within a defined usage period, including quota limits and violation state.

## API

### Properties

- **`Id`** (Guid)
  A unique identifier for this usage record. Read-only.

- **`TenantId`** (Guid)
  The identifier of the tenant to which this usage record belongs. Read-only.

- **`MetricKey`** (string)
  The key identifying the metric being tracked (e.g., "storage", "api_calls"). Read-only.

- **`CurrentValue`** (long)
  The current measured value of the metric for the tenant in this period. Read-only.

- **`QuotaLimit`** (long?)
  The maximum allowed value for the metric in this period, if a quota is enforced. `null` if no quota is set. Read-only.

- **`Period`** (UsagePeriod)
  The usage period this record applies to (e.g., monthly, daily). Read-only.

- **`PeriodStart`** (DateTime)
  The start date and time of the current usage period. Read-only.

- **`ResetAt`** (DateTime?)
  The date and time when the usage will be reset (e.g., end of billing cycle). `null` if never reset. Read-only.

- **`CreatedAt`** (DateTime)
  The date and time when the record was created. Read-only.

- **`UpdatedAt`** (DateTime)
  The date and time when the record was last updated. Read-only.

- **`IsApproachingLimit`** (bool)
  Indicates whether the current usage is approaching the quota limit (e.g., within 10% of `QuotaLimit`). Read-only.

- **`IsAllowed`** (bool)
  Indicates whether the current usage is within the allowed quota (i.e., not exceeded and not approaching). Read-only.

- **`IsExceeded`** (bool)
  Indicates whether the current usage exceeds the quota limit. Read-only.

- **`UsagePercentage`** (double)
  The current usage as a percentage of the quota limit (0.0 to 100.0). Read-only.

- **`CurrentUsage`** (long)
  Alias for `CurrentValue`. Read-only.

- **`ViolationMessage`** (string?)
  A message describing any violation of quota limits, if applicable. `null` if no violation. Read-only.

### Methods

- **`Allow()`** (static)
  Returns a `QuotaCheckResult` indicating that the current usage is within allowed limits.
  **Returns:** A `QuotaCheckResult` representing an allowed state.

- **`Deny()`** (static)
  Returns a `QuotaCheckResult` indicating that the current usage exceeds allowed limits.
  **Returns:** A `QuotaCheckResult` representing a denied state.

## Usage
