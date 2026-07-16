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

## Services

### TenantResolutionService
Provides tenant resolution strategies for multi-tenant applications.

### TenantService
Manages tenant lifecycle and configuration.

### ConfigurationService
Handles tenant-specific configuration settings with encryption and validation.

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
