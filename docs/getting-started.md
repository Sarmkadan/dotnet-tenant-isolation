# Getting Started with dotnet-tenant-isolation

This guide walks you through setting up a multi-tenant ASP.NET Core application from scratch using the dotnet-tenant-isolation framework.

## Prerequisites

- **.NET 10 SDK** or later ([download](https://dotnet.microsoft.com/download))
- **Visual Studio 2024**, **VS Code**, or **JetBrains Rider**
- **SQL Server Express 2019+** or **PostgreSQL 12+** (or use in-memory for development)
- Basic knowledge of ASP.NET Core and Entity Framework Core

## Step 1: Create a New ASP.NET Core Project

```bash
dotnet new webapi -n MultiTenantApp
cd MultiTenantApp
```

## Step 2: Add the Framework NuGet Package

```bash
dotnet add package dotnet-tenant-isolation
```

## Step 3: Update Program.cs

Replace the contents of `Program.cs` with:

```csharp
using TenantIsolation.Configuration;
using TenantIsolation.Services;

var builder = WebApplication.CreateBuilder(args);

// Add TenantIsolation services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddTenantIsolationSqlServer(connectionString, options =>
{
    options.AutoMigrate = true;
    options.EnableAuditLogging = true;
    options.CacheDurationMinutes = 60;
});

builder.Services.AddTenantFeatureToggle();

// Add controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add TenantResolution middleware
app.UseTenantResolution();

app.UseAuthorization();

app.MapControllers();

app.Run();
```

## Step 4: Configure Database Connection

Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MultiTenantDb;Trusted_Connection=true;"
  },
  "TenantIsolation": {
    "AutoMigrate": true,
    "EnableAuditLogging": true,
    "CacheDurationMinutes": 60,
    "TenantIdentificationStrategy": "Header"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  }
}
```

For PostgreSQL, use:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=multitenant_db;Username=postgres;Password=password"
  }
}
```

## Step 5: Create Your First API Controller

Create `Controllers/TenantsController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using TenantIsolation.Models;
using TenantIsolation.Services;

namespace MultiTenantApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly TenantService _tenantService;
    private readonly TenantResolutionService _tenantResolution;

    public TenantsController(
        TenantService tenantService,
        TenantResolutionService tenantResolution)
    {
        _tenantService = tenantService;
        _tenantResolution = tenantResolution;
    }

    /// <summary>Create a new tenant</summary>
    [HttpPost]
    public async Task<ActionResult<Tenant>> CreateTenant(CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Slug) ||
            string.IsNullOrWhiteSpace(request.AdminEmail))
        {
            return BadRequest("Name, slug, and admin email are required");
        }

        var tenant = await _tenantService.CreateTenantAsync(
            request.Name,
            request.Slug,
            request.AdminEmail);

        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
    }

    /// <summary>Get a specific tenant</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Tenant>> GetTenant(Guid id)
    {
        var tenant = await _tenantService.GetTenantAsync(id);

        if (tenant == null)
            return NotFound($"Tenant {id} not found");

        return Ok(tenant);
    }

    /// <summary>Get tenant by slug</summary>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<Tenant>> GetTenantBySlug(string slug)
    {
        var tenant = await _tenantService.GetTenantBySlugAsync(slug);

        if (tenant == null)
            return NotFound($"Tenant '{slug}' not found");

        return Ok(tenant);
    }

    /// <summary>Get current tenant from request</summary>
    [HttpGet("current")]
    public async Task<ActionResult<Tenant>> GetCurrentTenant()
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Could not resolve tenant from request");

        return Ok(tenant);
    }

    /// <summary>Activate a tenant</summary>
    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> ActivateTenant(Guid id)
    {
        var tenant = await _tenantService.GetTenantAsync(id);

        if (tenant == null)
            return NotFound();

        await _tenantService.ActivateTenantAsync(id);
        return NoContent();
    }

    /// <summary>Suspend a tenant</summary>
    [HttpPut("{id:guid}/suspend")]
    public async Task<IActionResult> SuspendTenant(Guid id)
    {
        var tenant = await _tenantService.GetTenantAsync(id);

        if (tenant == null)
            return NotFound();

        await _tenantService.SuspendTenantAsync(id);
        return NoContent();
    }

    /// <summary>Delete a tenant</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        var tenant = await _tenantService.GetTenantAsync(id);

        if (tenant == null)
            return NotFound();

        await _tenantService.DeleteTenantAsync(id);
        return NoContent();
    }
}

public class CreateTenantRequest
{
    public string Name { get; set; }
    public string Slug { get; set; }
    public string AdminEmail { get; set; }
}
```

