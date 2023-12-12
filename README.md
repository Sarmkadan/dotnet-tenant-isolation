// existing content ...

## TenantConfiguration

The `TenantConfiguration` class represents a tenant-specific configuration setting, storing key-value pairs with additional metadata such as encryption status, required flag, and creation/modification timestamps.

Here's an example usage:

```csharp
using TenantIsolation.Models;

public class Program
{
    public static void Main(string[] args)
    {
        var config = new TenantConfiguration
        {
            TenantId = Guid.NewGuid(),
            Key = "features:api:enabled",
            Value = "true",
            IsEncrypted = false,
            IsRequired = true,
            IsOverridable = true,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        var value = config.GetValueAs<bool>(); // true
        config.SetValue<bool>(false);
        var isValid = config.IsValid(out string? errorMessage); // True
    }
}
```

This example demonstrates creating a `TenantConfiguration` instance and using its public members to manage configuration settings.
// ... rest of existing content ...
