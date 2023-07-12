# Multi-Tenancy Framework for ASP.NET Core

Enterprise-grade multi-tenancy isolation framework for ASP.NET Core with support for multiple data isolation strategies, per-tenant configuration management, and comprehensive feature toggle system.

## Features

### Core Multi-Tenancy Support
- **Automatic Tenant Resolution** - Resolve tenants from HTTP headers, route parameters, claims, or subdomains
- **Multiple Isolation Strategies**
  - Database-per-Tenant: Each tenant has dedicated database
  - Schema-per-Tenant: Single database with isolated schemas
  - Row-Level Security: Single database/schema with tenant identification column
  - Hybrid: Combination of strategies

### Data Isolation & Security
- **Configurable Data Isolation Policies** - Define field-level access controls
- **Cross-Tenant Access Control** - Explicitly allow/deny cross-tenant data access
- **Soft Delete Support** - Mark entities as deleted without removing from database
- **Tenant Status Management** - Active, Suspended, Trial, Inactive, Archived, Provisioning states

### Tenant Management
- **Per-Tenant Configuration** - Store and manage tenant-specific settings
- **Database Connection Management** - Support multiple connection strings per tenant
- **Subscription Management** - Track subscription plans, expiration dates, and user limits
- **Feature Toggle System** - Enable/disable features per tenant with rollout percentage

### Developer Experience
- **Simple DI Integration** - One-line registration in `Program.cs`
- **Middleware-Based Resolution** - Automatic tenant detection on every request
- **Query Filtering** - Global query filters for tenant isolation
- **Caching Layer** - Built-in memory cache for configuration and features
- **Comprehensive Logging** - Detailed logging for debugging and auditing

## Architecture

```
TenantIsolation/
├── Models/              # Domain entities
│   ├── Tenant.cs
│   ├── User.cs
│   ├── Organization.cs
│   ├── TenantConfiguration.cs
│   ├── TenantConnectionString.cs
│   ├── DataIsolationPolicy.cs
│   └── TenantFeature.cs
├── Data/                # Data access layer
│   ├── TenantDbContext.cs
│   ├── Repository.cs    # Generic base repository
│   ├── TenantRepository.cs
│   ├── UserRepository.cs
│   └── OrganizationRepository.cs
├── Services/            # Business logic layer
│   ├── TenantService.cs
│   ├── TenantResolutionService.cs
│   ├── DataIsolationService.cs
│   ├── ConfigurationService.cs
│   └── TenantFeatureService.cs
├── Middleware/          # ASP.NET middleware
│   └── TenantResolutionMiddleware.cs
├── Configuration/       # DI and configuration
│   └── DependencyInjectionExtensions.cs
└── Exceptions/          # Custom exceptions
    └── TenantIsolationException.cs
```

## Quick Start

### Installation

```bash
dotnet add package TenantIsolation
```

### Configuration

Register the framework in `Program.cs`:

```csharp
using TenantIsolation.Configuration;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddTenantIsolationSqlServer(connectionString, options =>
{
    options.AutoMigrate = true;
    options.EnableAuditLogging = true;
});

builder.Services.AddTenantFeatureToggle();

var app = builder.Build();

app.UseTenantResolution();
app.MapControllers();

app.Run();
```

### Create a Tenant

```csharp
[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly TenantService _tenantService;

    public TenantsController(TenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant(CreateTenantRequest request)
    {
        var tenant = await _tenantService.CreateTenantAsync(
            request.Name,
            request.Slug,
            request.AdminEmail);

        return Ok(tenant);
    }
}
```

### Resolve Current Tenant

```csharp
[ApiController]
[Route("api/organizations")]
public class OrganizationsController : ControllerBase
{
    private readonly TenantResolutionService _tenantResolution;
    private readonly OrganizationRepository _orgRepository;

    [HttpGet]
    public async Task<IActionResult> GetOrganizations()
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();
        var organizations = await _orgRepository.GetActiveOrganizationsAsync(tenant.Id);

        return Ok(organizations);
    }
}
```

