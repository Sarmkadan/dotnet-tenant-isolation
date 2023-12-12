// existing content ...

## DataIsolationPolicy

The `DataIsolationPolicy` class represents a data isolation policy for a tenant, defining rules for accessing specific data entities. It allows for fine-grained control over data access, including filtering, field-level access control, and cross-tenant access.

Here's an example usage:

```csharp
using TenantIsolation.Models;

public class Program
{
    public static void Main(string[] args)
    {
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            PolicyType = DataIsolationPolicyType.Relaxed,
            EntityType = "Order",
            Description = "Example policy for orders",
            FilterRule = "CustomerId == 123",
            AllowedFields = "Id, CustomerId, OrderDate",
            DeniedFields = "TotalAmount",
            AllowedCrossTenantAccess = "tenant-id-1, tenant-id-2",
            IsActive = true,
            Priority = 50
        };

        var allowedFields = policy.GetAllowedFields(); // ["Id", "CustomerId", "OrderDate"]
        var deniedFields = policy.GetDeniedFields(); // ["TotalAmount"]
        var isFieldAllowed = policy.IsFieldAccessAllowed("Id"); // True
        var isCrossTenantAccessAllowed = policy.IsCrossTenantAccessAllowed(Guid.Parse("tenant-id-1")); // True
        var isValid = policy.IsValidPolicy(out string? errorMessage); // True
    }
}
```

This example demonstrates creating a `DataIsolationPolicy` instance and using its public members to manage data access rules.
// ... rest of existing content ...
