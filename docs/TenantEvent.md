# TenantEvent

The `TenantEvent` class serves as the base event definition for tenant lifecycle and administrative actions within the `dotnet-tenant-isolation` library. It provides a standardized schema for tracking tenant states, user context, and operational metadata, facilitating consistent logging, auditing, and event-driven architecture integration across the tenant management system.

## API

*   **`string EventId`**
    *   The unique identifier for the event instance.

*   **`DateTime OccurredAt`**
    *   The timestamp indicating when the event took place.

*   **`Guid TenantId`**
    *   The unique identifier for the tenant associated with this event.

*   **`string? UserId`**
    *   The optional unique identifier for the user who initiated the action.

*   **`string? CorrelationId`**
    *   An optional identifier used for distributed tracing across services.

*   **`void SetUserId(string userId)`**
    *   Sets the `UserId` for the event. This is typically used to associate an event with a specific user context after initial event construction.

*   **`string Source`**
    *   Indicates the originating system, service, or component that generated the event.

*   **`string TenantName`**
    *   The display name of the tenant associated with this event.

*   **`string TenantSlug`**
    *   The unique, URL-friendly slug representing the tenant.

*   **`string AdminEmail`**
    *   The contact email address for the tenant's primary administrator.

*   **`string IsolationStrategy`**
    *   The specific tenant isolation strategy configured for this tenant (e.g., "Database", "Schema").

*   **`TenantCreatedEvent`**
    *   Represents the event type associated with tenant creation.

*   **`DateTime ActivatedAt`**
    *   The timestamp when the tenant was activated.

*   **`TenantActivatedEvent`**
    *   Represents the event type associated with tenant activation.

*   **`string? SuspensionReason`**
    *   Provides context for the tenant's suspension, if applicable.

*   **`DateTime SuspendedAt`**
    *   The timestamp when the tenant was suspended.

*   **`TenantSuspendedEvent`**
    *   Represents the event type associated with tenant suspension.

*   **`string? DeletionReason`**
    *   Provides context for the tenant's deletion, if applicable.

*   **`DateTime DeletedAt`**
    *   The timestamp when the tenant was deleted.

*   **`TenantDeletedEvent`**
    *   Represents the event type associated with tenant deletion.

## Usage

### Example 1: Creating and populating a tenant event
```csharp
var tenantEvent = new TenantEvent
{
    EventId = Guid.NewGuid().ToString(),
    OccurredAt = DateTime.UtcNow,
    TenantId = Guid.NewGuid(),
    TenantName = "Acme Corp",
    TenantSlug = "acme-corp",
    Source = "ProvisioningService"
};

tenantEvent.SetUserId("user-123");
```

### Example 2: Handling a lifecycle transition
```csharp
public void HandleTenantSuspension(TenantEvent tenantEvent, string reason)
{
    // Logic to process the suspension
    Console.WriteLine($"Tenant {tenantEvent.TenantName} suspended at {tenantEvent.SuspendedAt}. Reason: {reason}");
}
```

## Notes

*   **Thread Safety:** Instances of `TenantEvent` are not inherently thread-safe. If an event object is shared across multiple threads during processing, appropriate synchronization mechanisms should be implemented.
*   **Optional Fields:** Fields marked as nullable (e.g., `UserId`, `CorrelationId`, `SuspensionReason`) may be `null` depending on the nature of the event or if the required context was unavailable at the time of event generation.
*   **Polymorphism:** The members representing lifecycle-specific events (`TenantCreatedEvent`, `TenantActivatedEvent`, etc.) suggest that concrete implementations or derived types should be used when handling specific state transitions.
