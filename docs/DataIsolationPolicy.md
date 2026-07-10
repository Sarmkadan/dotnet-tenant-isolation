# DataIsolationPolicy

Represents a configurable data isolation rule that governs how entities of a specific type are filtered, masked, or restricted per tenant. Each policy binds a `PolicyType` to an `EntityType`, optionally specifying field-level allow/deny lists, cross-tenant access rules, and a priority for conflict resolution. The class provides helper methods to inspect parsed field lists and evaluate whether field access or cross-tenant access is permitted under the current policy.

## API

### Properties

#### `Guid Id`
Unique identifier for the policy record.

#### `Guid TenantId`
Foreign key referencing the tenant that owns this policy.

#### `DataIsolationPolicyType PolicyType`
Enum indicating the isolation strategy applied to the target entity (e.g., `Filter`, `Mask`, `Block`).

#### `string EntityType`
The fully qualified name or logical type identifier of the entity to which this policy applies.

#### `string? Description`
Optional human-readable explanation of the policy’s purpose.

#### `string? FilterRule`
A serialized filter expression (e.g., JSON or a query string) used when `PolicyType` involves row-level filtering. Null when not applicable.

#### `string? AllowedFields`
A delimited string of field names that are explicitly permitted for access. Parsed by `GetAllowedFields`.

#### `string? DeniedFields`
A delimited string of field names that are explicitly denied for access. Parsed by `GetDeniedFields`.

#### `string? AllowedCrossTenantAccess`
A delimited string of tenant identifiers or patterns that are permitted to bypass isolation for this entity. Evaluated by `IsCrossTenantAccessAllowed`.

#### `bool IsActive`
Indicates whether the policy is currently enforced. Inactive policies are ignored during isolation evaluation.

#### `int Priority`
Determines the evaluation order when multiple policies target the same entity type. Lower numeric values indicate higher priority.

#### `DateTime CreatedAt`
Timestamp of policy creation (UTC).

#### `DateTime UpdatedAt`
Timestamp of the last modification (UTC).

#### `virtual Tenant? Tenant`
Navigation property to the owning `Tenant` entity. May be null if not eagerly loaded.

### Methods

#### `List<string> GetAllowedFields`
Returns a `List<string>` parsed from the `AllowedFields` string. If `AllowedFields` is null or whitespace, the list is empty. The delimiter and parsing rules are implementation-defined but typically split on commas or semicolons with whitespace trimming.

#### `List<string> GetDeniedFields`
Returns a `List<string>` parsed from the `DeniedFields` string. If `DeniedFields` is null or whitespace, the list is empty. Delimiter handling mirrors `GetAllowedFields`.

#### `bool IsFieldAccessAllowed`
Evaluates whether access to a specific field is permitted by checking the field against the allowed and denied lists. The exact signature and logic depend on internal implementation; typically accepts a field name parameter and returns `true` if the field is not denied and either the allowed list is empty or the field appears in it. Throws `ArgumentNullException` if a required field name argument is null.

#### `bool IsCrossTenantAccessAllowed`
Determines whether a given tenant identifier is permitted cross-tenant access based on the `AllowedCrossTenantAccess` list. Expects a tenant identifier parameter. Returns `true` if the identifier matches an entry in the parsed list; otherwise `false`. Throws `ArgumentNullException` if the identifier argument is null.

#### `bool IsValidPolicy`
Validates the policy’s structural integrity. Returns `true` when `EntityType` is non-null and non-empty, `PolicyType` is a defined enum value, and any required fields for the selected `PolicyType` (such as `FilterRule` for filter-based policies) are present. Returns `false` otherwise. Does not throw.

## Usage

### Example 1: Registering and validating a field-masking policy

```csharp
var policy = new DataIsolationPolicy
{
    Id = Guid.NewGuid(),
    TenantId = tenant.Id,
    PolicyType = DataIsolationPolicyType.Mask,
    EntityType = "Customer",
    AllowedFields = "Id,Name,Email",
    DeniedFields = "TaxId,BankAccount",
    IsActive = true,
    Priority = 10,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

if (!policy.IsValidPolicy())
{
    throw new InvalidOperationException("Policy configuration is invalid.");
}

bool canAccessTaxId = policy.IsFieldAccessAllowed("TaxId"); // false
bool canAccessName = policy.IsFieldAccessAllowed("Name");   // true
```

### Example 2: Evaluating cross-tenant access with priority ordering

```csharp
var policies = new List<DataIsolationPolicy>
{
    new DataIsolationPolicy
    {
        PolicyType = DataIsolationPolicyType.Filter,
        EntityType = "Order",
        AllowedCrossTenantAccess = "TenantB,TenantC",
        Priority = 1,
        IsActive = true,
        FilterRule = "{\"tenant_id\": \"{current}\"}"
    },
    new DataIsolationPolicy
    {
        PolicyType = DataIsolationPolicyType.Block,
        EntityType = "Order",
        Priority = 5,
        IsActive = true
    }
};

var applicablePolicy = policies
    .Where(p => p.IsActive && p.EntityType == "Order")
    .OrderBy(p => p.Priority)
    .FirstOrDefault();

if (applicablePolicy != null && applicablePolicy.IsCrossTenantAccessAllowed("TenantB"))
{
    // Allow cross-tenant query for TenantB under the highest-priority policy
}
```

## Notes

- `GetAllowedFields` and `GetDeniedFields` always return a non-null list; callers do not need to guard against null returns.
- `IsFieldAccessAllowed` and `IsCrossTenantAccessAllowed` may throw `ArgumentNullException` when passed null arguments. Always validate inputs before calling.
- `IsValidPolicy` performs only structural checks. It does not verify that referenced tenants exist or that `FilterRule` is syntactically correct for the underlying query engine.
- The `AllowedFields` and `DeniedFields` properties store raw strings. If multiple threads mutate these strings while another thread calls the parsing methods, the results may be inconsistent. Synchronize writes to policy properties and reads of parsed lists if concurrent modification is possible.
- Priority evaluation is the caller’s responsibility. This class does not resolve conflicts between overlapping policies; it merely exposes `Priority` for external ordering.
- Navigation property `Tenant` is virtual and may be null if lazy loading is disabled or the relationship is not included in a query. Production code should null-check before dereferencing.
