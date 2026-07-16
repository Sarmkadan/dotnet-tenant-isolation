# Tenant Isolation Library

A .NET library for implementing multi-tenant data isolation patterns in ASP.NET Core applications.

## Architecture

Tenants are resolved once per request (header -> claims -> route -> subdomain), cached in `HttpContext.Items`, and every downstream component - the tenant-aware `DbContext` factory, caching, feature toggles, usage metering - works off that resolved tenant. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full component breakdown, data flow, design decisions and known limitations.

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

## User



The `User` class represents a user within a multi-tenant system, storing core identity information, authentication state, and tenant association. It supports user lifecycle management, authentication tracking, and provides navigation properties for related tenant and organization entities.




Here's an example usage:

```csharp
using TenantIsolation.Models;
using TenantIsolation.Constants;

public class UserManagement
{
public static void Main(string[] args)
{
 // Create a new user for a tenant
 var user = new User
 {
 Id = Guid.NewGuid(),
 TenantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
 OrganizationId = Guid.Parse("4fa85f64-5717-4562-b3fc-2c963f66afa6"),
 Email = "john.doe@acme-corp.com",
 FirstName = "John",
 LastName = "Doe",
 Role = UserRole.Administrator,
 PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecurePassword123!"),
 IsActive = true,
 IsEmailVerified = true,
 IsTwoFactorEnabled = false,
 LastLoginAt = DateTime.UtcNow.AddDays(-1),
 FailedLoginAttempts = 0,
 LockedUntil = null,
 PhoneNumber = "+1-555-0101",
 AvatarUrl = "https://acme-corp.com/avatars/john-doe.jpg",
 Preferences = "{\"theme\": \"dark\", \"language\": \"en-US\"}",
 LastPasswordChangeAt = DateTime.UtcNow,
 CreatedAt = DateTime.UtcNow,
 UpdatedAt = DateTime.UtcNow
 };

 // Check user status
 bool canLogin = user.CanLogin(); // true
 bool isLocked = user.IsLocked(); // false
 bool requiresPasswordChange = user.RequiresPasswordChange(); // false

 // Update user properties
 user.FirstName = "Jonathan";
 user.LastLoginAt = DateTime.UtcNow;
 user.FailedLoginAttempts = 0;
 user.UpdatedAt = DateTime.UtcNow;

 // Reset failed login attempts
 user.ResetFailedLoginAttempts();

 // Check tenant and organization navigation
 if (user.Tenant != null)
 {
 Console.WriteLine($"User belongs to tenant: {user.Tenant.Name}");
 }

 if (user.Organization != null)
 {
 Console.WriteLine($"User belongs to organization: {user.Organization.Name}");
 }
}
}
```

This example demonstrates creating a `User` instance with all required properties, checking authentication status, updating user properties, and using navigation properties to access related entities.



## TenantFeature

`TenantFeature` represents a feature flag for a specific tenant. It stores metadata such as the feature key, display name, rollout percentage, availability dates, usage limits, and provides helper methods to determine if the feature is currently available, if its usage limit has been exceeded, and whether it can be used in the current context.

```csharp
using System;
using TenantIsolation.Models;

public class FeatureDemo
{
    public static void Main(string[] args)
    {
        // Create a new feature flag for a tenant
        var feature = new TenantFeature
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            FeatureKey = "advanced-reporting",
            DisplayName = "Advanced Reporting",
            Description = "Enables detailed analytics reports.",
            IsEnabled = true,
            Category = "Analytics",
            RolloutPercentage = 75,               // 75% of users see the feature
            AvailabilityLevel = "Beta",
            AvailableFrom = DateTime.UtcNow.AddDays(-1),
            DeprecatedAt = null,
            UsageLimit = 10_000,                  // max 10,000 uses
            CurrentUsage = 0,
            Metadata = "{\"requiresLicense\":true}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Determine if the feature is currently available
        bool available = feature.IsAvailable();

        // Check usage limits
        bool limitExceeded = feature.IsUsageLimitExceeded();

        // Try to use the feature
        if (feature.CanUseFeature(out string? error))
        {
            Console.WriteLine($"{feature.DisplayName} can be used.");
            feature.RecordUsage(); // increment usage counter
        }
        else
        {
            Console.WriteLine($"Cannot use feature: {error}");
        }

        // Output status
        Console.WriteLine($"Feature status: {feature.GetStatus()}");
    }
}
```

The example demonstrates constructing a `TenantFeature`, checking its availability, enforcing usage limits, and recording usage—all using the public members defined on the type.


## TenantFeatureService

The `TenantFeatureService` manages tenant-specific feature flags, enabling or disabling features, tracking usage, and enforcing rollout percentages. It provides methods to check feature availability, enable/disable features, set rollout percentages, record usage metrics, and retrieve feature statistics.

**Key capabilities:**
- Enable and disable features per tenant
- Set rollout percentages for gradual feature deployment
- Track and enforce usage limits
- Retrieve feature statistics and reports
- Cache feature data for performance
- Initialize default features for new tenants

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Models;
using TenantIsolation.Services;
using TenantIsolation.Data;

public class FeatureManagementExample
{
public static async Task Main(string[] args)
{
// Setup dependency injection
var services = new ServiceCollection();
services.AddLogging(configure => configure.AddConsole());
services.AddDbContext<TenantDbContext>();
services.AddScoped<TenantFeatureService>();

var provider = services.BuildServiceProvider();

var featureService = provider.GetRequiredService<TenantFeatureService>();
var tenantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

// Initialize default features for tenant
await featureService.InitializeDefaultFeaturesAsync(tenantId);
Console.WriteLine("Default features initialized");

// Enable a feature with 75% rollout
var enabledFeature = await featureService.EnableFeatureAsync(tenantId, "advanced-reporting", 75);
Console.WriteLine($"Enabled feature: {enabledFeature.DisplayName} (Rollout: {enabledFeature.RolloutPercentage}%)");

// Check if feature is enabled
bool isEnabled = await featureService.IsFeatureEnabledAsync(tenantId, "advanced-reporting");
Console.WriteLine($"Feature enabled: {isEnabled}");

// Get feature details
var featureDetails = await featureService.GetFeatureAsync(tenantId, "advanced-reporting");
if (featureDetails != null)
{
Console.WriteLine($"Feature details: {featureDetails.DisplayName}, Category: {featureDetails.Category}");
}

// Record feature usage
bool usageRecorded = await featureService.RecordFeatureUsageAsync(tenantId, "advanced-reporting", 5);
Console.WriteLine($"Usage recorded: {usageRecorded}");

// Check usage limit
bool canUse = await featureService.CheckUsageLimitAsync(tenantId, "advanced-reporting");
Console.WriteLine($"Can use feature: {canUse}");

// Get all enabled features
var enabledFeatures = await featureService.GetEnabledFeaturesAsync(tenantId);
Console.WriteLine($"Enabled features count: {enabledFeatures.Count}");

// Get statistics
var stats = await featureService.GetStatisticsAsync(tenantId);
Console.WriteLine($"Total features: {stats.TotalFeatures}, Enabled: {stats.EnabledFeatures}");

// Disable feature
bool disabled = await featureService.DisableFeatureAsync(tenantId, "advanced-reporting");
Console.WriteLine($"Feature disabled: {disabled}");

// Set rollout percentage
bool rolloutSet = await featureService.SetRolloutPercentageAsync(tenantId, "advanced-reporting", 50);
Console.WriteLine($"Rollout percentage set: {rolloutSet}");
}
}
```

This example demonstrates creating a `TenantFeatureService` instance through dependency injection, initializing default features, enabling/disabling features, checking feature status, recording usage, and retrieving feature statistics using the public members defined on the type.

## ExportRequest

The `ExportRequest` class represents a request to export tenant-specific data in various formats (JSON, CSV, XML). It contains properties to specify the tenant context, resource type to export, output format, filtering criteria, and field selection for selective data export.

**Key properties:**
- `TenantId`: The unique identifier of the tenant
- `ResourceType`: The type of resource being exported (e.g., "Users", "Products")
- `Format`: The export format (JSON, CSV, or XML)
- `Filters`: Optional dictionary of filter criteria to apply during export
- `IncludeFields`: Optional list of field names to include in the export

**Usage example**

```csharp
using System;
using System.Collections.Generic;
using TenantIsolation.Services;

public class ExportExample
{
    public static async Task Main(string[] args)
    {
        // Create an export request for user data
        var exportRequest = new ExportRequest
        {
            TenantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ResourceType = "Users",
            Format = ExportFormat.Json,
            Filters = new Dictionary<string, object>
            {
                { "status", "active" },
                { "createdAfter", DateTime.UtcNow.AddDays(-30) }
            },
            IncludeFields = new List<string> { "Id", "Email", "FirstName", "LastName", "Role" }
        };

        // Get the export service
        var services = new ServiceCollection();
        services.AddExportService();
        services.AddLogging(configure => configure.AddConsole());
        
        var provider = services.BuildServiceProvider();
        var exportService = provider.GetRequiredService<IExportService>();

        // Create sample data to export
        var users = new List<object>
        {
            new { Id = Guid.NewGuid(), Email = "john.doe@acme.com", FirstName = "John", LastName = "Doe", Role = "Administrator" },
            new { Id = Guid.NewGuid(), Email = "jane.smith@acme.com", FirstName = "Jane", LastName = "Smith", Role = "User" },
            new { Id = Guid.NewGuid(), Email = "bob.johnson@acme.com", FirstName = "Bob", LastName = "Johnson", Role = "User" }
        };

        // Export the data
        var exportResult = await exportService.ExportAsync(exportRequest, users);
        
        Console.WriteLine($"Export completed:");
        Console.WriteLine($"  File: {exportResult.FileName}");
        Console.WriteLine($"  Format: {exportResult.Format}");
        Console.WriteLine($"  Size: {exportResult.SizeBytes} bytes");
        Console.WriteLine($"  Content Type: {exportResult.ContentType}");
        Console.WriteLine($"  Created: {exportResult.CreatedAt}");
    }
}
```

This example demonstrates creating an `ExportRequest` instance with all required properties, registering the export service, and using it to export data in JSON format with filtering and field selection.

## Services

### TenantResolutionService
Provides tenant resolution strategies for multi-tenant applications.

### TenantService

The `TenantService` provides services for managing tenant lifecycles, including creation, activation, suspension, and deletion. It handles tenant operations such as creating new tenants, retrieving tenant information, managing tenant status, and providing tenant statistics.

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Models;
using TenantIsolation.Services;
using TenantIsolation.Data;
using TenantIsolation.Constants;

public class TenantManagementExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddSingleton<IDynamicTenantStore, InMemoryDynamicTenantStore>(); // Replace with real implementation
        services.AddScoped<TenantRepository>();
        services.AddScoped<TenantService>();
        
        var provider = services.BuildServiceProvider();
        
        var tenantService = provider.GetRequiredService<TenantService>();
        
        // Create a new tenant
        var newTenant = await tenantService.CreateTenantAsync(
            name: "Acme Corporation",
            slug: "acme-corp",
            adminEmail: "admin@acme-corp.com",
            strategy: TenantIsolationStrategy.DatabasePerTenant
        );
        
        Console.WriteLine($"Created tenant: {newTenant.Name} (Id: {newTenant.Id})");
        
        // Get tenant by ID
        var retrievedTenant = await tenantService.GetTenantAsync(newTenant.Id);
        Console.WriteLine($"Retrieved tenant: {retrievedTenant.Name}");
        
        // Activate tenant
        var activationResult = await tenantService.ActivateTenantAsync(newTenant.Id);
        Console.WriteLine($"Activation successful: {activationResult}");
        
        // Check subscription validity
        var isValid = await tenantService.IsSubscriptionValidAsync(newTenant.Id);
        Console.WriteLine($"Subscription valid: {isValid}");
        
        // Get all active tenants
        var activeTenants = await tenantService.GetActiveTenantsAsync();
        Console.WriteLine($"Active tenants count: {activeTenants.Count}");
        
        // Search tenants
        var searchResults = await tenantService.SearchTenantsAsync("acme");
        Console.WriteLine($"Search results: {searchResults.Count}");
        
        // Get tenant statistics
        var statistics = await tenantService.GetTenantStatisticsAsync();
        Console.WriteLine($"Total tenants: {statistics.TotalTenants}, Active: {statistics.ActiveTenants}");
        
        // Update tenant
        await tenantService.UpdateTenantAsync(newTenant.Id, tenant => {
            tenant.Description = "Enterprise manufacturing solutions provider";
            tenant.MaxUsers = 500;
        });
        
        // Suspend tenant
        var suspendResult = await tenantService.SuspendTenantAsync(newTenant.Id, "Account suspended for non-payment");
        Console.WriteLine($"Suspension successful: {suspendResult}");
        
        // Delete tenant (soft delete)
        var deleteResult = await tenantService.DeleteTenantAsync(newTenant.Id);
        Console.WriteLine($"Deletion successful: {deleteResult}");
    }
}
```

*Note:* `InMemoryDynamicTenantStore` is a placeholder for your own `IDynamicTenantStore` implementation.

### ConfigurationService
Handles tenant-specific configuration settings with encryption and validation.

## ITenantUsageMeteringService

The `ITenantUsageMeteringService` interface provides per-tenant usage metering and quota enforcement capabilities for tracking resource consumption in multi-tenant applications. It allows recording usage metrics, checking quota limits, and enforcing those limits across different metric keys for each tenant.

**Key capabilities:**
- Record usage for specific metrics (API calls, storage, users, etc.)
- Check and enforce quota limits
- Retrieve current usage records
- Reset usage counters
- Set or update quota limits

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Models;
using TenantIsolation.Services;

