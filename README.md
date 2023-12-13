# Tenant Isolation Library

A .NET library for implementing multi-tenant data isolation patterns in ASP.NET Core applications.

## Models

### TenantConfiguration

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

### Organization

The `Organization` class represents an organization entity within a multi-tenant system, storing core business information such as name, contact details, and operational metadata. It supports soft deletion, activation/deactivation, and provides navigation properties for related entities.

For detailed API documentation and usage examples, see the [Organization documentation](docs/Organization.md).

## Tenant

The `Tenant` class represents a tenant in a multi-tenancy system, storing configuration, status, and isolation settings. It supports soft deletion, subscription management, and provides methods to check tenant validity and limits.

Here's an example usage:

```csharp
using TenantIsolation.Models;
using TenantIsolation.Constants;

public class TenantManagement
{
    public static void Main(string[] args)
    {
        // Create a new tenant
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = "acme-corp",
            Name = "ACME Corporation",
            Description = "Enterprise manufacturing solutions provider",
            AdminEmail = "admin@acme-corp.com",
            PhoneNumber = "+1-555-0123",
            Status = TenantStatus.Active,
            IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant,
            PlanId = "enterprise-pro",
            MaxUsers = 500,
            MaxStorageGb = 1024.50m,
            SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
            Metadata = "{\"industry\": \"manufacturing\", \"country\": \"US\"}"
        };

        // Check tenant validity
        bool canActivate = tenant.CanActivate();
        bool isValid = tenant.IsSubscriptionValid();
        bool isLimitExceeded = tenant.IsUserLimitExceeded(150);

        // Update tenant
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.Status = TenantStatus.Active;

        // Soft delete tenant
        tenant.Delete();
        
        // Restore tenant
        tenant.Restore();
    }
}
```

This example demonstrates creating a `Tenant` instance, checking its status and limits, and managing its lifecycle through the public members and methods.



### TenantConnectionString


The `TenantConnectionString` class manages database connection strings for each tenant in a multi-tenant application. It stores connection details including database type, server information, timeouts, and connection pooling settings, with support for connection testing and validation.

Here's an example usage:

```csharp
using TenantIsolation.Models;

public class DatabaseSetup
{
  public static void Main(string[] args)
  {
    // Create a new tenant connection string
    var connectionString = new TenantConnectionString
    {
      Id = Guid.NewGuid(),
      TenantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
      DatabaseType = "SqlServer",
      ConnectionString = "Server=sql-server-01;Database=tenant_db_01;User Id=tenant_user;Password=SecurePass123!;Connection Timeout=30;Max Pool Size=100",
      Name = "Primary Database",
      SchemaName = "tenant_01_schema",
      DatabaseName = "tenant_db_01",
      ServerHost = "sql-server-01.database.windows.net",
      ServerPort = 1433,
      ConnectionTimeout = 30,
      CommandTimeout = 300,
      MaxPoolSize = 100,
      UseConnectionPooling = true,
      IsPrimary = true,
      IsActive = true
    };

    // Validate connection string
    bool isValid = connectionString.IsValidConnectionString(out string? errorMessage);
    
    // Get test connection string (with shorter timeout)
    string testConnection = connectionString.GetTestConnectionString();
    
    // Extract hostname
    string hostname = connectionString.ExtractHostname();
    
    // Record successful connection test
    connectionString.RecordSuccessfulTest();
    
    // Check tenant navigation
    if (connectionString.Tenant != null)
    {
      Console.WriteLine($"Connected to tenant: {connectionString.Tenant.Name}");
    }
  }
}
```

This example demonstrates creating a `TenantConnectionString` instance with all required properties, validating the connection string, and using helper methods to manage database connections for multi-tenant applications.



## Services

### TenantResolutionService
Provides tenant resolution strategies for multi-tenant applications.

### TenantService
Manages tenant lifecycle and configuration.

### ConfigurationService
Handles tenant-specific configuration settings with encryption and validation.

## Controllers

### TenantApiController
REST API endpoints for tenant management.

### FeaturesController
Feature flag management for tenant-specific features.

## Getting Started

See the [Getting Started Guide](docs/getting-started.md) for installation and basic setup instructions.

## Data Isolation Guide

Learn about data isolation patterns in [Data Isolation Guide](docs/data-isolation-guide.md).


## API Reference

See the full [API Reference](docs/api-reference.md) for detailed documentation.