## Step 6: Run Database Migrations

The framework includes automatic migrations with `AutoMigrate = true`. When you first run the application, it will create all necessary tables.

Alternatively, manually run migrations:
```bash
dotnet ef database update
```

## Step 7: Test Your API

### Create a Tenant

```bash
curl -X POST https://localhost:7001/api/tenants \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Acme Corporation",
    "slug": "acme",
    "adminEmail": "admin@acme.com"
  }'
```

Response:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Acme Corporation",
  "slug": "acme",
  "status": "Active",
  "createdAt": "2026-05-04T10:00:00Z"
}
```

### Get Current Tenant

```bash
curl -X GET https://localhost:7001/api/tenants/current \
  -H "X-Tenant-Id: 550e8400-e29b-41d4-a716-446655440000"
```

### Get Tenant by Slug

```bash
curl -X GET https://localhost:7001/api/tenants/slug/acme
```

## Step 8: Create a Multi-Tenant Data Controller

Create `Controllers/OrganizationsController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using TenantIsolation.Data;
using TenantIsolation.Services;

namespace MultiTenantApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationsController : ControllerBase
{
    private readonly OrganizationRepository _repository;
    private readonly TenantResolutionService _tenantResolution;

    public OrganizationsController(
        OrganizationRepository repository,
        TenantResolutionService tenantResolution)
    {
        _repository = repository;
        _tenantResolution = tenantResolution;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrganizations()
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Tenant not resolved");

        var organizations = await _repository.GetActiveOrganizationsAsync(tenant.Id);
        return Ok(organizations);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrganization(CreateOrganizationRequest request)
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Tenant not resolved");

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _repository.AddAsync(organization);
        return CreatedAtAction(nameof(GetOrganizations), organization);
    }
}

public class CreateOrganizationRequest
{
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Description { get; set; }
}
```

## Step 9: Enable Feature Toggles (Optional)

Create `Controllers/FeaturesController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using TenantIsolation.Services;

namespace MultiTenantApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeaturesController : ControllerBase
{
    private readonly TenantFeatureService _featureService;
    private readonly TenantResolutionService _tenantResolution;

    public FeaturesController(
        TenantFeatureService featureService,
        TenantResolutionService tenantResolution)
    {
        _featureService = featureService;
        _tenantResolution = tenantResolution;
    }

    [HttpGet("{featureKey}/enabled")]
    public async Task<ActionResult<bool>> IsFeatureEnabled(string featureKey)
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Tenant not resolved");

        var isEnabled = await _featureService.IsFeatureEnabledAsync(tenant.Id, featureKey);
        return Ok(isEnabled);
    }

    [HttpPost("{featureKey}/enable")]
    public async Task<IActionResult> EnableFeature(string featureKey)
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Tenant not resolved");

        await _featureService.EnableFeatureAsync(tenant.Id, featureKey);
        return NoContent();
    }

    [HttpPost("{featureKey}/disable")]
    public async Task<IActionResult> DisableFeature(string featureKey)
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Tenant not resolved");

        await _featureService.DisableFeatureAsync(tenant.Id, featureKey);
        return NoContent();
    }
}
```

## Step 10: Run Your Application

```bash
dotnet run
```

The API will be available at `https://localhost:7001/swagger`.

## Next Steps

- **Enable Authentication**: Add JWT or OAuth2 for secure tenant resolution
- **Configure Data Isolation**: Set up data isolation policies in `Startup.cs`
- **Add Logging**: Configure serilog for structured logging
- **Deploy**: Use Docker for containerized deployment
- **Monitor**: Set up health checks and metrics

## Common Issues

### "Tenant not resolved"
Ensure you're sending the `X-Tenant-Id` header or using route parameters for tenant identification.

### "Database does not exist"
Check your connection string and ensure the database server is running:
```bash
# SQL Server
sqlcmd -S localhost -U sa -P "YourPassword"

# PostgreSQL
psql -h localhost -U postgres
```

### "Migrations pending"
Run pending migrations:
```bash
dotnet ef database update
```

## Further Reading

- [Architecture Documentation](architecture.md)
- [API Reference](api-reference.md)
- [Deployment Guide](deployment.md)
- [FAQ](faq.md)
