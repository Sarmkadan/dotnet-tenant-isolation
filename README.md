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

### FeaturesController
Feature flag management for tenant-specific features.

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
