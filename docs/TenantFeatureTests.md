# TenantFeatureTests

Unit tests for `TenantFeature` class, validating feature availability, usage limits, deprecation handling, and field-level access control logic within a multi-tenant isolation context.

## API

### `IsAvailable_WhenDisabled_ReturnsFalse`
Verifies that a disabled feature returns `false` regardless of other conditions.

### `IsAvailable_WhenDeprecatedBeforeNow_ReturnsFalse`
Ensures that a feature marked as deprecated before the current time is unavailable.

### `IsAvailable_WhenAvailableFromIsInFuture_ReturnsFalse`
Confirms that a feature scheduled for future availability is not yet accessible.

### `IsAvailable_WhenEnabledWithFullRollout_ReturnsTrue`
Validates that a fully enabled feature with complete rollout is available.

### `IsUsageLimitExceeded_WhenCurrentUsageEqualsLimit_ReturnsTrue`
Checks that usage is considered exceeded when current usage matches the defined limit.

### `IsUsageLimitExceeded_WhenUsageLimitIsNull_ReturnsFalse`
Ensures that a feature without a usage limit is never considered exceeded.

### `CanUseFeature_WhenDeprecated_ReturnsFalseWithDeprecationMessage`
Returns `false` and includes a deprecation message when the feature is deprecated.

### `CanUseFeature_WhenUsageLimitReached_ReturnsFalseWithLimitMessage`
Returns `false` with a usage limit message when the current usage exceeds or equals the limit.

### `CanUseFeature_WhenAvailableAndWithinLimits_ReturnsTrueWithNullError`
Returns `true` with no error message when the feature is available and usage is within limits.

### `RecordUsage_IncrementsCurrentUsageBySpecifiedAmount`
Increases the current usage counter by the provided amount.

### `ResetUsage_SetsCurrentUsageBackToZero`
Resets the usage counter to zero.

### `GetStatus_WhenDeprecatedInPast_ReturnsDeprecated`
Returns `Deprecated` status when the feature was deprecated in the past.

### `GetStatus_WhenDisabled_ReturnsDisabled`
Returns `Disabled` status when the feature is explicitly disabled.

### `GetStatus_WhenPartialRollout_ReturnsBetaWithPercentage`
Returns `Beta` status with the rollout percentage when the feature is partially rolled out.

### `GetStatus_WhenFullyActiveFeature_ReturnsActive`
Returns `Active` status when the feature is fully enabled and available.

### `GetAllowedFields_WithCommaSeparatedString_ReturnsListWithTrimmedEntries`
Parses a comma-separated string into a list of allowed field names, trimming whitespace.

### `GetDeniedFields_WhenNull_ReturnsEmptyList`
Returns an empty list when the denied fields input is `null`.

### `IsFieldAccessAllowed_WhenFieldInDeniedList_ReturnsFalse`
Returns `false` if the requested field is in the denied list.

### `IsFieldAccessAllowed_WhenFieldNotInAllowedList_ReturnsFalse`
Returns `false` if the field is not in the allowed list and no global access is granted.

### `IsFieldAccessAllowed_WhenNoRestrictions_ReturnsTrue`
Returns `true` when there are no field-level restrictions in place.

## Usage