### Use Data Isolation Policies

```csharp
var policyService = serviceProvider.GetRequiredService<DataIsolationService>();

// Create a strict policy
var policy = await policyService.CreatePolicyAsync(
    tenantId: tenantId,
    entityType: "User",
    policyType: DataIsolationPolicyType.Strict);

// Verify field access
await policyService.VerifyFieldAccessAsync(tenantId, "User", "Email");
```

### Feature Toggle Management

```csharp
var featureService = serviceProvider.GetRequiredService<TenantFeatureService>();

// Check if feature is enabled
if (await featureService.IsFeatureEnabledAsync(tenantId, "advanced-security"))
{
    // Enable advanced security features
}

// Set rollout percentage
await featureService.SetRolloutPercentageAsync(tenantId, "beta-feature", 25);

// Record usage
await featureService.RecordFeatureUsageAsync(tenantId, "api-access");
```

## Tenant Resolution Strategies

### 1. HTTP Header
```http
GET /api/data
X-Tenant-Id: 550e8400-e29b-41d4-a716-446655440000
```

### 2. Route Parameter
```http
GET /api/tenants/550e8400-e29b-41d4-a716-446655440000/data
```

### 3. User Claims
```csharp
claims.Add(new Claim("tenant_id", "550e8400-e29b-41d4-a716-446655440000"));
```

### 4. Subdomain
```http
GET https://acme.example.com/api/data
```

## Data Isolation Policies

### Strict Isolation (Default)
```csharp
// No cross-tenant access allowed
var policy = new DataIsolationPolicy
{
    PolicyType = DataIsolationPolicyType.Strict,
    EntityType = "User"
};
```

### Relaxed Isolation
```csharp
// Allow specific cross-tenant access
var policy = new DataIsolationPolicy
{
    PolicyType = DataIsolationPolicyType.Relaxed,
    EntityType = "User",
    AllowedCrossTenantAccess = "550e8400-e29b-41d4-a716-446655440000" // Comma-separated
};
```

### Custom Isolation
```csharp
// Define custom filter rules
var policy = new DataIsolationPolicy
{
    PolicyType = DataIsolationPolicyType.Custom,
    EntityType = "User",
    FilterRule = "WHERE Status = 'Active'"
};
```

## Configuration Management

```csharp
var configService = serviceProvider.GetRequiredService<ConfigurationService>();

// Set configuration
await configService.SetConfigurationAsync(
    tenantId,
    "features:api:rateLimit",
    "1000",
    valueType: "int");

// Get configuration
var rateLimit = await configService.GetConfigurationAsync<int>(
    tenantId,
    "features:api:rateLimit",
    defaultValue: 100);

// Import/Export
var json = await configService.ExportConfigurationAsync(tenantId);
await configService.ImportConfigurationAsync(tenantId, json);
```

## Database Support

- **SQL Server** 2019+
- **PostgreSQL** 12+
- **MySQL** 8.0+
- **In-Memory** (for testing)

## Performance

- **Query Filtering** - Automatic tenant isolation in queries
- **Caching** - Configurable memory cache for tenants and settings
- **Connection Pooling** - Optimized database connection management
- **Async Operations** - Full async/await support throughout

## Testing

```csharp
var services = new ServiceCollection();

services.AddTenantIsolationInMemory("TestDb", options =>
{
    options.AutoMigrate = false;
    options.EnableAuditLogging = false;
});

var provider = services.BuildServiceProvider();
var context = provider.GetRequiredService<TenantDbContext>();
await context.Database.EnsureCreatedAsync();
```

## License

MIT License - Copyright © 2026 Vladyslav Zaiets

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/sarmkadan/dotnet-tenant-isolation).

## Roadmap

- [ ] Entity Framework Core EF6 compatibility
- [ ] Redis-based distributed caching
- [ ] Tenant migration utilities
- [ ] Multi-database query federation
- [ ] Advanced audit logging system
- [ ] GraphQL support
- [ ] Event-driven architecture
- [ ] Tenant provisioning API
