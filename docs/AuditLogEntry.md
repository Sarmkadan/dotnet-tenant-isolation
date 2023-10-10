# AuditLogEntry

The `AuditLogEntry` type represents a single record of an auditable operation within a multi‑tenant application. It captures who performed the action, what was acted upon, when it occurred, and the outcome, together with optional contextual details such as the caller’s IP address and a change set describing before‑and‑after state.

## API

### Id  
**Type:** `string`  
**Purpose:** Unique identifier for the audit entry, typically a UUID or database‑generated key.  
**Remarks:** Read‑write; should be set before persisting the entry. Setting to `null` or empty may cause storage failures.

### Timestamp  
**Type:** `DateTime`  
**Purpose:** Date and time (UTC) when the audited action occurred.  
**Remarks:** Read‑write; if not supplied, the logging methods will assign `DateTime.UtcNow`.

### TenantId  
**Type:** `Guid`  
**Purpose:** Identifier of the tenant to which the entry belongs.  
**Remarks:** Read‑write; must be a non‑empty Guid; otherwise `LogAsync` and related methods throw `ArgumentException`.

### UserId  
**Type:** `string?`  
**Purpose:** Identifier of the user who performed the action; may be `null` for system‑generated events.  
**Remarks:** Read‑write; length unrestricted but excessive values may be truncated by the underlying store.

### Action  
**Type:** `string`  
**Purpose:** Short description of the operation (e.g., “Create”, “Update”, “Delete”).  
**Remarks:** Read‑write; required; `null` or whitespace causes `ArgumentException` in logging methods.

### Resource  
**Type:** `string`  
**Purpose:** Name or type of the entity that was acted upon (e.g., “Order”, “UserProfile”).  
**Remarks:** Read‑write; required; validation similar to `Action`.

### ResourceId  
**Type:** `string?`  
**Purpose:** Optional identifier of the specific resource instance (e.g., primary key).  
**Remarks:** Read‑write; may be `null` when the action applies to the resource type generally.

### ActionType  
**Type:** `AuditAction` (enum)  
**Purpose:** Strongly typed categorisation of the action (e.g., `Create`, `Read`, `Update`, `Delete`).  
**Remarks:** Read‑write; must be a defined enum value; invalid values result in `InvalidEnumArgumentException`.

### Details  
**Type:** `string?`  
**Purpose:** Free‑form text providing additional context (e.g., error messages, user comments).  
**Remarks:** Read‑write; may be `null` or empty.

### Success  
**Type:** `bool`  
**Purpose:** Indicates whether the audited operation completed successfully.  
**Remarks:** Read‑write; default is `false`; logging methods do not alter this value.

### ErrorMessage  
**Type:** `string?`  
**Purpose:** When `Success` is `false`, contains the exception or validation message.  
**Remarks:** Read‑write; ignored if `Success` is `true`.

### IpAddress  
**Type:** `string?`  
**Purpose:** IP address of the caller that initiated the operation.  
**Remarks:** Read‑write; may be `null` for internal or trusted calls.

### ChangeSet  
**Type:** `Dictionary<string, object>`  
**Purpose:** Captures the before‑and‑after state of the resource; keys are property names, values are the corresponding objects.  
**Remarks:** Read‑write; may be `null`; the logging methods do not serialize the dictionary—callers must ensure contained types are serializable by the persistence layer.

### AuditLogger  
**Type:** `AuditLogger`  
**Purpose:** Provides access to the logging infrastructure associated with this entry (e.g., configuration, store accessor).  
**Remarks:** Read‑only property; returns a shared logger instance; `null` indicates the logger has not been initialized.

### LogAsync  
**Signature:** `public async Task LogAsync()`  
**Purpose:** Persists the current `AuditLogEntry` instance to the configured audit store.  
**Parameters:** None (operates on the instance’s properties).  
**Return Value:** A `Task` that completes when the write operation finishes.  
**Exceptions:**  
- `ArgumentNullException` if `TenantId` is empty or required string properties are `null`.  
- `InvalidOperationException` if the `AuditLogger` property is `null`.  
- `IOException` or derived storage exceptions for persistence failures.  

### GetLogsAsync  
**Signature:** `public async Task<IEnumerable<AuditLogEntry>> GetLogsAsync(DateTime? startUtc = null, DateTime? endUtc = null)`  
**Purpose:** Retrieves audit entries for the entry’s `TenantId` within an optional time window.  
**Parameters:**  
- `startUtc`: Inclusive lower bound (UTC); if `null`, no lower filter is applied.  
- `endUtc`: Exclusive upper bound (UTC); if `null`, no upper filter is applied.  
**Return Value:** An `IEnumerable<AuditLogEntry>` containing matching records, ordered by `Timestamp` descending.  
**Exceptions:**  
- `ArgumentException` if `startUtc` is after `endUtc`.  
- `IOException` for store access errors.  

### GetUserLogsAsync  
**Signature:** `public async Task<IEnumerable<AuditLogEntry>> GetUserLogsAsync(string userId)`  
**Purpose:** Retrieves audit entries for the entry’s `TenantId` performed by a specific user.  
**Parameters:**  
- `userId`: The user identifier to filter by; must not be `null` or whitespace.  
**Return Value:** An `IEnumerable<AuditLogEntry>` ordered by `Timestamp` descending.  
**Exceptions:**  
- `ArgumentNullException` or `ArgumentException` if `userId` is invalid.  
- `IOException` for store failures.  