public class UsageMeteringExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddSingleton<ITenantUsageMeteringService, TenantUsageMeteringService>();
        
        var provider = services.BuildServiceProvider();
        
        var meteringService = provider.GetRequiredService<ITenantUsageMeteringService>();
        
        var tenantId = Guid.NewGuid();
        const string metricKey = "api_calls";
        
        // Set quota limit for a metric
        await meteringService.SetQuotaAsync(tenantId, metricKey, 1000);
        
        // Record usage
        var usageRecord = await meteringService.RecordUsageAsync(tenantId, metricKey, 50);
        Console.WriteLine($"Recorded {usageRecord.CurrentValue} {metricKey} for tenant {tenantId}");
        
        // Check quota status
        var quotaCheck = await meteringService.CheckQuotaAsync(tenantId, metricKey);
        Console.WriteLine($"Quota status: {(quotaCheck.IsAllowed ? "Allowed" : "Denied")} - {quotaCheck.CurrentUsage}/{quotaCheck.QuotaLimit}");
        
        // Get all metrics for a tenant
        var allMetrics = await meteringService.GetAllMetricsAsync(tenantId);
        foreach (var metric in allMetrics)
        {
            Console.WriteLine($"Metric: {metric.MetricKey} = {metric.CurrentValue}");
        }
        
        // Enforce quota (throws if exceeded)
        try
        {
            await meteringService.EnforceQuotaAsync(tenantId, metricKey);
            Console.WriteLine("Quota check passed");
        }
        catch (TenantIsolationException ex) when (ex.Code == "QUOTA_EXCEEDED")
        {
            Console.WriteLine("Quota exceeded!");
        }
        
        // Reset usage
        await meteringService.ResetUsageAsync(tenantId, metricKey);
        
        // Get specific usage record
        var specificUsage = await meteringService.GetUsageAsync(tenantId, metricKey);
        Console.WriteLine($"Current usage: {specificUsage?.CurrentValue ?? 0}");
    }
}
```

## TenantResolutionService

The `TenantResolutionService` resolves the current tenant for each request using a prioritized set of strategies (header → claims → route → subdomain) and caches the result in `HttpContext.Items`. It exposes helper methods to retrieve the resolved tenant, its identifier, and to check whether a tenant is present.

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Services;

public class Example
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // Register required services
        services.AddHttpContextAccessor();
        services.AddLogging();

        // Register your implementation of IDynamicTenantStore
        services.AddSingleton<IDynamicTenantStore, InMemoryDynamicTenantStore>(); // replace with real implementation

        // Register the resolution service
        services.AddTransient<TenantResolutionService>();

        var provider = services.BuildServiceProvider();

        var tenantResolver = provider.GetRequiredService<TenantResolutionService>();

        // Resolve the tenant for the current request
        var tenant = await tenantResolver.ResolveTenantAsync();

        // Access helper members
        var tenantId = tenantResolver.GetCurrentTenantId();
        var hasTenant = tenantResolver.HasTenant();

        Console.WriteLine($"Resolved tenant: {tenant.Name} (Id: {tenantId}), HasTenant: {hasTenant}");
    }
}
```

*Note:* `InMemoryDynamicTenantStore` is a placeholder for your own `IDynamicTenantStore` implementation.

## ConfigurationService
The `ConfigurationService` is responsible for managing tenant-specific configuration settings. It provides methods to set, get, and delete configurations, as well as batch operations and caching for improved performance. Here's an example usage:

```csharp
using TenantIsolation.Services;

public class ConfigurationExample
{
    public static async Task Main(string[] args)
    {
        var configurationService = new ConfigurationService(
            new TenantDbContext(),
            new MemoryCache(new MemoryCacheOptions()),
            new LoggerFactory().CreateLogger<ConfigurationService>());

        // Set a configuration value
        var config = await configurationService.SetConfigurationAsync(
            Guid.NewGuid(),
            "features:api:enabled",
            "true",
            "string",
            false);

        // Get a configuration value
        var getConfig = await configurationService.GetConfigurationAsync<Guid>(Guid.NewGuid(), "features:api:enabled");

        // Delete a configuration
        var deleted = await configurationService.DeleteConfigurationAsync(Guid.NewGuid(), "features:api:enabled");
    }
}
```

This example demonstrates creating a `ConfigurationService` instance and using its public members to manage configuration settings.

## Controllers

### TenantApiController
REST API endpoints for tenant management.

## FeaturesController

The `FeaturesController` provides RESTful API endpoints for managing tenant-specific feature flags in a multi-tenant application. It enables feature toggle management through endpoints for checking feature status, enabling/disabling features, setting rollout percentages, tracking usage, and retrieving statistics. The controller integrates with the `TenantFeatureService` to provide tenant-aware feature management operations.

**Key capabilities:**
- Check if a feature is enabled for the current tenant
- Retrieve feature details and all features for a tenant
- Enable and disable features with tenant scope
- Set rollout percentages for gradual feature deployment
- Record feature usage and check usage limits
- Retrieve feature statistics and reports
- Initialize default features for new tenants

**Usage example**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Controllers;
using TenantIsolation.Models;

public class FeaturesControllerExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddScoped<FeaturesController>();
        services.AddScoped<ITenantResolutionService, TenantResolutionService>();
        services.AddScoped<TenantFeatureService>();
        
        var provider = services.BuildServiceProvider();
        var featuresController = provider.GetRequiredService<FeaturesController>();
        
        // Get current tenant (simulated)
        var tenant = new Tenant
        {
            Id = Guid.Parse("3fa85f6-4717-4562-b3fc-2c963f66afa6"),
            Name = "ACME Corporation",
            Slug = "acme-corp"
        };
        
        // Initialize default features
        var initResult = await featuresController.InitializeDefaults();
        Console.WriteLine($"Default features initialized: {initResult}");
        
        // Enable a feature
        var enableResult = await featuresController.EnableFeature("advanced-reporting");
        Console.WriteLine($"Feature enabled: {enableResult}");
        
        // Check if feature is enabled
        var checkResult = await featuresController.IsFeatureEnabled("advanced-reporting");
        Console.WriteLine($"Feature enabled check: {checkResult}");
        
        // Get feature details
        var featureResult = await featuresController.GetFeature("advanced-reporting");
        Console.WriteLine($"Feature details: {featureResult}");
        
        // Set rollout percentage
        var setRolloutResult = await featuresController.SetRolloutPercentage(
            "advanced-reporting", 
            new SetRolloutRequest { Percentage = 75 }
        );
        Console.WriteLine($"Rollout percentage set: {setRolloutResult}");
        
        // Record feature usage
        var usageResult = await featuresController.RecordUsage(
            "advanced-reporting",
            new RecordUsageRequest { Amount = 5 }
        );
        Console.WriteLine($"Usage recorded: {usageResult}");
        
        // Check usage limit
        var limitResult = await featuresController.CheckUsageLimit("advanced-reporting");
        Console.WriteLine($"Usage limit check: {limitResult}");
        
        // Get all enabled features
        var enabledFeatures = await featuresController.GetEnabledFeatures();
        Console.WriteLine($"Enabled features count: {((OkObjectResult)enabledFeatures).Value}");
        
        // Get all features
        var allFeatures = await featuresController.GetAllFeatures();
        Console.WriteLine($"All features count: {((OkObjectResult)allFeatures).Value}");
        
        // Get statistics
        var statsResult = await featuresController.GetStatistics();
        Console.WriteLine($"Feature statistics: {((OkObjectResult)statsResult).Value}");
        
        // Disable feature
        var disableResult = await featuresController.DisableFeature("advanced-reporting");
        Console.WriteLine($"Feature disabled: {disableResult}");
    }
}
```

### AdminController
The `AdminController` provides administrative endpoints for tenant management and system operations. It requires administrative authorization in production environments and exposes endpoints for retrieving system statistics, managing tenant lifecycles, monitoring background tasks, and handling expiring subscriptions.

**Key capabilities:**
- Retrieve system-wide statistics and tenant overview
- List, filter, and paginate all tenants by status
- Suspend and activate tenant accounts with administrative control
- Monitor background task queue statistics and performance
- Enqueue manual cleanup tasks for system maintenance
- Generate reports on expiring subscriptions

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Controllers;
using TenantIsolation.Models;

public class AdminControllerExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddScoped<AdminController>();
        
        var provider = services.BuildServiceProvider();
        var adminController = provider.GetRequiredService<AdminController>();
        
        // Get system statistics
        var statsResult = await adminController.GetStatistics();
        Console.WriteLine($"System statistics retrieved: {statsResult.Value?.Data}");
        
        // Get all active tenants (paginated)
        var tenantsResult = await adminController.GetAllTenants(page: 1, pageSize: 20, status: "active");
        Console.WriteLine($"Active tenants count: {tenantsResult.Value?.Data?.TotalCount}");
        
        // Suspend a tenant
        var suspendResult = await adminController.SuspendTenant(
            Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            new AdminController.SuspensionRequest { Reason = "Account suspended for non-payment" }
        );
        Console.WriteLine($"Suspension result: {suspendResult.Value?.Success}");
        
        // Activate a tenant
        var activateResult = await adminController.ActivateTenant(
            Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6")
        );
        Console.WriteLine($"Activation result: {activateResult.Value?.Success}");
        
        // Get queue statistics
        var queueStats = adminController.GetQueueStatistics();
        Console.WriteLine($"Queue size: {queueStats.Value?.Data?.TotalItems}");
        
        // Enqueue a maintenance task
        var taskResult = adminController.EnqueueTask(new AdminController.TaskRequest {
            TaskName = "Cleanup expired subscriptions",
            Priority = 2
        });
        Console.WriteLine($"Task queued: {taskResult.Value?.Success}");
        
        // Get expiring subscriptions report
        var expiringResult = await adminController.GetExpiringSubscriptions(daysUntilExpiry: 30);
        Console.WriteLine($"Expiring subscriptions: {expiringResult.Value?.Data?.Count}");
    }
}
```

## Getting Started

See the [Getting Started Guide](docs/getting-started.md) for installation and basic setup instructions.

## Data Isolation Guide

Learn about data isolation patterns in [Data Isolation Guide](docs/data-isolation-guide.md).


## API Reference

See the full [API Reference](docs/api-reference.md) for detailed documentation.

## Notification

The `Notification` class represents an in-app notification for multi-tenant applications. It stores notification details including title, message, type, recipient information, timestamps, and metadata. Notifications support tracking read status, expiration dates, and can be associated with either individual users or entire tenants.

Here's an example usage:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Services;
using TenantIsolation.Constants;

public class NotificationManagement
{
    public static async Task Main(string[] args)
    {
        // Create a new notification
        var notification = new Notification
        {
            Title = "Welcome to the platform",
            Message = "Your account has been successfully created and is ready to use.",
            Type = NotificationType.Success,
            RecipientUserId = "user-12345",
            TenantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            Metadata = new Dictionary<string, string>
            {
                { "actionUrl", "/dashboard" },
                { "priority", "high" }
            },
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        // Send notification through service
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddNotificationService();
        
        var serviceProvider = services.BuildServiceProvider();
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();
        
        var sentNotification = await notificationService.SendNotificationAsync(notification);
        Console.WriteLine($"Notification sent: {sentNotification.Id}");
        
        // Send tenant-wide notification
        await notificationService.SendTenantNotificationAsync(
            tenantId: Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            title: "Maintenance Scheduled",
            message: "The system will be undergoing maintenance tonight at 2 AM UTC.",
            type: NotificationType.Warning
        );
        
        // Get unread notifications
        var unreadNotifications = await notificationService.GetUnreadNotificationsAsync("user-12345");
        foreach (var unread in unreadNotifications)
        {
            Console.WriteLine($"Unread: {unread.Title} - {unread.Message}");
        }
        
        // Mark as read
        bool markedAsRead = await notificationService.MarkAsReadAsync(sentNotification.Id);
        Console.WriteLine($"Marked as read: {markedAsRead}");
        
        // Get notification history
        var history = await notificationService.GetNotificationHistoryAsync("user-12345", limit: 10);
        Console.WriteLine($"Total notifications: {history.Count()}");
        
        // Delete notification
        bool deleted = await notificationService.DeleteNotificationAsync(sentNotification.Id);
        Console.WriteLine($"Deleted: {deleted}");
    }
}
```

This example demonstrates creating a `Notification` instance with all properties, registering the notification service, and using its public methods to send, retrieve, mark as read, and delete notifications.

## AuditLogEntry

The `AuditLogEntry` class represents a single audit log entry for tracking system events, user actions, and data changes in a multi-tenant application. It stores metadata about who performed what action on which resource, when, and with what outcome, enabling compliance auditing, security monitoring, and troubleshooting.

**Key capabilities:**
- Track tenant-specific actions (create, read, update, delete, login, etc.)
- Store user context and IP address for security auditing
- Capture change sets for data modifications
- Support filtering by tenant, user, or resource
- Enforce retention policies with automatic cleanup

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Models;
using TenantIsolation.Services;

public class AuditLoggingExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddSingleton<IAuditLogger, AuditLogger>();

        var provider = services.BuildServiceProvider();
        var auditLogger = provider.GetRequiredService<IAuditLogger>();

        var tenantId = Guid.NewGuid();
        var userId = "user-12345";

        // Create a new audit log entry
        var entry = new AuditLogEntry
        {
            TenantId = tenantId,
            UserId = userId,
            Action = "Create User",
            Resource = "User",
            ResourceId = "user-12345",
            ActionType = AuditAction.Create,
            Details = "Creating new user account for john.doe@example.com",
            Success = true,
            IpAddress = "192.168.1.100",
            ChangeSet = new Dictionary<string, object>
            {
                { "Email", "john.doe@example.com" },
                { "Role", "Administrator" },
                { "FirstName", "John" },
                { "LastName", "Doe" }
            }
        };

        // Log the audit entry
        await auditLogger.LogAsync(entry);
        Console.WriteLine($"Audit entry logged: {entry.Id} at {entry.Timestamp}");

        // Use convenience extension methods
        await auditLogger.LogCreateAsync(
            tenantId,
            userId,
            "User",
            "user-12345",
            new Dictionary<string, object>
            {
                { "Email", "jane.doe@example.com" },
                { "Role", "User" }
            }
        );

        // Retrieve audit logs
        var tenantLogs = await auditLogger.GetLogsAsync(tenantId, limit: 50);
        Console.WriteLine($"Found {tenantLogs.Count()} logs for tenant");

        var userLogs = await auditLogger.GetUserLogsAsync(userId, limit: 20);
        Console.WriteLine($"Found {userLogs.Count()} logs for user");

        var resourceLogs = await auditLogger.GetResourceLogsAsync(
            tenantId,
            "User",
            "user-12345",
            limit: 10
        );
        Console.WriteLine($"Found {resourceLogs.Count()} logs for specific resource");

        // Clean up old logs (retention policy)
        await auditLogger.ClearOldLogsAsync(retentionDays: 90);
    }
}

```

## TracingContext

The `TracingContext` class provides distributed tracing capabilities for tracking requests across multiple services in multi-tenant applications. It captures correlation IDs, trace IDs, span IDs, tenant context, user context, and metadata to enable comprehensive request tracing and debugging.

**Key capabilities:**
- Track requests with correlation IDs and trace IDs
- Support nested operations with parent/child span relationships
- Include tenant and user context for multi-tenant applications
- Store operation metadata for debugging and monitoring
- Provide structured logging integration

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Utilities;

public class TracingExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddDistributedTracing();

