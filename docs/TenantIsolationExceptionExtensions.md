# TenantIsolationExceptionExtensions

Provides extension methods for enriching `TenantIsolationException` instances with contextual information, error codes, and structured details to support debugging and error handling in multi-tenant applications.

## API

### WithDetail

Appends a single detail message to the exception's detail collection.

**Parameters**
- `exception` (`TenantIsolationException`): The target exception instance.
- `detail` (`string`): The detail message to add.

**Returns**
- `TenantIsolationException`: The modified exception instance.

**Exceptions**
- `ArgumentNullException`: Thrown when `exception` is `null`.

---

### WithDetails

Appends multiple detail messages to the exception's detail collection.

**Parameters**
- `exception` (`TenantIsolationException`): The target exception instance.
- `details` (`IEnumerable<string>`): The collection of detail messages to add.

**Returns**
- `TenantIsolationException`: The modified exception instance.

**Exceptions**
- `ArgumentNullException`: Thrown when `exception` or `details` is `null`.

---

### WithErrorCode

Associates an error code with the exception for categorization or programmatic handling.

**Parameters**
- `exception` (`TenantIsolationException`): The target exception instance.
- `errorCode` (`string`): The error code to associate.

**Returns**
- `TenantIsolationException`: The modified exception instance.

**Exceptions**
- `ArgumentNullException`: Thrown when `exception` is `null`.

---

### WithContext

Adds a key-value pair to the exception's context dictionary for additional diagnostic data.

**Parameters**
- `exception` (`TenantIsolationException`): The target exception instance.
- `key` (`string`): The context key.
- `value` (`string`): The context value.

**Returns**
- `TenantIsolationException`: The modified exception instance.

**Exceptions**
- `ArgumentNullException`: Thrown when `exception` or `key` is `null`.

---

### TryGetTenantId

Attempts to retrieve the tenant identifier from the exception's context.

**Parameters**
- `exception` (`TenantIsolationException`): The source exception instance.
- `tenantId` (`out string`): Receives the tenant identifier if present.

**Returns**
- `bool`: `true` if the tenant ID was found; otherwise, `false`.

**Exceptions**
- None. Returns `false` if the exception is `null` or lacks the tenant ID context.

---

### TryGetEntityType

Attempts to retrieve the entity type from the exception's context.

**Parameters**
- `exception` (`TenantIsolationException`): The source exception instance.
- `entityType` (`out string`): Receives the entity type if present.

**Returns**
- `bool`: `true` if the entity type was found; otherwise, `false`.

**Exceptions**
- None. Returns `false` if the exception is `null` or lacks the entity type context.

---

## Usage

```csharp
try
{
    // Tenant isolation violation occurs
    throw new TenantIsolationException("Access denied")
        .WithDetail("User attempted to access resource in tenant 'TenantA'")
        .WithErrorCode("TENANT_ACCESS_DENIED")
        .WithContext("ResourceType", "Document");
}
catch (TenantIsolationException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Code: {ex.ErrorCode}");
    Console.WriteLine($"Details: {string.Join(", ", ex.Details)}");
}
```

```csharp
public void LogTenantViolation(TenantIsolationException ex)
{
    if (ex.TryGetTenantId(out var tenantId))
    {
        _logger.LogWarning("Tenant isolation violation in tenant '{TenantId}'", tenantId);
    }

    if (ex.TryGetEntityType(out var entityType))
    {
        _logger.LogWarning("Affected entity type: {EntityType}", entityType);
    }
}
```

---

## Notes

- All extension methods require a non-null `TenantIsolationException` instance. Passing `null` will result in an `ArgumentNullException`.
- The `WithContext`, `WithDetail`, and `WithDetails` methods mutate the original exception instance. If the same exception is shared across threads, concurrent modifications may lead to race conditions.
- `TryGetTenantId` and `TryGetEntityType` return `false` silently if the context key is missing or the exception is `null`, avoiding exceptions for missing data.
- These methods are designed to be chained for fluent configuration of exception metadata.