### GetResourceLogsAsync  
**Signature:** `public async Task<IEnumerable<AuditLogEntry>> GetResourceLogsAsync(string resource, string? resourceId = null)`  
**Purpose:** Retrieves audit entries for the entry’s `TenantId` that match a given resource type and optionally a specific resource instance.  
**Parameters:**  
- `resource`: Resource type name; required, non‑whitespace.  
- `resourceId`: Optional instance identifier; if supplied, only entries for that instance are returned.  
**Return Value:** An `IEnumerable<AuditLogEntry>` ordered by `Timestamp` descending.  
**Exceptions:**  
- `ArgumentException` if `resource` is invalid.  
- `IOException` for store access problems.  

### ClearOldLogsAsync  
**Signature:** `public async Task ClearOldLogsAsync(DateTime cutoffUtc)`  
**Purpose:** Permanently removes audit entries older than the specified cutoff for the entry’s `TenantId`.  
**Parameters:**  
- `cutoffUtc`: Entries with `Timestamp` earlier than this value are deleted.  
**Return Value:** A `Task` that completes when the deletion operation finishes.  
**Exceptions:**  
- `ArgumentOutOfRangeException` if `cutoffUtc` is in the future.  
- `UnauthorizedAccessException` if the caller lacks permission to purge logs.  
- `IOException` for underlying store errors.  

### LogCreateAsync (static)  
**Signature:** `public static async Task<AuditLogEntry> LogCreateAsync(Guid tenantId, string action, string resource, string? userId = null, string? resourceId = null, AuditAction actionType = AuditAction.Create, bool success = true, string? details = null, string? errorMessage = null, string? ipAddress = null, IDictionary<string, object>? changeSet = null)`  
**Purpose:** Convenience method that creates, populates, and persists a new `AuditLogEntry` in a single operation.  
**Parameters:**  
- `tenantId`: Tenant identifier; must not be empty.  
- `action`: Action description; required.  
- `resource`: Resource type; required.  
- `userId`: Optional user identifier.  
- `resourceId`: Optional resource instance identifier.  
- `actionType`: Typed action; defaults to `Create`.  
- `success`: Outcome flag; defaults to `true`.  
- `details`: Optional free‑form notes.  
- `errorMessage`: Optional error text when `success` is `false`.  
- `ipAddress`: Optional caller IP address.  
- `changeSet`: Optional dictionary of property changes.  
**Return Value:** The created `AuditLogEntry` instance (with `Id` and `Timestamp` populated by the store).  
**Exceptions:**  
- `ArgumentNullException` for any required parameter that is `null`.  
- `ArgumentException` for malformed strings (empty/whitespace).  
- `InvalidOperationException` if the internal `AuditLogger` cannot be resolved.  
- `IOException` for persistence failures.  

## Usage

### Example 1: Manual entry creation and logging
```csharp
var entry = new AuditLogEntry
{
    TenantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    UserId   = "alice@example.com",
    Action   = "Update",
    Resource = "Order",
    ResourceId = "order-123",
    ActionType = AuditAction.Update,
    Success    = true,
    Details    = "Changed shipping address",
    IpAddress  = "203.0.113.42",
    ChangeSet  = new Dictionary<string, object>
    {
        ["ShippingAddress"] = "123 New St, Anytown"
    }
};

await entry.LogAsync(); // persists the entry
```

### Example 2: Using the static helper to log a deletion
```csharp
var loggedEntry = await AuditLogEntry.LogCreateAsync(
    tenantId: Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    action:   "Delete",
    resource: "Order",
    userId:   "bob@example.com",
    resourceId: "order-456",
    actionType: AuditAction.Delete,
    success: false,
    errorMessage: "Order not found",
    ipAddress:  "203.0.113.99");

Console.WriteLine($"Logged entry {loggedEntry.Id} at {loggedEntry.Timestamp:u}");
```

## Notes

- All instance properties are mutable; concurrent modification of the same `AuditLogEntry` instance from multiple threads without external synchronization can lead to inconsistent state before logging. It is recommended to create a new instance per operation or to treat the instance as immutable after population.
- The static `LogCreateAsync` method is thread‑safe; it creates a new instance internally and relies on the underlying `AuditLogger`, which is expected to be thread‑safe.
- Retrieval methods (`GetLogsAsync`, `GetUserLogsAsync`, `GetResourceLogsAsync`) return snapshots that reflect the state of the store at the moment of the query; they do not provide live updates.
- `ClearOldLogsAsync` performs a irreversible deletion; callers should verify the cutoff value and ensure appropriate backup or retention policies are in place.
- If the `AuditLogger` property is `null`, all instance‑based logging methods will throw `InvalidOperationException`; ensure the logger is initialized (e.g., via dependency injection) before attempting to persist entries.
- String‑based parameters (`Action`, `Resource`, `UserId`, etc.) are validated for null or whitespace only where indicated; excessively long values may be truncated by the persistence layer, which is outside the scope of this type.