        var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<TracingExample>();

        // Create a new tracing context for a request
        var tracingContext = new TracingContext
        {
            RequestPath = "/api/users/get",
            TenantId = Guid.Parse("3fa85f6-4717-4562-b3fc-2c963f66afa6"),
            UserId = "user-12345"
        };

        // Add custom metadata
        tracingContext.Metadata["operation"] = "GetUserDetails";
        tracingContext.Metadata["user_type"] = "premium";

        // Set as current context
        DistributedTracingExtensions.SetCurrentContext(tracingContext);

        // Log with tracing context
        logger.LogWithTracing(LogLevel.Information, "Starting user details operation");

        // Create child context for nested operation
        var childContext = DistributedTracingExtensions.CreateChildContext("DatabaseQuery");
        childContext.Metadata["query"] = "SELECT * FROM Users WHERE Id = @id";

        using (DistributedTracingExtensions.BeginTracingScope(childContext))
        {
            // Simulate database operation
            logger.LogWithTracing(LogLevel.Debug, "Executing database query");
            
            // Add more metadata during operation
            DistributedTracingExtensions.AddMetadata("query_duration_ms", "42");
        }

        // Get current context
        var currentContext = DistributedTracingExtensions.GetCurrentContext();
        Console.WriteLine($"Correlation ID: {currentContext?.CorrelationId}");
        Console.WriteLine($"Trace ID: {currentContext?.TraceId}");
        Console.WriteLine($"Span ID: {currentContext?.SpanId}");
        Console.WriteLine($"Tenant ID: {currentContext?.TenantId}");
        Console.WriteLine($"User ID: {currentContext?.UserId}");

        // Use structured logging state
        var logState = DistributedTracingExtensions.GetTracingLogState();
        foreach (var kvp in logState)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }

        // Execute operation with automatic tracing
        var result = await DistributedTracingExtensions.ExecuteWithTracingAsync(
            "ProcessUserRequest",
            async () =>
            {
                await Task.Delay(100); // Simulate work
                return "User processed successfully";
            },
            logger
        );

        Console.WriteLine($"Result: {result}");
    }
}
```

## ITimeProvider

The `ITimeProvider` interface provides a dependency injection-friendly abstraction for time operations, enabling testable and mockable time-dependent code in multi-tenant applications. It supports both real system time and mock time for testing scenarios.

**Key capabilities:**
- Get current UTC, local, or date-only time
- Convert between UTC and tenant-specific timezones
- Check business hours and calculate deadlines
- Mock time for deterministic testing

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Utilities;

public class TimeProviderExample
{
    public static void Main(string[] args)
    {
        // Setup dependency injection with real time provider
        var services = new ServiceCollection();
        services.AddTimeProvider(useMock: false);
        
        var provider = services.BuildServiceProvider();
        var timeProvider = provider.GetRequiredService<ITimeProvider>();
        
        // Get current time
        Console.WriteLine($"Current UTC time: {timeProvider.UtcNow}");
        Console.WriteLine($"Current local time: {timeProvider.Now}");
        Console.WriteLine($"Current date: {timeProvider.Today}");
        Console.WriteLine($"Timezone: {timeProvider.TimeZone.DisplayName}");
        
        // Convert between UTC and tenant time
        var tenantTimeZoneId = "Eastern Standard Time";
        var utcTime = DateTime.UtcNow;
        var tenantTime = timeProvider.ConvertToTenantTime(utcTime, tenantTimeZoneId);
        Console.WriteLine($"Tenant time: {tenantTime}");
        
        var backToUtc = timeProvider.ConvertFromTenantTime(tenantTime, tenantTimeZoneId);
        Console.WriteLine($"Back to UTC: {backToUtc}");
        
        // Check business hours
        bool isBusinessHours = timeProvider.IsBusinessHours(DateTime.UtcNow);
        Console.WriteLine($"Is business hours: {isBusinessHours}");
        
        // Calculate deadlines
        var deadline = timeProvider.CalculateSlaDeadline(businessHoursNeeded: 2);
        Console.WriteLine($"SLA deadline in 2 business hours: {deadline}");
        
        var timeUntilDeadline = timeProvider.GetTimeUntilDeadline(deadline);
        Console.WriteLine($"Time until deadline: {timeUntilDeadline.TotalMinutes} minutes");
        
        bool deadlineExceeded = timeProvider.IsDeadlineExceeded(deadline);
        Console.WriteLine($"Deadline exceeded: {deadlineExceeded}");
    }
}
```

### Using Mock Time Provider for Testing

```csharp
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Utilities;

public class TimeProviderTestingExample
{
    public static void Main(string[] args)
    {
        // Setup dependency injection with mock time provider
        var services = new ServiceCollection();
        services.AddTimeProvider(useMock: true);
        
        var provider = services.BuildServiceProvider();
        var mockTimeProvider = provider.GetRequiredService<ITimeProvider>() as MockTimeProvider;
        
        // Set mock time
        mockTimeProvider.SetCurrentTime(new DateTime(2024, 1, 15, 10, 30, 0));
        Console.WriteLine($"Mock time set to: {mockTimeProvider.UtcNow}");
        
        // Advance time
        mockTimeProvider.AdvanceTime(TimeSpan.FromHours(2));
        Console.WriteLine($"Time advanced by 2 hours: {mockTimeProvider.UtcNow}");
        
        // Reset to system time
        mockTimeProvider.Reset();
        Console.WriteLine($"Reset to system time: {mockTimeProvider.UtcNow}");
    }
}
```

## ValidationUtility

The `ValidationUtility` class provides centralized validation methods for common data validation patterns across the tenant isolation framework. It includes validation for email addresses, slugs, GUIDs, URLs, and various string and numeric constraints, with both boolean check methods and exception-throwing validation methods for different use cases.

**Key capabilities:**
- Validate email format (RFC-compliant regex pattern)
- Validate tenant slug format (lowercase alphanumeric with hyphens, 3-63 characters)
- Validate GUID format
- Validate URL format (HTTP/HTTPS only)
- Validate string length constraints (min, max, range)
- Validate numeric ranges and positivity
- Validate date relationships (future, past, valid ranges)
- Validate enum values
- Throw `TenantIsolationException` for invalid values or provide boolean results

**Usage example**

```csharp
using TenantIsolation.Utilities;
using TenantIsolation.Exceptions;

public class ValidationExample
{
    public static void Main(string[] args)
    {
        // Validate email format
        string email = "user@example.com";
        bool isEmailValid = ValidationUtility.IsValidEmail(email);
        Console.WriteLine($"Email '{email}' is valid: {isEmailValid}");

        // Validate tenant slug
        string slug = "acme-corp";
        bool isSlugValid = ValidationUtility.IsValidSlug(slug);
        Console.WriteLine($"Slug '{slug}' is valid: {isSlugValid}");

        // Validate GUID
        string guid = Guid.NewGuid().ToString();
        bool isGuidValid = ValidationUtility.IsValidGuid(guid);
        Console.WriteLine($"GUID '{guid}' is valid: {isGuidValid}");

        // Validate URL
        string url = "https://example.com/api/users";
        bool isUrlValid = ValidationUtility.IsValidUrl(url);
        Console.WriteLine($"URL '{url}' is valid: {isUrlValid}");

        // String length validation
        string name = "John Doe";
        ValidationUtility.RequireMinLength(name, 3, nameof(name));
        ValidationUtility.RequireMaxLength(name, 50, nameof(name));
        ValidationUtility.RequireLengthBetween(name, 3, 50, nameof(name));

        // Numeric validation
        int userCount = 42;
        ValidationUtility.RequirePositive(userCount, nameof(userCount));
        ValidationUtility.RequireRange(userCount, 1, 1000, nameof(userCount));

        // Date validation
        DateTime futureDate = DateTime.UtcNow.AddDays(30);
        ValidationUtility.RequireFutureDate(futureDate, nameof(futureDate));

        DateTime pastDate = DateTime.UtcNow.AddDays(-1);
        ValidationUtility.RequirePastDate(pastDate, nameof(pastDate));

        // Date range validation
        DateTime startDate = DateTime.UtcNow;
        DateTime endDate = DateTime.UtcNow.AddDays(7);
        ValidationUtility.RequireValidDateRange(startDate, endDate, nameof(startDate), nameof(endDate));

        // Enum validation
        ValidationUtility.RequireValidEnum(UserRole.Administrator);

        // Exception-throwing validation methods
        try
        {
            ValidationUtility.RequireNotEmpty(null, nameof(email));
        }
        catch (TenantIsolationException ex)
        {
            Console.WriteLine($"Validation failed: {ex.Message}");
        }

        try
        {
            ValidationUtility.RequireValidEmail("invalid-email");
        }
        catch (TenantIsolationException ex)
        {
            Console.WriteLine($"Email validation failed: {ex.Message}");
        }

        try
        {
            ValidationUtility.RequireValidSlug("Invalid Slug");
        }
        catch (TenantIsolationException ex)
        {
            Console.WriteLine($"Slug validation failed: {ex.Message}");
        }

        try
        {
            ValidationUtility.RequireValidGuid("not-a-guid", nameof(guid));
        }
        catch (TenantIsolationException ex)
        {
            Console.WriteLine($"GUID validation failed: {ex.Message}");
        }

        try
        {
            ValidationUtility.RequireValidUrl("ftp://invalid.com", nameof(url));
        }
        catch (TenantIsolationException ex)
        {
            Console.WriteLine($"URL validation failed: {ex.Message}");
        }
    }
}

public enum UserRole { Administrator, User, Guest }
```

## WebhookController

The `WebhookController` provides RESTful API endpoints for managing webhook subscriptions in multi-tenant applications. It allows tenants to register, retrieve, update, and delete webhook endpoints for receiving event notifications, with support for delivery history and testing capabilities.

**Key capabilities:**
- Register webhook endpoints for specific event types
- Retrieve webhook subscriptions by ID or tenant
- Delete webhook subscriptions
- View webhook delivery history
- Test webhook endpoints with sample payloads
- Secure webhook endpoints with optional secrets for signature verification

**Public members:**
- `TenantId` - Tenant identifier
- `EventType` - Event type to subscribe to
- `Url` - Webhook endpoint URL
- `Secret` - Optional secret for webhook signature verification
- `RegisterWebhook(RegisterWebhookRequest)` - Register a new webhook endpoint
- `GetWebhook(Guid)` - Get webhook by ID
- `GetTenantWebhooks(string, string?)` - Get all webhooks for a tenant
- `DeleteWebhook(Guid)` - Delete webhook subscription
- `GetWebhookDeliveries(Guid, int)` - Get webhook delivery history
- `TestWebhook(Guid)` - Test webhook by sending a sample payload

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Controllers;
using TenantIsolation.Models;

public class WebhookControllerExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddScoped<WebhookController>();
        
        // Mock implementations for example
        services.AddScoped<IWebhookHandler>(_ => new MockWebhookHandler());
        services.AddScoped<IResponseFormatter, ResponseFormatter>();
        
        var provider = services.BuildServiceProvider();
        var webhookController = provider.GetRequiredService<WebhookController>();
        
        // Register a new webhook subscription
        var registerRequest = new WebhookController.RegisterWebhookRequest
        {
            TenantId = Guid.NewGuid().ToString(),
            EventType = "user.created",
            Url = "https://webhook.site/12345-abcde",
            Secret = "my-secret-key-for-signature-verification"
        };
        
        var registerResult = await webhookController.RegisterWebhook(registerRequest);
        var subscription = ((Microsoft.AspNetCore.Mvc.CreatedAtActionResult)registerResult.Result).Value as ApiResponse<WebhookSubscription>;
        Console.WriteLine($"Registered webhook: {subscription?.Data?.Id}");
        
        // Get webhook by ID
        var getResult = await webhookController.GetWebhook(subscription.Data.Id);
        var retrievedWebhook = ((Microsoft.AspNetCore.Mvc.OkObjectResult)getResult.Result).Value as ApiResponse<WebhookSubscription>;
        Console.WriteLine($"Retrieved webhook: {retrievedWebhook?.Data?.Url}");
        
        // Get all webhooks for a tenant
        var tenantId = Guid.NewGuid().ToString();
        var tenantWebhooksResult = await webhookController.GetTenantWebhooks(tenantId, "user.created");
        var tenantWebhooks = ((Microsoft.AspNetCore.Mvc.OkObjectResult)tenantWebhooksResult.Result).Value as ApiResponse<List<WebhookSubscription>>;
        Console.WriteLine($"Tenant webhooks count: {tenantWebhooks?.Data?.Count}");
        
        // Get webhook delivery history
        var deliveriesResult = await webhookController.GetWebhookDeliveries(subscription.Data.Id, 10);
        var deliveries = ((Microsoft.AspNetCore.Mvc.OkObjectResult)deliveriesResult.Result).Value as ApiResponse<List<WebhookDelivery>>;
        Console.WriteLine($"Delivery history count: {deliveries?.Data?.Count}");
        
        // Test webhook by sending sample payload
        var testResult = await webhookController.TestWebhook(subscription.Data.Id);
        var testResponse = ((Microsoft.AspNetCore.Mvc.OkObjectResult)testResult.Result).Value as ApiResponse<object>;
        Console.WriteLine($"Test initiated: {testResponse?.Message}");
        
        // Delete webhook subscription
        var deleteResult = await webhookController.DeleteWebhook(subscription.Data.Id);
        var deleteResponse = ((Microsoft.AspNetCore.Mvc.OkObjectResult)deleteResult.Result).Value as ApiResponse<object>;
        Console.WriteLine($"Webhook deleted: {deleteResponse?.Success}");
    }
}

