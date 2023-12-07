// existing content ...

## TenantEvent

The `TenantEvent` class represents a base class for tenant-related events. It provides a standardized way to store event metadata, such as the event ID, occurred at date, tenant ID, and user ID.

### Example Usage

```csharp
public class TenantCreatedEvent : TenantEvent
{
    public TenantCreatedEvent(string tenantName, string tenantSlug, string adminEmail, string isolationStrategy)
    {
        TenantName = tenantName;
        TenantSlug = tenantSlug;
        AdminEmail = adminEmail;
        IsolationStrategy = isolationStrategy;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var tenantCreatedEvent = new TenantCreatedEvent("My Tenant", "my-tenant", "admin@example.com", "IsolationStrategy1");
        Console.WriteLine($"Event ID: {tenantCreatedEvent.EventId}");
        Console.WriteLine($"Occurred At: {tenantCreatedEvent.OccurredAt}");
        Console.WriteLine($"Tenant ID: {tenantCreatedEvent.TenantId}");
        Console.WriteLine($"Tenant Name: {tenantCreatedEvent.TenantName}");
        Console.WriteLine($"Tenant Slug: {tenantCreatedEvent.TenantSlug}");
        Console.WriteLine($"Admin Email: {tenantCreatedEvent.AdminEmail}");
        Console.WriteLine($"Isolation Strategy: {tenantCreatedEvent.IsolationStrategy}");
    }
}
```

// existing content ...
