# Data isolation policy validator

`DataIsolationPolicyValidator` validates `DataIsolationPolicy` instances and returns structured `PolicyValidationResult` objects. It checks both the policy's own fields and, when requested, the database state needed to enforce the policy safely.

The validator is registered as the scoped `IDataIsolationPolicyValidator` implementation. It reports every error it finds; validation does not stop after the first failure.

## Validation entry points

| Method | Scope | Result |
| --- | --- | --- |
| `ValidateFields(policy)` | Database-independent field and consistency checks only | `PolicyValidationResult` |
| `ValidateAsync(policy, cancellationToken)` | All field checks, plus the tenant connection-string and sibling-policy checks | `Task<PolicyValidationResult>` |
| `ValidateAllAsync(cancellationToken)` | Loads every stored policy and calls `ValidateAsync` for each one | `Task<PolicyValidationReport>` |

All three methods preserve individual errors rather than reducing validation to a Boolean. Passing `null` to either single-policy method throws `ArgumentNullException`. Database operations in the asynchronous methods observe the supplied cancellation token.

The legacy `DataIsolationPolicyValidation.ValidateStructured()` extension uses the same field-validation implementation as `ValidateFields`; it does not perform database-aware checks. The older `Validate()` and `EnsureValid()` compatibility methods convert those field errors to display text.

## Field-level policies and error codes

These checks are performed by both `ValidateFields` and `ValidateAsync`.

| Error code | Field | Validation condition |
| --- | --- | --- |
| `MissingId` | `Id` | `Id` is `Guid.Empty`. |
| `MissingTenantId` | `TenantId` | `TenantId` is `Guid.Empty`. |
| `MissingEntityType` | `EntityType` | The value is `null`, empty, or whitespace. |
| `EntityTypeTooLong` | `EntityType` | The value contains more than 100 characters. This is checked only when the value is not blank. |
| `InvalidPolicyType` | `PolicyType` | The numeric value is not defined by `DataIsolationPolicyType`. |
| `PriorityOutOfRange` | `Priority` | The value is less than 0 or greater than 1000. Both endpoints are valid. |
| `InvalidCreatedAt` | `CreatedAt` | The value is `default(DateTime)`, or is later than five minutes after the validator's current UTC time. |
| `InvalidUpdatedAt` | `UpdatedAt` | The value is `default(DateTime)`, or is later than five minutes after the validator's current UTC time. |
| `MissingFilterRule` | `FilterRule` | `PolicyType` is `Custom` and the filter rule is `null`, empty, or whitespace. |
| `InvalidFieldList` | `AllowedFields` | A nonblank comma-separated list produces an empty or whitespace field name after its entries are trimmed. |
| `InvalidFieldList` | `DeniedFields` | A nonblank comma-separated list produces an empty or whitespace field name after its entries are trimmed. |
| `InvalidCrossTenantAccessFormat` | `AllowedCrossTenantAccess` | Any non-empty comma-separated entry, after trimming, cannot be parsed as a GUID. |
| `ConflictingFieldRules` | `AllowedFields` | At least one field appears in both the allowed and denied lists. Comparison is case-insensitive. The error message names the overlapping fields. |
| `DescriptionTooLong` | `Description` | A nonblank description contains more than 1,000 characters. |
| `FilterRuleTooLong` | `FilterRule` | A nonblank filter rule contains more than 10,000 characters. |
| `ConflictingIsolationMode` | `AllowedCrossTenantAccess` | `PolicyType` is `Strict` while `AllowedCrossTenantAccess` is populated with a nonblank value. Strict policies never permit cross-tenant access. |

`InvalidCreatedAt` and `InvalidUpdatedAt` deliberately allow up to five minutes of positive clock skew. Because the unset and future checks are mutually exclusive for each timestamp, only one error is returned for a given timestamp field.

Field lists are parsed with `StringSplitOptions.RemoveEmptyEntries` and each retained entry is trimmed. Consequently, empty comma segments are discarded, while an entry containing only whitespace can produce `InvalidFieldList`. Cross-tenant access entries use the same empty-segment behavior before GUID parsing.

## Database-aware policies and error codes

`ValidateAsync` adds these checks after collecting all field-level errors:

| Error code | Field | Validation condition |
| --- | --- | --- |
| `MissingConnectionString` | `TenantId` | No active `TenantConnectionString` exists for the policy's `TenantId`. The check applies whether the policy itself is active or inactive. |
| `ConflictingIsolationMode` | `PolicyType` | The policy is active and another active policy has the same `TenantId` and exact `EntityType`, a different `Id`, and a different `PolicyType`. |

The sibling-policy lookup does not run for an inactive policy. It excludes the policy's own ID, and the entity-type comparison follows the database provider's configured string comparison behavior. A policy can receive more than one `ConflictingIsolationMode` error—for example, a strict policy can both declare cross-tenant access and conflict with a differently typed active sibling.

Database checks still run when field validation has already failed. Callers should therefore expect a result containing both intrinsic and database-aware errors.

## Results and reports

Each failure is a `PolicyValidationError` with:

- `Code`: a `PolicyErrorCode` intended for programmatic branching;
- `Message`: a human-readable explanation;
- `Field`: the related `DataIsolationPolicy` property name.

`PolicyValidationResult.IsValid` is `true` only when `Errors` is empty. `ToDisplayString()` formats failures as `Code: Message` lines; consumers should use `Code`, not parse this display text.

`ValidateAllAsync` reads all policies without tracking, validates them sequentially, and returns a `PolicyValidationReport`. Each report entry includes the policy ID, tenant ID, entity type, and its complete `PolicyValidationResult`. An empty policy collection is valid. `FailedEntries` contains only invalid entries, and `ValidatedAt` records when the report was created in UTC.

## Startup and change validation

When the event bus is enabled, service registration also adds `DataIsolationPolicyStartupValidator` as a hosted service. At application startup it calls `ValidateAllAsync`; any invalid policy causes an `InvalidOperationException` containing the aggregated report and prevents startup from succeeding.

After a successful startup check, the hosted service subscribes to `DataIsolationPolicyChangedEvent`. A policy-change event triggers another full validation pass. Failures discovered after startup are logged but do not terminate the running application.

`ValidateAsync` logs a failed single-policy validation as a warning. `ValidateFields` does not log. A failed aggregate validation is logged as an error; a successful aggregate validation is logged as information.