// Mock implementation for example
public class MockWebhookHandler : IWebhookHandler
{
    public Task<WebhookSubscription> RegisterWebhookAsync(Guid tenantId, string eventType, string url, string? secret)
    {
        return Task.FromResult(new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = eventType,
            Url = url,
            Secret = secret,
            CreatedAt = DateTime.UtcNow
        });
    }
    
    public Task<WebhookSubscription?> GetWebhookByIdAsync(Guid id) => Task.FromResult<WebhookSubscription?>(null);
    public Task<IEnumerable<WebhookSubscription>> GetWebhooksAsync(Guid tenantId, string? eventType) => Task.FromResult(Enumerable.Empty<WebhookSubscription>());
    public Task<bool> UnregisterWebhookAsync(Guid id) => Task.FromResult(false);
    public Task<IEnumerable<WebhookDelivery>> GetDeliveryHistoryAsync(Guid webhookId, int limit) => Task.FromResult(Enumerable.Empty<WebhookDelivery>());
}
```

## DynamicTenantStore
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddDbContext<TenantDbContext>();
        services.AddScoped<TenantRepository>();
        
        // Configure tenant isolation options
        services.Configure<TenantIsolationOptions>(options =>
        {
            options.DynamicTenantStoreReloadIntervalMinutes = 5; // Reload every 5 minutes
        });
        
        // Register DynamicTenantStore as a service
        services.AddSingleton<IDynamicTenantStore, DynamicTenantStore>();
        services.AddSingleton<DynamicTenantStore>(); // Also register concrete type
        
        var provider = services.BuildServiceProvider();
        
        // Get the tenant store instance
        var tenantStore = provider.GetRequiredService<DynamicTenantStore>();
        
        // Start automatic reloading of tenant data
        tenantStore.StartReloading();
        
        // Get all active tenants
        var activeTenants = await tenantStore.GetAllActiveTenantsAsync();
        Console.WriteLine($"Active tenants count: {activeTenants.Count()}");
        
        // Get a specific tenant by ID
        var tenantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var tenant = await tenantStore.GetTenantByIdAsync(tenantId);
        
        if (tenant != null)
        {
            Console.WriteLine($"Found tenant: {tenant.Name} (Slug: {tenant.Slug}, Status: {tenant.Status})");
        }
        
        // Subscribe to tenant change events
        tenantStore.OnTenantRegistered += (sender, e) =>
        {
            Console.WriteLine($"Tenant registered: {e.Tenant.Name} (Id: {e.Tenant.Id})");
        };
        
        tenantStore.OnTenantRemoved += (sender, e) =>
        {
            Console.WriteLine($"Tenant removed: {e.Tenant.Name} (Id: {e.Tenant.Id})");
        };
        
        // When shutting down the application, stop the reloading timer
        // tenantStore.StopReloading();
        
        // Dispose when done (stops the timer)
        tenantStore.Dispose();
    }
}
```

## DependencyInjectionExtensions

The `DependencyInjectionExtensions` class provides extension methods for registering tenant isolation services in ASP.NET Core applications. It simplifies the setup process by offering methods to configure the core services including the tenant-aware DbContext, repositories, and resolution services.

**Key capabilities:**
- Register tenant isolation with various database providers (In-Memory, SQL Server, PostgreSQL)
- Configure tenant resolution middleware for request pipeline
- Register core services including repositories, configuration, and feature toggles
- Support for automatic database migration and soft-delete filtering
- Enable/disable various framework features like audit logging, caching, and health checks

**Usage examples**

### Basic Setup with SQL Server

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;
using TenantIsolation.Data;

public class Program
{
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();

        // Configure tenant isolation with SQL Server
        services.AddTenantIsolation(options =>
            options.UseSqlServer("Server=(localdb)\mssqllocaldb;Database=TenantIsolationDb;Trusted_Connection=True;MultipleActiveResultSets=true"));

        // Build service provider
        var provider = services.BuildServiceProvider();

        // Resolve services
        var dbContextFactory = provider.GetRequiredService<ITenantDbContextFactory<TenantDbContext>>();
        var tenantService = provider.GetRequiredService<TenantService>();
    }
}
```

### Setup with In-Memory Database (for testing)

```csharp
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();

        // Configure tenant isolation with in-memory database
        services.AddTenantIsolationInMemory(databaseName: "TestTenantIsolationDb");

        // Build service provider
        var provider = services.BuildServiceProvider();

        // Resolve services
        var dbContextFactory = provider.GetRequiredService<ITenantDbContextFactory<TenantDbContext>>();
    }
}
```

### Advanced Setup with Custom Options

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();

        // Configure tenant isolation with custom options
        services.AddTenantIsolation(
            options => options.UseSqlServer("Server=sql-server;Database=TenantIsolationDb;User Id=sa;Password=YourPassword123;"),
            configureOptions: options =>
            {
                options.AutoMigrate = true;
                options.EnableSoftDeleteFilter = true;
                options.EnableAuditLogging = true;
                options.EnableCaching = true;
                options.EnableHealthChecks = true;
                options.MaxConcurrentTenants = 500;
            });

        // Build service provider
        var provider = services.BuildServiceProvider();
    }
}
```

### Using Tenant Resolution Middleware

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;
using TenantIsolation.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Register tenant isolation services
builder.Services.AddTenantIsolation(options =>
    options.UseSqlServer("Server=(localdb)\mssqllocaldb;Database=TenantIsolationDb;Trusted_Connection=True"));

var app = builder.Build();

// Use tenant resolution middleware in the pipeline
app.UseTenantResolution();

// Add other middleware
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapGet("/", () => "Hello World!");
app.Run();
```

### Registering Feature Toggle Services

```csharp
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;
using TenantIsolation.Services;

public class Program
{
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();

        // Register tenant isolation
        services.AddTenantIsolationInMemory();

        // Register feature toggle service
        services.AddTenantFeatureToggle();

        // Build service provider
        var provider = services.BuildServiceProvider();

        // Resolve feature service
        var featureService = provider.GetRequiredService<TenantFeatureService>();
    }
}
```

## JsonUtility

The `JsonUtility` class provides consistent JSON serialization and deserialization utilities for the tenant isolation framework. It handles JSON conversion with standardized options including camelCase naming, null value handling, and cycle reference management. The class offers both compact and pretty-print serialization, safe deserialization methods, and utilities for JSON manipulation and validation.

**Key capabilities:**
- Standardized JSON serialization with camelCase naming policy
- Pretty-print JSON output for logging and debugging
- Safe deserialization with error handling
- JSON validation and formatting utilities
- Dynamic JSON manipulation and property extraction
- Object-to-dictionary conversion for parameter passing

**Usage example**

```csharp
using System;
using System.Collections.Generic;
using TenantIsolation.Utilities;

public class JsonUtilityExample
{
    public static void Main(string[] args)
    {
        // Create a sample object to serialize
        var user = new
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            IsActive = true,
            Preferences = new Dictionary<string, object>
            {
                { "theme", "dark" },
                { "language", "en-US" },
                { "notificationsEnabled", true }
            },
            Metadata = new
            {
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Serialize to compact JSON
        var compactJson = JsonUtility.Serialize(user);
        Console.WriteLine("Compact JSON:");
        Console.WriteLine(compactJson);

        // Serialize to pretty JSON with indentation
        var prettyJson = JsonUtility.SerializePretty(user);
        Console.WriteLine("\nPretty JSON:");
        Console.WriteLine(prettyJson);

        // Deserialize back to object
        var deserializedUser = JsonUtility.Deserialize<Dictionary<string, object>>(compactJson);
        Console.WriteLine($"\nDeserialized user email: {deserializedUser?["email"]}");

        // Safe deserialization (returns null on failure)
        var safeResult = JsonUtility.DeserializeSafe<Dictionary<string, object>>("invalid json");
        Console.WriteLine($"Safe deserialization result: {safeResult == null}");

        // Validate JSON
        var isValid = JsonUtility.IsValidJson(compactJson);
        Console.WriteLine($"\nIs valid JSON: {isValid}");

        // Pretty print existing JSON
        var minified = JsonUtility.Minify(prettyJson);
        Console.WriteLine($"\nMinified length: {minified.Length} chars");

        var prettyAgain = JsonUtility.PrettyPrint(minified);
        Console.WriteLine($"Pretty print length: {prettyAgain.Length} chars");

        // Get property value from JSON string
        var emailValue = JsonUtility.GetPropertyValue(compactJson, "email");
        Console.WriteLine($"\nExtracted email: {emailValue}");

        // Convert object to dictionary
        var userDict = JsonUtility.ConvertToDictionary(user);
        Console.WriteLine($"\nUser dictionary keys: {string.Join(", ", userDict.Keys)}");

        // Create custom serializer options
        var customOptions = JsonUtility.CreateCustomOptions(indented: true, ignoreNulls: false);
        Console.WriteLine($"\nCustom options configured: indented={customOptions.WriteIndented}, ignoreNulls={customOptions.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull}");
    }
}
```

## CryptographyUtility

The `CryptographyUtility` class provides cryptographic operations for hashing, encryption, and security in multi-tenant applications. It implements secure algorithms like SHA-256, SHA-512, HMAC-SHA256, AES-256 encryption, and PBKDF2 password hashing to ensure data protection and identity verification.

**Key capabilities:**
- Generate cryptographic hashes (SHA-256, SHA-512) for data integrity
- Create secure random tokens and codes for authentication
- Encrypt and decrypt sensitive data using AES-256
- Generate and verify HMAC signatures for message authentication
- Secure password hashing with salt using PBKDF2
- Generate cryptographically secure GUIDs and fingerprints

**Usage example**

```csharp
using System;
using TenantIsolation.Utilities;

public class CryptographyExample
{
    public static void Main(string[] args)
    {
        // Generate cryptographic hashes
        string data = "sensitive-data-123";
        string sha256Hash = CryptographyUtility.GenerateSha256Hash(data);
        string sha512Hash = CryptographyUtility.GenerateSha512Hash(data);
        
        Console.WriteLine($"SHA256 hash: {sha256Hash}");
        Console.WriteLine($"SHA512 hash: {sha512Hash}");

        // Generate secure random tokens and codes
        string secureToken = CryptographyUtility.GenerateSecureToken(32);
        string randomCode = CryptographyUtility.GenerateRandomNumericCode(6);
        string randomPassword = CryptographyUtility.GenerateRandomString(12, true);
        
        Console.WriteLine($"Secure token: {secureToken}");
        Console.WriteLine($"Random code: {randomCode}");
        Console.WriteLine($"Random password: {randomPassword}");

        // Generate and verify HMAC signatures
        string secretKey = "my-secret-key";
        string message = "webhook-payload";
        string signature = CryptographyUtility.GenerateHmacSha256(message, secretKey);
        bool isValid = CryptographyUtility.VerifyHmacSha256(message, signature, secretKey);
        
        Console.WriteLine($"HMAC signature: {signature}");
        Console.WriteLine($"Signature valid: {isValid}");

        // Encrypt and decrypt sensitive data
        string plainText = "Top secret message";
        string encryptionKey = "strong-encryption-key-123";
        string encrypted = CryptographyUtility.EncryptAes256(plainText, encryptionKey);
        string decrypted = CryptographyUtility.DecryptAes256(encrypted, encryptionKey);
        
        Console.WriteLine($"Encrypted: {encrypted}");
        Console.WriteLine($"Decrypted: {decrypted}");

        // Secure password hashing and verification
        string password = "user-password-123";
        var (hash, salt) = CryptographyUtility.HashPassword(password);
        bool passwordValid = CryptographyUtility.VerifyPassword(password, hash, salt);
        
        Console.WriteLine($"Password hash: {hash}");
        Console.WriteLine($"Salt: {salt}");
        Console.WriteLine($"Password valid: {passwordValid}");

        // Generate secure GUID and fingerprint
        Guid secureGuid = CryptographyUtility.GenerateSecureGuid();
        string fingerprint = CryptographyUtility.ComputeFingerprint("device-id", "hardware-info", "os-version");
        
        Console.WriteLine($"Secure GUID: {secureGuid}");
        Console.WriteLine($"Device fingerprint: {fingerprint}");
    }
}
```

## CollectionExtensions

The `CollectionExtensions` class provides a comprehensive set of extension methods for working with collections, lists, dictionaries, and enumerables in .NET applications. These methods offer safe collection manipulation patterns, prevent common exceptions like `IndexOutOfRangeException`, and provide convenient operations for filtering, partitioning, and transforming collections while maintaining thread safety and proper null handling.


Here's an example usage:

```csharp
using System;
using System.Collections.Generic;
using TenantIsolation.Utilities;

public class CollectionExtensionsExample
{
    public static void Main(string[] args)
    {
        // Create sample collections
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Name = "Alice", Age = 30 },
            new User { Id = Guid.NewGuid(), Name = "Bob", Age = 25 },
            new User { Id = Guid.NewGuid(), Name = "Charlie", Age = 35 },
            new User { Id = Guid.NewGuid(), Name = "Diana", Age = 28 }
        };

        var userDictionary = new Dictionary<Guid, User>
        {
            { users[0].Id, users[0] },
            { users[1].Id, users[1] }
        };

        // Use CollectionExtensions methods

        // Check if collection has items
        bool hasItems = users.HasItems();
        Console.WriteLine($"Collection has items: {hasItems}");

        // Safely get item at index
        var firstUser = users.SafeGetAt(0);
        var outOfBoundsUser = users.SafeGetAt(10); // returns null
        Console.WriteLine($"First user: {firstUser?.Name}, Out of bounds: {outOfBoundsUser?.Name}");

        // Add if not exists
        users.AddIfNotExists(new User { Id = Guid.NewGuid(), Name = "Alice", Age = 30 }); // Won't add duplicate
        Console.WriteLine($"List count after AddIfNotExists: {users.Count}");

        // Add range
        users.AddRange(new[] { 
            new User { Id = Guid.NewGuid(), Name = "Eve", Age = 22 },
            new User { Id = Guid.NewGuid(), Name = "Frank", Age = 27 }
        });
        Console.WriteLine($"List count after AddRange: {users.Count}");

        // Remove where
        int removedCount = users.RemoveWhere(u => u.Age < 25);
        Console.WriteLine($"Removed {removedCount} users under 25");

        // Distinct by
        var usersWithDuplicates = new List<User>
        {
            new User { Id = Guid.NewGuid(), Name = "Alice", Age = 30 },
            new User { Id = Guid.NewGuid(), Name = "Alice", Age = 31 },
            new User { Id = Guid.NewGuid(), Name = "Bob", Age = 25 }
        };
        var distinctUsers = usersWithDuplicates.DistinctBy(u => u.Name);
        Console.WriteLine($"Distinct users by name: {distinctUsers.Count()}");

        // Chunk
        var chunks = users.Chunk(2);
        foreach (var chunk in chunks)
        {
            Console.WriteLine($"Chunk: {chunk.Count()} users");
        }

        // Safe dictionary access
        var user = userDictionary.SafeGet(users[0].Id);
        var missingUser = userDictionary.SafeGet(Guid.NewGuid()); // returns null
        Console.WriteLine($"Found user: {user?.Name}, Missing user: {missingUser?.Name}");

        // Get value or default
        var defaultUser = userDictionary.GetValueOrDefault(Guid.NewGuid(), new User { Name = "Default" });
        Console.WriteLine($"Default user: {defaultUser.Name}");

        // MaxBy and MinBy
        var oldestUser = users.MaxBy(u => u.Age);
        var youngestUser = users.MinBy(u => u.Age);
        Console.WriteLine($"Oldest: {oldestUser?.Name} ({oldestUser?.Age}), Youngest: {youngestUser?.Name} ({youngestUser?.Age})");

        // Partition
        var (adults, minors) = users.Partition(u => u.Age >= 18);
        Console.WriteLine($"Adults: {adults.Count()}, Minors: {minors.Count()}");

        // Get intersection
        var moreUsers = new List<User>
        {
            new User { Id = Guid.NewGuid(), Name = "George" },
            new User { Id = Guid.NewGuid(), Name = "Alice" }
        };
        var intersection = users.GetIntersection(moreUsers);
        Console.WriteLine($"Intersection count: {intersection.Count()}");

        // Get difference
        var difference = users.GetDifference(moreUsers);
        Console.WriteLine($"Difference count: {difference.Count()}");

        // Flatten
        var nestedLists = new List<List<User>>
        {
            new List<User> { users[0], users[1] },
            new List<User> { users[2] }
        };
        var flattened = nestedLists.Flatten();
        Console.WriteLine($"Flattened count: {flattened.Count()}");

        // Page
        var page1 = users.Page(1, 2);
        var page2 = users.Page(2, 2);
        Console.WriteLine($"Page 1 count: {page1.Count()}, Page 2 count: {page2.Count()}");
    }
}

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

## DateTimeExtensions

The `DateTimeExtensions` class provides a comprehensive set of extension methods for common date and time operations in multi-tenant applications. It includes methods for getting the start/end of various time periods (day, week, month, year), checking date relationships (today, past, future), calculating business days, and generating human-readable relative time strings.





Here's an example usage demonstrating the key public members:

```csharp
using System;
using TenantIsolation.Utilities;

public class DateTimeExtensionsExample
{
    public static void Main(string[] args)
    {
        var now = DateTime.UtcNow;
        var today = now.StartOfDay();
        var endOfDay = now.EndOfDay();
        
        Console.WriteLine($"Start of day: {today:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"End of day: {endOfDay:yyyy-MM-dd HH:mm:ss}");
        
        // Week operations
        var startOfWeek = now.StartOfWeek();
        var endOfWeek = now.EndOfWeek();
        Console.WriteLine($"Week starts: {startOfWeek:yyyy-MM-dd} (Monday)");
        Console.WriteLine($"Week ends: {endOfWeek:yyyy-MM-dd} (Sunday)");
        
        // Month operations
        var startOfMonth = now.StartOfMonth();
        var endOfMonth = now.EndOfMonth();
        Console.WriteLine($"Month starts: {startOfMonth:yyyy-MM-dd}");
        Console.WriteLine($"Month ends: {endOfMonth:yyyy-MM-dd}");
        
        // Date checks
        Console.WriteLine($"Is today: {now.IsToday()}");
        Console.WriteLine($"Is past: {now.AddDays(-1).IsPast()}");
        Console.WriteLine($"Is future: {now.AddDays(1).IsFuture()}");
        
        // Relative time
        var yesterday = now.AddDays(-1);
        var tomorrow = now.AddDays(1);
        Console.WriteLine($"Yesterday: {yesterday.ToRelativeTime()}");
        Console.WriteLine($"Tomorrow: {tomorrow.ToRelativeTime()}");
        
        // Business days
        var businessDaysAdded = now.AddBusinessDays(5);
        Console.WriteLine($"5 business days from now: {businessDaysAdded:yyyy-MM-dd}");
        
        var daysBetween = now.GetBusinessDaysBetween(now.AddDays(-7), now);
        Console.WriteLine($"Business days in last week: {daysBetween}");
        
        // Age calculation
        var birthDate = new DateTime(1990, 5, 15);
        var age = birthDate.GetAgeInYears();
        Console.WriteLine($"Age in years: {age}");
        
        // ISO 8601 format
        var isoDate = now.ToIso8601String();
        Console.WriteLine($"ISO 8601 format: {isoDate}");
        
        // Range check
        var inRange = now.IsInRange(now.AddDays(-1), now.AddDays(1));
        Console.WriteLine($"Is in range: {inRange}");
        
        // Expiry checks
        var expiryDate = now.AddDays(30);
        Console.WriteLine($"Is expiring within 7 days: {expiryDate.IsExpiringWithin(7)}");
        Console.WriteLine($"Has expired: {expiryDate.AddDays(-60).HasExpired()}");
    }
}
```

## ServiceRegistrationExtensions

The `ServiceRegistrationExtensions` class provides comprehensive service registration methods for configuring Phase 2 features of the tenant isolation framework. It simplifies setup by offering extension methods to register all framework services in one call, with support for custom configuration through options.

**Key capabilities:**
- Register all Phase 2 services with default or custom options
- Configure tenant-aware caching (automatically detects distributed vs in-memory cache)
- Set up middleware pipeline with error handling, logging, rate limiting, and tenant resolution
- Register background services for cleanup and maintenance tasks
- Configure audit logging, notifications, health checks, and export services
- Enable/disable features like event bus, webhooks, and distributed tracing

**Usage examples**

### Basic Setup with Default Options

```csharp
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        
        // Register all Phase 2 services with default options
        services.AddTenantIsolationPhase2Services();
        
        // Build service provider
        var provider = services.BuildServiceProvider();
    }
}
```

### Advanced Setup with Custom Options

```csharp
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        
        // Register all Phase 2 services with custom configuration
        services.AddTenantIsolationPhase2Services(options =>
        {
            options.EnableCaching = true;
            options.EnableEventBus = true;
            options.EnableNotifications = true;
            options.EnableHealthChecks = true;
            options.EnableBackgroundTasks = true;
            options.EnableAuditLogging = true;
            options.EnableDistributedTracing = true;
            options.EnableExternalApiClient = true;
            options.EnableWebhooks = true;
        });
        
        // Build service provider
        var provider = services.BuildServiceProvider();
    }
}
```

### Configuring Middleware Pipeline

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;
using TenantIsolation.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Register all Phase 2 services
builder.Services.AddTenantIsolationPhase2Services();

var app = builder.Build();

// Configure middleware pipeline with Phase 2 middleware
app.UseTenantIsolationPhase2Middleware();

// Log registered services during startup
app.LogPhase2ServicesOnStartup();

app.MapGet("/", () => "Hello World!");
app.Run();
```

### Tenant-Aware Caching Configuration

```csharp
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        
        // Register distributed cache (Redis, SQL Server, etc.)
        services.AddDistributedMemoryCache();
        
        // Register tenant-aware cache provider (automatically detects IDistributedCache)
        services.AddTenantAwareCacheProvider();
        
        // Build service provider
        var provider = services.BuildServiceProvider();
        
        // Resolve the cache provider
        var cacheProvider = provider.GetRequiredService<ICacheProvider>();
    }
}
```

## Repository

The `Repository<TEntity>` class is a generic base repository that provides tenant-aware CRUD operations for multi-tenant applications. It automatically handles tenant isolation through the `ITenantDbContextFactory<TenantDbContext>` and ensures all operations are scoped to the current tenant's database context.

**Key capabilities:**
- Generic CRUD operations for any entity type
- Tenant-aware database operations
- Pagination support for large datasets
- Bulk operations for performance
- Queryable interface for custom LINQ queries
- Soft-delete pattern support

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Models;
using TenantIsolation.Data;

public class ProductRepositoryExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddDbContext<TenantDbContext>();
        services.AddScoped<ITenantDbContextFactory<TenantDbContext>, TenantDbContextFactory<TenantDbContext>>();
        services.AddScoped(typeof(Repository<>));

        var provider = services.BuildServiceProvider();

        // Get repository instance for Product entity
        var repository = provider.GetRequiredService<Repository<Product>>();

        // Create a new product
        var newProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Premium Widget",
            Description = "High-quality widget for enterprise use",
            Price = 99.99m,
            StockQuantity = 100,
            Category = "Hardware"
        };

        // Add product to database
        var addedProduct = await repository.AddAsync(newProduct);
        Console.WriteLine($"Added product: {addedProduct.Name} (Id: {addedProduct.Id})");

        // Get product by ID
        var retrievedProduct = await repository.GetByIdAsync(addedProduct.Id);
        Console.WriteLine($"Retrieved product: {retrievedProduct?.Name}");

        // Update product
        retrievedProduct.Price = 89.99m;
        var updatedProduct = await repository.UpdateAsync(retrievedProduct);
        Console.WriteLine($"Updated product price to: {updatedProduct.Price}");

        // Get all products
        var allProducts = await repository.GetAllAsync();
        Console.WriteLine($"Total products: {allProducts.Count}");

        // Find products by criteria
        var expensiveProducts = await repository.FindAsync(p => p.Price > 50);
        Console.WriteLine($"Products over $50: {expensiveProducts.Count}");

        // Get first matching product
        var firstExpensive = await repository.FindFirstAsync(p => p.Price > 50);
        Console.WriteLine($"First expensive product: {firstExpensive?.Name}");

        // Check if product exists
        var exists = await repository.ExistsAsync(p => p.Name == "Premium Widget");
        Console.WriteLine($"Product exists: {exists}");

        // Count products
        var productCount = await repository.CountAsync();
        Console.WriteLine($"Total product count: {productCount}");

        // Pagination example
        var (pagedProducts, totalCount) = await repository.GetPagedAsync(pageNumber: 1, pageSize: 10);
        Console.WriteLine($"Page 1: {pagedProducts.Count} of {totalCount} total products");

        // Bulk update example
        var updatedCount = await repository.BulkUpdateAsync(
            p => p.Category == "Hardware",
            setters => setters.SetProperty(p => p.Price, p => p.Price * 0.9m) // 10% discount
        );
        Console.WriteLine($"Updated {updatedCount} products with bulk operation");

        // Bulk delete example
        var deletedCount = await repository.BulkDeleteAsync(p => p.StockQuantity == 0);
        Console.WriteLine($"Deleted {deletedCount} out-of-stock products");

        // Delete product
        var deleteSuccess = await repository.DeleteAsync(addedProduct.Id);
        Console.WriteLine($"Delete successful: {deleteSuccess}");

        // Use AsQueryable for custom queries
        var query = repository.AsQueryable()
            .Where(p => p.Price > 50)
            .OrderBy(p => p.Name)
            .Take(5);
        
        var topExpensive = await query.ToListAsync();
        Console.WriteLine($"Top 5 most expensive products: {topExpensive.Count}");
    }
}
```

This example demonstrates creating a generic `Repository<TEntity>` instance through dependency injection and using its public methods for common CRUD operations, bulk operations, and custom queries in a tenant-aware multi-tenant application.


## TenantDbContext

The `TenantDbContext` class is the Entity Framework Core database context for the tenant isolation framework. It manages entity configurations, relationships, and soft-delete filters for all tenant-related models including Tenants, Organizations, Users, TenantConfigurations, TenantConnectionStrings, DataIsolationPolicies, and TenantFeatures. The context automatically applies global query filters to exclude soft-deleted entities and provides timestamp management for CreatedAt and UpdatedAt fields.

Here's an example usage:

```csharp
using Microsoft.EntityFrameworkCore;
using TenantIsolation.Data;
using TenantIsolation.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class TenantDbContextExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        
        // Configure DbContext with SQL Server (example configuration)
        services.AddDbContext<TenantDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\mssqllocaldb;Database=TenantIsolationDb;Trusted_Connection=True;MultipleActiveResultSets=true"));
        
        var provider = services.BuildServiceProvider();
        
        // Get the DbContext instance
        var dbContext = provider.GetRequiredService<TenantDbContext>();
        
        // Create and add a new tenant
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = "acme-corp",
            Name = "ACME Corporation",
            AdminEmail = "admin@acme-corp.com",
            Status = TenantStatus.Active,
            IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant,
            PlanId = "enterprise-pro",
            MaxUsers = 500,
            MaxStorageGb = 1024.50m,
            SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        dbContext.Tenants.Add(tenant);
        
        // Create and add an organization
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Slug = "acme-corp-main",
            Name = "ACME Corporation Main Org",
            Industry = "Manufacturing",
            CountryCode = "US",
            RegistrationNumber = "REG-ACME-001",
            ContactEmail = "contact@acme-corp.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        dbContext.Organizations.Add(organization);
        
        // Create and add a user
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            OrganizationId = organization.Id,
            Email = "john.doe@acme-corp.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Administrator,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecurePassword123!"),
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        dbContext.Users.Add(user);
        
        // Create and add a tenant configuration
        var config = new TenantConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Key = "features:api:enabled",
            Value = "true",
            IsEncrypted = false,
            IsRequired = true,
            IsOverridable = true,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        
        dbContext.TenantConfigurations.Add(config);
        
        // Create and add a tenant connection string
        var connectionString = new TenantConnectionString
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            DatabaseType = "SqlServer",
            ConnectionString = "Server=sql-server-01;Database=acme_corp_db;User Id=tenant_user;Password=SecurePass123!;Connection Timeout=30;Max Pool Size=100",
            Name = "Primary Database",
            SchemaName = "acme_corp_schema",
            DatabaseName = "acme_corp_db",
            ServerHost = "sql-server-01.database.windows.net",
            ServerPort = 1433,
            ConnectionTimeout = 30,
            CommandTimeout = 300,
            MaxPoolSize = 100,
            UseConnectionPooling = true,
            IsPrimary = true,
            IsActive = true
        };
        
        dbContext.TenantConnectionStrings.Add(connectionString);
        
        // Create and add a data isolation policy
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            EntityType = "User",
            PolicyType = "RowLevelSecurity",
            FilterRule = "TenantId = @tenantId",
            IsEnabled = true,
            Priority = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        dbContext.DataIsolationPolicies.Add(policy);
        
        // Create and add a tenant feature
        var feature = new TenantFeature
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FeatureKey = "advanced-reporting",
            DisplayName = "Advanced Reporting",
            Description = "Enables detailed analytics reports.",
            IsEnabled = true,
            Category = "Analytics",
            RolloutPercentage = 75,
            AvailabilityLevel = "Beta",
            AvailableFrom = DateTime.UtcNow.AddDays(-1),
            UsageLimit = 10000,
            CurrentUsage = 0,
            Metadata = "{\"requiresLicense\":true}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        dbContext.TenantFeatures.Add(feature);
        
        // Save all changes to the database
        var changesSaved = await dbContext.SaveChangesAsync();
        Console.WriteLine($"Saved {changesSaved} changes to the database");
        
        // Query data using the DbContext
        var activeTenants = await dbContext.Tenants
            .Where(t => t.Status == TenantStatus.Active)
            .ToListAsync();
        
        var tenantOrgs = await dbContext.Organizations
            .Where(o => o.TenantId == tenant.Id)
            .ToListAsync();
        
        var tenantUsers = await dbContext.Users
            .Where(u => u.TenantId == tenant.Id)
            .ToListAsync();
        
        Console.WriteLine($"Tenant has {tenantOrgs.Count} organizations and {tenantUsers.Count} users");
        
        // Soft delete an entity (will be filtered out by global query filter)
        dbContext.Tenants.Remove(tenant);
        await dbContext.SaveChangesAsync();
        
        // Verify soft delete by querying again
        var activeTenantsAfterDelete = await dbContext.Tenants
            .Where(t => t.Status == TenantStatus.Active)
            .ToListAsync();
        Console.WriteLine($"After soft delete, {activeTenantsAfterDelete.Count} active tenants");
    }
}
```

## OrganizationRepository

The `OrganizationRepository` class provides data access operations for organization management within multi-tenant applications. It extends the base `Repository<Organization>` class and offers specialized methods for querying, filtering, and managing organizations based on various criteria such as slug, industry, country, and activity status.

## UsageMeteringOptions

The `UsageMeteringOptions` class configures tenant usage metering and quota enforcement behavior in multi-tenant applications. It allows customization of warning thresholds, default billing periods, enforcement behavior, and metric tracking limits when registering usage metering services.

**Key capabilities:**
- Set warning threshold percentage for quota consumption
- Configure default billing period for new usage records
- Control whether quota violations throw exceptions
- Limit the number of distinct metrics tracked per tenant

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;
using TenantIsolation.Services;

public class UsageMeteringExample
{
    public static void Main(string[] args)
    {
        // Setup dependency injection with custom usage metering options
        var services = new ServiceCollection();
        
        services.AddTenantUsageMetering(options =>
        {
            // Set warning threshold to 90% of quota
            options.WarningThresholdPercent = 90;
            
            // Use weekly billing period instead of monthly
            options.DefaultPeriod = UsagePeriod.Weekly;
            
            // Throw exceptions when quota is exceeded
            options.ThrowOnQuotaExceeded = true;
            
            // Allow tracking up to 1000 distinct metrics per tenant
            options.MaxMetricsPerTenant = 1000;
        });
        
        // Build service provider
        var provider = services.BuildServiceProvider();
        
        // Resolve the usage metering service
        var meteringService = provider.GetRequiredService<ITenantUsageMeteringService>();
        var options = provider.GetRequiredService<UsageMeteringOptions>();
        
        Console.WriteLine($"Usage metering configured:");
        Console.WriteLine($"  Warning threshold: {options.WarningThresholdPercent}%");
        Console.WriteLine($"  Default period: {options.DefaultPeriod}");
        Console.WriteLine($"  Throw on quota exceeded: {options.ThrowOnQuotaExceeded}");
        Console.WriteLine($"  Max metrics per tenant: {options.MaxMetricsPerTenant}");
        
        // Use the metering service with a tenant
        var tenantId = Guid.NewGuid();
        const string metricKey = "api_calls";
        
        // Set quota limit
        await meteringService.SetQuotaAsync(tenantId, metricKey, 1000);
        
        // Record usage
        await meteringService.RecordUsageAsync(tenantId, metricKey, 50);
        
        // Check quota status
        var quotaCheck = await meteringService.CheckQuotaAsync(tenantId, metricKey);
        Console.WriteLine($"Quota status: {(quotaCheck.IsAllowed ? "Allowed" : "Denied")} - {quotaCheck.CurrentUsage}/{quotaCheck.QuotaLimit}");
    }
}
```

## ValidationResult

The `ValidationResult` class represents the outcome of configuration validation operations in multi-tenant applications. It tracks validation status, collects error messages, and provides helper methods to add validation feedback. This type is primarily used by the `ConfigurationValidator` to report configuration issues during application startup.

**Key capabilities:**
- Track overall validation status with `IsValid` property
- Collect error messages in `Errors` list for invalid configurations
- Collect warning messages in `Warnings` list for non-critical issues
- Add errors and warnings dynamically using helper methods

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Configuration;

public class ConfigurationValidationExample
{
    public static void Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddConfigurationValidator();
        
        var provider = services.BuildServiceProvider();
        
        // Get the configuration validator
        var validator = provider.GetRequiredService<IConfigurationValidator>();
        
        // Validate configuration
        var result = validator.Validate();
        
        Console.WriteLine($"Configuration valid: {result.IsValid}");
        Console.WriteLine($"Error count: {result.Errors.Count}");
        Console.WriteLine($"Warning count: {result.Warnings.Count}");
        
        // Add custom validation errors
        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Error: {error}");
            }
        }
        
        // Add custom validation warnings
        result.AddWarning("Consider enabling audit logging for production environment");
        result.AddWarning("Review feature flags for optimal performance");
        
        Console.WriteLine($"Updated warning count: {result.Warnings.Count}");
        
        // Validate specific configuration section
        var sectionResult = validator.ValidateSection("TenantIsolation");
        Console.WriteLine($"Section validation valid: {sectionResult.IsValid}");
        
        // Throw exception if validation fails
        try
        {
            validator.ValidateAndThrow();
            Console.WriteLine("Configuration validation passed!");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Configuration validation failed: {ex.Message}");
        }
    }
}
```

## ICacheProvider

The `ICacheProvider` interface provides a tenant-aware caching abstraction for multi-tenant applications. It supports both in-memory and distributed caching scenarios with Time-To-Live (TTL) expiration, automatic cleanup, and consistent key generation through the `CacheKeyBuilder`.

**Key capabilities:**
- Thread-safe asynchronous operations with zero allocations
- Sliding expiration and automatic cleanup of expired entries
- Tenant isolation through consistent key generation
- Support for both in-memory and distributed cache implementations
- Efficient memory management with automatic cleanup

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Caching;

public class CacheExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        
        // Register cache provider (automatically detects distributed vs in-memory)
        services.AddTenantAwareCacheProvider();
        
        var provider = services.BuildServiceProvider();
        
        // Resolve the cache provider
        var cacheProvider = provider.GetRequiredService<ICacheProvider>();
        
        var tenantId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        
        // Create a cache key using the builder
        var cacheKey = new CacheKeyBuilder("user-data")
            .WithTenant(tenantId)
            .WithUser(userId)
            .Add("profile")
            .Build();
        
        // Store data in cache with 5 minute expiration
        var userProfile = new { 
            Name = "John Doe", 
            Email = "john.doe@example.com",
            Role = "Administrator"
        };
        
        await cacheProvider.SetAsync(cacheKey, userProfile, TimeSpan.FromMinutes(5));
        
        // Retrieve data from cache
        var cachedProfile = await cacheProvider.GetAsync<object>(cacheKey);
        Console.WriteLine($"Cached profile: {cachedProfile?.GetType().Name}");
        
        // Check if key exists
        var exists = await cacheProvider.ExistsAsync(cacheKey);
        Console.WriteLine("Key exists: {exists}");
        
        // Remove specific key
        await cacheProvider.RemoveAsync(cacheKey);
        
        // Clear all cache entries
        await cacheProvider.ClearAsync();
        
        // Get all cache keys (useful for debugging)
        var allKeys = await cacheProvider.GetAllKeysAsync();
        Console.WriteLine($"Total cache keys: {allKeys.Count()}");
    }
}
```

## ITenantAwareDistributedCacheProvider

The `ITenantAwareDistributedCacheProvider` interface provides a tenant-aware caching abstraction that automatically prefixes cache keys with the current tenant's identifier. This ensures data isolation between tenants when using distributed caching solutions like Redis, SQL Server, or other distributed cache implementations.

**Key capabilities:**
- Automatic tenant-based key prefixing for cache isolation
- Thread-safe asynchronous operations
- Support for both in-memory and distributed cache implementations
- Time-based expiration with tenant context
- Graceful fallback when tenant context is unavailable

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Caching;
using TenantIsolation.Configuration;

public class CacheServiceExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        
        // Register distributed cache (e.g., Redis)
        services.AddDistributedMemoryCache();
        
        // Register tenant-aware cache provider
        services.AddTenantAwareCacheProvider();
        
        // Register tenant isolation services
        services.AddTenantIsolationInMemory();
        
        var provider = services.BuildServiceProvider();
        
        // Resolve the cache provider
        var cacheProvider = provider.GetRequiredService<ICacheProvider>();
        
        // Alternatively, resolve the tenant-aware distributed cache provider directly
        var tenantAwareCache = provider.GetRequiredService<ITenantAwareDistributedCacheProvider>();
        
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        // Create a cache key using the builder
        var cacheKey = new CacheKeyBuilder("user-profile")
            .WithTenant(tenantId.ToString())
            .WithUser(userId.ToString())
            .Add("preferences")
            .Build();
        
        // Store user preferences in cache with 10 minute expiration
        var userPreferences = new 
        {
            Theme = "dark",
            Language = "en-US",
            Timezone = "UTC-5",
            NotificationsEnabled = true
        };
        
        await cacheProvider.SetAsync(cacheKey, userPreferences, TimeSpan.FromMinutes(10));
        
        // Retrieve user preferences from cache
        var cachedPreferences = await cacheProvider.GetAsync<object>(cacheKey);
        Console.WriteLine($"Retrieved preferences: {cachedPreferences != null}");
        
        // Check if key exists
        var exists = await cacheProvider.ExistsAsync(cacheKey);
        Console.WriteLine($"Key exists: {exists}");
        
        // Remove specific key
        await cacheProvider.RemoveAsync(cacheKey);
        
        // Get all cache keys (returns empty for distributed cache)
        var allKeys = await cacheProvider.GetAllKeysAsync();
        Console.WriteLine($"Total cache keys: {allKeys.Count()}");
    }
}
```

## ICachingService

The `ICachingService` interface provides a high-level caching abstraction for application-specific caching operations. It implements the cache-aside pattern with automatic expiration and provides convenient methods for caching frequently accessed data. The service tracks cache statistics including hits and misses, enabling performance monitoring and optimization.

**Key capabilities:**
- Get or fetch values with automatic caching
- Support for both synchronous and asynchronous operations
- Cache statistics tracking (hits, misses, hit rate)
- Tenant-aware caching through `TenantAwareCachingService`
- Automatic expiration with configurable TTL

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Caching;
using TenantIsolation.Services;

public class CachingExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddScoped<ICachingService, CachingService>();
        services.AddScoped<ITenantService, TenantService>();
        
        var provider = services.BuildServiceProvider();
        
        // Resolve the caching service
        var cachingService = provider.GetRequiredService<ICachingService>();
        
        // Cache a value with automatic expiration
        var cacheKey = "user:12345:profile";
        
        // Get or fetch user profile (will cache the result)
        var userProfile = await cachingService.GetOrFetchAsync(
            cacheKey,
            async () => 
            {
                // This fetch function will only be called if the value is not in cache
                var tenantService = provider.GetRequiredService<ITenantService>();
                var user = await tenantService.GetUserProfileAsync(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
                return user;
            },
            TimeSpan.FromMinutes(10) // Cache for 10 minutes
        );
        
        Console.WriteLine($"User profile retrieved: {userProfile?.Name}");
        
        // Retrieve from cache (will be much faster)
        var cachedProfile = await cachingService.GetAsync<object>(cacheKey);
        Console.WriteLine($"Retrieved from cache: {cachedProfile != null}");
        
        // Get cache statistics
        var stats = await cachingService.GetStatisticsAsync();
        Console.WriteLine($"Cache stats - Hits: {stats.CacheHits}, Misses: {stats.CacheMisses}, Hit Rate: {stats.HitRate:P0}");
        
        // Update cache
        await cachingService.SetAsync(cacheKey, userProfile, TimeSpan.FromMinutes(15));
        
        // Remove specific key
        await cachingService.RemoveAsync(cacheKey);
        
        // Clear all cache entries
        await cachingService.ClearAsync();
    }
}
```

## StringExtensions

The `StringExtensions` class provides a comprehensive set of string utility extension methods for common operations like slug generation, truncation, validation, and formatting. These methods are designed for use in multi-tenant applications where consistent string handling is essential for URLs, identifiers, and display purposes.

Here's an example usage demonstrating the key public members:

```csharp
using System;
using TenantIsolation.Utilities;

public class StringExtensionsExample
{
    public static void Main(string[] args)
    {
        // Convert to URL-safe slug
        string companyName = "ACME Corporation Ltd.";
        string slug = companyName.ToSlug();
        Console.WriteLine($"Slug: {slug}"); // Output: "slug-acme-corporation-ltd"

        // Truncate long strings
        string longText = "This is a very long text that needs to be shortened for display purposes";
        string truncated = longText.Truncate(20);
        Console.WriteLine($"Truncated: {truncated}"); // Output: "This is a very long..."

        // Validate email format
        string email = "user@example.com";
        bool isValidEmail = email.IsValidEmail();
        Console.WriteLine($"Is valid email: {isValidEmail}"); // Output: True

        // Validate URL format
        string url = "https://example.com/path?query=value";
        bool isValidUrl = url.IsValidUrl();
        Console.WriteLine($"Is valid URL: {isValidUrl}"); // Output: True

        // Safe substring extraction
        string text = "Hello World";
        string safeSubstring = text.SafeSubstring(6, 5);
        Console.WriteLine($"Safe substring: {safeSubstring}"); // Output: "World"

        // Remove special characters
        string specialText = "Hello, World! 123#";
        string cleaned = specialText.RemoveSpecialCharacters();
        Console.WriteLine($"Cleaned text: {cleaned}"); // Output: "HelloWorld123"

        // Mask sensitive data
        string sensitiveEmail = "admin@company.com";
        string masked = sensitiveEmail.MaskSensitiveData();
        Console.WriteLine($"Masked email: {masked}"); // Output: "adm*******"

        // Convert to PascalCase
        string camelCase = "helloWorld";
        string pascalCase = camelCase.ToPascalCase();
        Console.WriteLine($"PascalCase: {pascalCase}"); // Output: "HelloWorld"

        // Convert to human-readable format
        string pascalCaseText = "UserProfileSettings";
        string humanReadable = pascalCaseText.ToHumanReadable();
        Console.WriteLine($"Human readable: {humanReadable}"); // Output: "User Profile Settings"

        // Get deterministic hash code
        string data = "tenant-12345";
        int hashCode = data.GetDeterministicHashCode();
        Console.WriteLine($"Hash code: {hashCode}");
    }
}
```

## UserRepository

The `UserRepository` class provides data access operations for user management within multi-tenant applications. It extends the base `Repository<User>` class and offers specialized methods for querying, filtering, and managing users based on various criteria such as email, role, organization, and authentication status.

**Key capabilities:**
- Retrieve users by email, role, or organization
- Search and filter users with complex queries
- Check email uniqueness and user activity status
- Manage user lifecycle (activation, deactivation, password changes)
- Get user statistics and activity reports
- Handle authentication-related operations (locked accounts, verification status)

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Models;
using TenantIsolation.Data;

public class UserManagementExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddDbContext<TenantDbContext>();
        services.AddScoped<ITenantDbContextFactory<TenantDbContext>, TenantDbContextFactory<TenantDbContext>>();
        services.AddScoped<UserRepository>();

        var provider = services.BuildServiceProvider();

        // Get repository instance
        var userRepository = provider.GetRequiredService<UserRepository>();
        var tenantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var organizationId = Guid.Parse("4fa85f64-5717-4562-b3fc-2c963f66afa6");

        // Create a sample user for demonstration
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = organizationId,
            Email = "john.doe@acme-corp.com",
            FirstName = "John",
            LastName = "Doe",
            Role = "Administrator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecurePassword123!"),
            IsActive = true,
            IsEmailVerified = true,
            IsTwoFactorEnabled = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add user to database (in real usage, this would be through UserService)
        // For this example, we'll query existing users

        // Get user by email
        var userByEmail = await userRepository.GetByEmailAsync("john.doe@acme-corp.com", tenantId);
        Console.WriteLine($"Found user by email: {userByEmail?.FirstName} {userByEmail?.LastName}");

        // Get active users in organization
        var activeUsers = await userRepository.GetActiveUsersInOrganizationAsync(organizationId);
        Console.WriteLine($"Active users in organization: {activeUsers.Count}");

        // Get users by role
        var adminUsers = await userRepository.GetByRoleAsync(tenantId, "Administrator");
        Console.WriteLine($"Administrator users: {adminUsers.Count}");

        // Get unverified users
        var unverifiedUsers = await userRepository.GetUnverifiedUsersAsync(tenantId);
        Console.WriteLine($"Unverified users: {unverifiedUsers.Count}");

        // Get never logged in users
        var neverLoggedIn = await userRepository.GetNeverLoggedInAsync(tenantId);
        Console.WriteLine($"Never logged in users: {neverLoggedIn.Count}");

        // Get locked accounts
        var lockedAccounts = await userRepository.GetLockedAccountsAsync(tenantId);
        Console.WriteLine($"Locked accounts: {lockedAccounts.Count}");

        // Get user count
        var userCount = await userRepository.GetUserCountAsync(tenantId);
        Console.WriteLine($"Total users: {userCount}");

        // Get recently active users
        var recentlyActive = await userRepository.GetRecentlyActiveAsync(tenantId, 7);
        Console.WriteLine($"Recently active users (last 7 days): {recentlyActive.Count}");

        // Search users
        var searchResults = await userRepository.SearchAsync(tenantId, "john");
        Console.WriteLine($"Search results for 'john': {searchResults.Count}");

        // Check if email is unique
        var isEmailUnique = await userRepository.IsEmailUniqueAsync("jane.smith@acme-corp.com", tenantId);
        Console.WriteLine($"Is email unique: {isEmailUnique}");

        // Get users requiring password change
        var usersNeedingPasswordChange = await userRepository.GetUsersRequiringPasswordChangeAsync(tenantId, 90);
        Console.WriteLine($"Users needing password change: {usersNeedingPasswordChange.Count}");

        // Get user statistics
        var statistics = await userRepository.GetUserStatisticsAsync(tenantId);
        Console.WriteLine("User statistics retrieved");

        // Deactivate organization users
        var deactivatedCount = await userRepository.DeactivateOrganizationUsersAsync(organizationId);
        Console.WriteLine($"Deactivated {deactivatedCount} users in organization");
    }
}
```


**Key capabilities:**
- Retrieve organizations by slug, industry, country, or registration number
- Search organizations by name, email, or slug
- Get organizations with user counts and statistics
- Manage organization lifecycle (activation, deactivation)
- Check uniqueness constraints (e.g., slug validation)
- Get recent organizations and bulk operations

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Models;
using TenantIsolation.Data;

public class OrganizationManagementExample
{
 public static async Task Main(string[] args)
 {
 // Setup dependency injection
 var services = new ServiceCollection();
 services.AddLogging(configure => configure.AddConsole());
 services.AddDbContext<TenantDbContext>();
 services.AddScoped<ITenantDbContextFactory<TenantDbContext>, TenantDbContextFactory<TenantDbContext>>();
 services.AddScoped<OrganizationRepository>();

 var provider = services.BuildServiceProvider();

 // Get repository instance
 var organizationRepository = provider.GetRequiredService<OrganizationRepository>();
 var tenantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

 // Create a sample organization for demonstration
 var organization = new Organization
 {
 Id = Guid.NewGuid(),
 TenantId = tenantId,
 Slug = "acme-corp",
 Name = "ACME Corporation",
 Industry = "Manufacturing",
 CountryCode = "US",
 RegistrationNumber = "REG-12345",
 ContactEmail = "contact@acme-corp.com",
 IsActive = true,
 CreatedAt = DateTime.UtcNow,
 UpdatedAt = DateTime.UtcNow
 };

 // Add organization to database (in real usage, this would be through OrganizationService)
 // For this example, we'll query existing organizations

 // Get organization by slug
 var orgBySlug = await organizationRepository.GetBySlugAsync(tenantId, "acme-corp");
 Console.WriteLine($"Found organization by slug: {orgBySlug?.Name}");

 // Get all active organizations
 var activeOrgs = await organizationRepository.GetActiveOrganizationsAsync(tenantId);
 Console.WriteLine($"Active organizations count: {activeOrgs.Count}");

 // Get organizations by industry
 var manufacturingOrgs = await organizationRepository.GetByIndustryAsync(tenantId, "Manufacturing");
 Console.WriteLine($"Manufacturing organizations count: {manufacturingOrgs.Count}");

 // Get organizations by country
 var usOrgs = await organizationRepository.GetByCountryAsync(tenantId, "US");
 Console.WriteLine($"US organizations count: {usOrgs.Count}");

 // Search organizations
 var searchResults = await organizationRepository.SearchAsync(tenantId, "acme");
 Console.WriteLine($"Search results count: {searchResults.Count}");

 // Get organization count
 var orgCount = await organizationRepository.GetOrganizationCountAsync(tenantId);
 Console.WriteLine($"Total organizations: {orgCount}");

 // Check if slug is unique
 var isSlugUnique = await organizationRepository.IsSlugUniqueAsync(tenantId, "new-org");
 Console.WriteLine($"Is slug unique: {isSlugUnique}");

 // Get organizations with user count
 var orgsWithUsers = await organizationRepository.GetOrganizationsWithUserCountAsync(tenantId);
 foreach (var item in orgsWithUsers)
 {
 var org = (dynamic)item;
 Console.WriteLine($"Organization: {org.Organization.Name}, Users: {org.UserCount}");
 }

 // Get organization by registration number
 var orgByRegNumber = await organizationRepository.GetByRegistrationNumberAsync(tenantId, "REG-12345");
 Console.WriteLine($"Found organization by registration number: {orgByRegNumber?.Name}");

 // Get organization statistics
 var statistics = await organizationRepository.GetStatisticsAsync(tenantId);
 Console.WriteLine($"Organization statistics retrieved");

 // Get recent organizations
 var recentOrgs = await organizationRepository.GetRecentAsync(tenantId, 5);
 Console.WriteLine($"Recent organizations count: {recentOrgs.Count}");

 // Deactivate organization
 var deactivateResult = await organizationRepository.DeactivateAsync(organization.Id);
 Console.WriteLine($"Deactivation successful: {deactivateResult}");

 // Bulk activate organizations
 var bulkActivateCount = await organizationRepository.BulkActivateAsync(tenantId, new List<Guid> { organization.Id });
 Console.WriteLine($"Bulk activate count: {bulkActivateCount}");
 }
}
```

This example demonstrates creating an `OrganizationRepository` instance through dependency injection and using its public methods to query, filter, and manage organizations in a multi-tenant application.



## TenantRepository

The `TenantRepository` class provides data access operations for tenant management in multi-tenant applications. It extends the base `Repository<Tenant>` class and offers specialized methods for querying, filtering, and managing tenants based on various criteria such as status, subscription dates, activity, and more.

**Key capabilities:**
- Retrieve tenants by slug, status, or custom criteria
- Search and filter tenants with complex queries
- Manage tenant lifecycle (activation, suspension)
- Get billing and usage summaries
- Check uniqueness constraints (e.g., slug validation)
- Track tenant activity and inactivity

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Models;
using TenantIsolation.Data;
using TenantIsolation.Constants;

public class TenantRepositoryExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddDbContext<TenantDbContext>();
        services.AddScoped<ITenantDbContextFactory<TenantDbContext>, TenantDbContextFactory<TenantDbContext>>();
        services.AddScoped<TenantRepository>();

        var provider = services.BuildServiceProvider();

        // Get repository instance
        var tenantRepository = provider.GetRequiredService<TenantRepository>();

        // Create a tenant for demonstration
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = "acme-corp",
            Name = "ACME Corporation",
            AdminEmail = "admin@acme-corp.com",
            Status = TenantStatus.Active,
            PlanId = "enterprise-pro",
            MaxUsers = 500,
            MaxStorageGb = 1024.50m,
            SubscriptionExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add tenant to database (in real usage, this would be through TenantService)
        // For this example, we'll query existing tenants

        // Get tenant by slug
        var tenantBySlug = await tenantRepository.GetBySlugAsync("acme-corp");
        Console.WriteLine($"Found tenant by slug: {tenantBySlug?.Name}");

        // Get all active tenants
        var activeTenants = await tenantRepository.GetActiveTenantAsync();
        Console.WriteLine($"Active tenants count: {activeTenants.Count}");

        // Get tenants by status
        var trialTenants = await tenantRepository.GetByStatusAsync(TenantStatus.Trial);
        Console.WriteLine($"Trial tenants count: {trialTenants.Count}");

        // Search tenants
        var searchResults = await tenantRepository.SearchAsync("acme");
        Console.WriteLine($"Search results count: {searchResults.Count}");

        // Get tenants with expiring subscriptions
        var expiringSoon = await tenantRepository.GetExpiringSubscriptionsAsync(daysUntilExpiry: 30);
        Console.WriteLine($"Tenants with expiring subscriptions: {expiringSoon.Count}");

        // Get recently created tenants
        var recentTenants = await tenantRepository.GetRecentlyCreatedAsync(days: 7);
        Console.WriteLine($"Recently created tenants: {recentTenants.Count}");

        // Get tenant with details
        var tenantWithDetails = await tenantRepository.GetWithDetailsAsync(tenant.Id);
        Console.WriteLine($"Tenant with details: {tenantWithDetails?.Name}");

        // Get status counts
        var statusCounts = await tenantRepository.GetStatusCountsAsync();
        foreach (var statusCount in statusCounts)
        {
            Console.WriteLine($"Status {statusCount.Key}: {statusCount.Value} tenants");
        }

        // Check if slug is unique
        var isSlugUnique = await tenantRepository.IsSlugUniqueAsync("new-tenant-slug");
        Console.WriteLine($"Is slug unique: {isSlugUnique}");

        // Get inactive tenants
        var inactiveTenants = await tenantRepository.GetInactiveTenantsAsync(inactiveDays: 90);
        Console.WriteLine($"Inactive tenants: {inactiveTenants.Count}");

        // Activate tenant
        var activationResult = await tenantRepository.ActivateTenantAsync(tenant.Id);
        Console.WriteLine($"Activation successful: {activationResult}");

        // Suspend tenant
        var suspendResult = await tenantRepository.SuspendTenantAsync(tenant.Id, "Account suspended for non-payment");
        Console.WriteLine($"Suspension successful: {suspendResult}");

        // Get billing summary
        var billingSummary = await tenantRepository.GetBillingSummaryAsync();
        Console.WriteLine($"Billing summary retrieved: {billingSummary}");
    }
}
```

This example demonstrates creating a `TenantRepository` instance through dependency injection and using its public methods to query, filter, and manage tenants in a multi-tenant application.

## TracingContext

The `TracingContext` class provides distributed tracing capabilities for tracking requests across multiple services in multi-tenant applications. It captures correlation IDs, trace IDs, span IDs, tenant context, user context, and metadata to enable comprehensive request tracing and debugging.

**Key capabilities:**
- Track requests with correlation IDs and trace IDs
- Support nested operations with parent/child span relationships
- Include tenant and user context for multi-tenant applications
- Store operation metadata for debugging and monitoring
- Provide structured logging integration

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Utilities;

public class TracingExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddDistributedTracing();

        var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<TracingExample>();

        // Create a new tracing context for a request
        var tracingContext = new TracingContext
        {
            RequestPath = "/api/users/get",
            TenantId = Guid.Parse("3fa85f6-4717-4562-b3fc-2c963f66afa6"),
            UserId = "user-12345"
        };

        // Add custom metadata
        tracingContext.Metadata["operation"] = "GetUserDetails";
        tracingContext.Metadata["user_type"] = "premium";

        // Set as current context
        DistributedTracingExtensions.SetCurrentContext(tracingContext);

        // Log with tracing context
        logger.LogWithTracing(LogLevel.Information, "Starting user details operation");

        // Create child context for nested operation
        var childContext = DistributedTracingExtensions.CreateChildContext("DatabaseQuery");
        childContext.Metadata["query"] = "SELECT * FROM Users WHERE Id = @id";

        using (DistributedTracingExtensions.BeginTracingScope(childContext))
        {
            // Simulate database operation
            logger.LogWithTracing(LogLevel.Debug, "Executing database query");
            
            // Add more metadata during operation
            DistributedTracingExtensions.AddMetadata("query_duration_ms", "42");
        }

        // Get current context
        var currentContext = DistributedTracingExtensions.GetCurrentContext();
        Console.WriteLine($"Correlation ID: {currentContext?.CorrelationId}");
        Console.WriteLine($"Trace ID: {currentContext?.TraceId}");
        Console.WriteLine($"Span ID: {currentContext?.SpanId}");
        Console.WriteLine($"Tenant ID: {currentContext?.TenantId}");
        Console.WriteLine($"User ID: {currentContext?.UserId}");

        // Use structured logging state
        var logState = DistributedTracingExtensions.GetTracingLogState();
        foreach (var kvp in logState)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }

        // Execute operation with automatic tracing
        var result = await DistributedTracingExtensions.ExecuteWithTracingAsync(
            "ProcessUserRequest",
            async () =>
            {
                await Task.Delay(100); // Simulate work
                return "User processed successfully";
            },
            logger
        );

        Console.WriteLine($"Result: {result}");
    }
}
```

## ITimeProvider

The `ITimeProvider` interface provides a dependency injection-friendly abstraction for time operations, enabling testable and mockable time-dependent code in multi-tenant applications. It supports both real system time and mock time for testing scenarios.

**Key capabilities:**
- Get current UTC, local, or date-only time
- Convert between UTC and tenant-specific timezones
- Check business hours and calculate deadlines
- Mock time for deterministic testing

**Usage example**

```csharp
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Utilities;

public class TimeProviderExample
{
    public static void Main(string[] args)
    {
        // Setup dependency injection with real time provider
        var services = new ServiceCollection();
        services.AddTimeProvider(useMock: false);
        
        var provider = services.BuildServiceProvider();
        var timeProvider = provider.GetRequiredService<ITimeProvider>();
        
        // Get current time
        Console.WriteLine($"Current UTC time: {timeProvider.UtcNow}");
        Console.WriteLine($"Current local time: {timeProvider.Now}");
        Console.WriteLine($"Current date: {timeProvider.Today}");
        Console.WriteLine($"Timezone: {timeProvider.TimeZone.DisplayName}");
        
        // Convert between UTC and tenant time
        var tenantTimeZoneId = "Eastern Standard Time";
        var utcTime = DateTime.UtcNow;
        var tenantTime = timeProvider.ConvertToTenantTime(utcTime, tenantTimeZoneId);
        Console.WriteLine($"Tenant time: {tenantTime}");
        
        var backToUtc = timeProvider.ConvertFromTenantTime(tenantTime, tenantTimeZoneId);
        Console.WriteLine($"Back to UTC: {backToUtc}");
        
        // Check business hours
        bool isBusinessHours = timeProvider.IsBusinessHours(DateTime.UtcNow);
        Console.WriteLine($"Is business hours: {isBusinessHours}");
        
        // Calculate deadlines
        var deadline = timeProvider.CalculateSlaDeadline(businessHoursNeeded: 2);
        Console.WriteLine($"SLA deadline in 2 business hours: {deadline}");
        
        var timeUntilDeadline = timeProvider.GetTimeUntilDeadline(deadline);
        Console.WriteLine($"Time until deadline: {timeUntilDeadline.TotalMinutes} minutes");
        
        bool deadlineExceeded = timeProvider.IsDeadlineExceeded(deadline);
        Console.WriteLine($"Deadline exceeded: {deadlineExceeded}");
    }
}
```

### Using Mock Time Provider for Testing

```csharp
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Utilities;

public class TimeProviderTestingExample
{
    public static void Main(string[] args)
    {
        // Setup dependency injection with mock time provider
        var services = new ServiceCollection();
        services.AddTimeProvider(useMock: true);
        
        var provider = services.BuildServiceProvider();
        var mockTimeProvider = provider.GetRequiredService<ITimeProvider>() as MockTimeProvider;
        
        // Set mock time
        mockTimeProvider.SetCurrentTime(new DateTime(2024, 1, 15, 10, 30, 0));
        Console.WriteLine($"Mock time set to: {mockTimeProvider.UtcNow}");
        
        // Advance time
        mockTimeProvider.AdvanceTime(TimeSpan.FromHours(2));
        Console.WriteLine($"Time advanced by 2 hours: {mockTimeProvider.UtcNow}");
        
        // Reset to system time
        mockTimeProvider.Reset();
        Console.WriteLine($"Reset to system time: {mockTimeProvider.UtcNow}");
    }
}
```

## ValidationUtility

The `ValidationUtility` class provides centralized validation methods for common data validation patterns across the tenant isolation framework. It includes validation for email addresses, slugs, GUIDs, URLs, and various string and numeric constraints, with both boolean check methods and exception-throwing validation methods for different use cases.

**Key capabilities:**
- Validate email format (RFC-compliant regex pattern)
- Validate tenant slug format (lowercase alphanumeric with hyphens, 3-63 characters)
- Validate GUID format
- Validate URL format (HTTP/HTTPS only)
- Validate string length constraints (min, max, range)
- Validate numeric ranges and positivity
- Validate date relationships (future, past, valid ranges)
- Validate enum values
- Throw `TenantIsolationException` for invalid values or provide boolean results

**Usage example**

```csharp
using TenantIsolation.Utilities;
using TenantIsolation.Exceptions;

public class ValidationExample
{
    public static void Main(string[] args)
    {
        // Validate email format
        string email = "user@example.com";
        bool isEmailValid = ValidationUtility.IsValidEmail(email);
        Console.WriteLine($"Email '{email}' is valid: {isEmailValid}");

        // Validate tenant slug
        string slug = "acme-corp";
        bool isSlugValid = ValidationUtility.IsValidSlug(slug);
        Console.WriteLine($"Slug '{slug}' is valid: {isSlugValid}");

        // Validate GUID
        string guid = Guid.NewGuid().ToString();
        bool isGuidValid = ValidationUtility.IsValidGuid(guid);
        Console.WriteLine($"GUID '{guid}' is valid: {isGuidValid}");

        // Validate URL
        string url = "https://example.com/api/users";
        bool isUrlValid = ValidationUtility.IsValidUrl(url);
        Console.WriteLine($"URL '{url}' is valid: {isUrlValid}");

        // String length validation
        string name = "John Doe";
        ValidationUtility.RequireMinLength(name, 3, nameof(name));
        ValidationUtility.RequireMaxLength(name, 50, nameof(name));
        ValidationUtility.RequireLengthBetween(name, 3, 50, nameof(name));

        // Numeric validation
        int userCount = 42;
        ValidationUtility.RequirePositive(userCount, nameof(userCount));
        ValidationUtility.RequireRange(userCount, 1, 1000, nameof(userCount));

        // Date validation
        DateTime futureDate = DateTime.UtcNow.AddDays(30);
        ValidationUtility.RequireFutureDate(futureDate, nameof(futureDate));

        DateTime pastDate = DateTime.UtcNow.AddDays(-1);
        ValidationUtility.RequirePastDate(pastDate, nameof(pastDate));

        // Date range validation
        DateTime startDate = DateTime.UtcNow;
        DateTime endDate = DateTime.UtcNow.AddDays(7);
        ValidationUtility.RequireValidDateRange(startDate, endDate, nameof(startDate), nameof(endDate));

        // Enum validation
        ValidationUtility.RequireValidEnum(UserRole.Administrator);

        // Exception-throwing validation methods
        try
        {
            ValidationUtility.RequireNotEmpty(null, nameof(email));
        }
        catch (TenantIsolationException ex)
        {
            Console.WriteLine($"Validation failed: {ex.Message}");
        }

        try
        {
            ValidationUtility.RequireValidEmail("invalid-email");
        }
        catch (TenantIsolationException ex)
        {
            Console.WriteLine($"Email validation failed: {ex.Message}");
        }

        try
        {
            ValidationUtility.RequireValidSlug("Invalid Slug");
        }
        catch (TenantIsolationException ex)
        {
            Console.WriteLine($"Slug validation failed: {ex.Message}");
        }

        try
        {
            ValidationUtility.RequireValidGuid("not-a-guid", nameof(guid));
        }
        catch (TenantIsolationException ex)
        {
            Console.WriteLine($"GUID validation failed: {ex.Message}");
        }

        try
        {
            ValidationUtility.RequireValidUrl("ftp://invalid.com", nameof(url));
        }
        catch (TenantIsolationException ex)
        {
            Console.WriteLine($"URL validation failed: {ex.Message}");
        }
    }
}

public enum UserRole { Administrator, User, Guest }
```

## DynamicTenantStore

The `ComponentHealthInfo` class represents the health status of a specific system component (database, cache, event bus, etc.) in a multi-tenant application. It tracks individual component health metrics including response time, status, and descriptive messages, enabling detailed health monitoring and troubleshooting.

Here's an example usage:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Services;
using TenantIsolation.Data;

public class HealthMonitoringExample
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddDbContext<TenantDbContext>();
        services.AddHealthCheckService();

        var provider = services.BuildServiceProvider();

        // Get the health check service
        var healthCheckService = provider.GetRequiredService<IHealthCheckService>();

        // Perform a component-specific health check
        var databaseHealth = await healthCheckService.CheckComponentAsync("database");
        
        Console.WriteLine($"Component: {databaseHealth.Name}");
        Console.WriteLine($"Status: {databaseHealth.Status}");
        Console.WriteLine($"Message: {databaseHealth.Message}");
        Console.WriteLine($"Response Time: {databaseHealth.ResponseTimeMs}ms");
        Console.WriteLine($"Checked At: {databaseHealth.CheckedAt}");

        // Check overall system health
        var healthReport = await healthCheckService.PerformHealthCheckAsync();
        
        Console.WriteLine($"\nOverall Status: {healthReport.Status}");
        Console.WriteLine($"Total Duration: {healthReport.TotalCheckDuration.TotalMilliseconds}ms");
        Console.WriteLine($"Checked At: {healthReport.CheckedAt}");
        
        foreach (var component in healthReport.Components)
        {
            Console.WriteLine($"\nComponent: {component.Key}");
            Console.WriteLine($"  Status: {component.Value.Status}");
            Console.WriteLine($"  Message: {component.Value.Message}");
            Console.WriteLine($"  Response Time: {component.Value.ResponseTimeMs}ms");
        }
        
        // Get cached report if available
        var cachedReport = healthCheckService.GetCachedHealthReport();
        if (cachedReport != null)
        {
            Console.WriteLine($"\nUsing cached report from: {cachedReport.CheckedAt}");
            Console.WriteLine($"Cached message: {cachedReport.GetMessage()}");
        }
    }
}
```

This example demonstrates creating a `ComponentHealthInfo` instance through the health check service, checking individual components, and retrieving comprehensive health reports using the public members defined on the type.

## ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` catches unhandled exceptions in the request pipeline, logs them, and returns a consistent JSON error response containing fields such as `Code`, `Message`, `StatusCode`, `TraceId`, `Details`, and `Timestamp`. This centralizes error handling and provides clients with structured error information.

**Usage example**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Register logging (already added by default)
builder.Services.AddLogging();

var app = builder.Build();

// Register the error handling middleware early in the pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapGet("/", () => "Hello World!");

app.Run();
```

In this example the middleware is added to the ASP.NET Core pipeline with `app.UseMiddleware<ErrorHandlingMiddleware>()`. When an exception occurs downstream, the middleware logs the error and returns a JSON payload with the standardized properties (`Code`, `Message`, `StatusCode`, `TraceId`, `Details`, `Timestamp`).


## RateLimitingMiddleware

The `RateLimitingMiddleware` implements sliding window rate limiting to prevent abuse and excessive resource consumption in multi-tenant applications. It tracks requests per tenant and IP address, protecting against DoS attacks and ensuring fair resource distribution across tenants. The middleware uses a configurable requests-per-minute limit and provides standard HTTP rate limiting headers in responses.

**Usage example**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Register logging (already added by default)
builder.Services.AddLogging();

var app = builder.Build();

// Configure rate limiting options
var rateLimitOptions = new RateLimitOptions
{
    RequestsPerMinute = 100,  // Allow 100 requests per minute
    RetryAfterSeconds = 30,    // Wait 30 seconds before retry
    Enabled = true             // Enable rate limiting
};

// Register the rate limiting middleware in the pipeline
app.UseRateLimiting(rateLimitOptions);

app.MapGet("/api/data", () => "Data response");

app.Run();
```

In this example, the middleware is configured with custom rate limiting options and added to the ASP.NET Core pipeline using the `UseRateLimiting` extension method. The middleware will track requests per tenant (from `context.Items["TenantId"]`) and IP address, returning HTTP 429 (Too Many Requests) with a `Retry-After` header when the limit is exceeded. Response headers include `X-RateLimit-Limit`, `X-RateLimit-Remaining`, and `X-RateLimit-Reset` for client-side rate limiting coordination.
