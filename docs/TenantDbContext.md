# TenantDbContext

`TenantDbContext` serves as the primary Entity Framework Core database context for the tenant isolation system. It aggregates all tenant-related domain entities into a single unit of work, enabling centralized persistence, querying, and transactional coordination across tenants, their configurations, connection strings, organizations, users, data isolation policies, and feature flags.

## API

### Constructors

#### `public TenantDbContext()`
Parameterless constructor. Initializes a new instance of the context without explicit options. Typically used in design-time scenarios or when options are injected via `OnConfiguring` override.

#### `public TenantDbContext(DbContextOptions<TenantDbContext> options)`
Initializes a new instance with the specified options delegate. This is the primary constructor for production use, allowing configuration of the database provider, connection string, and other context behaviors through dependency injection.

- **Parameters**:
  - `options` (`DbContextOptions<TenantDbContext>`): The options to configure the context.
- **Throws**:
  - `ArgumentNullException`: When `options` is `null`.

### Properties

#### `public DbSet<Tenant> Tenants`
Gets or sets the `DbSet<Tenant>` representing the collection of tenants in the system. Each tenant corresponds to a logically isolated customer or organizational unit.

#### `public DbSet<TenantConfiguration> TenantConfigurations`
Gets or sets the `DbSet<TenantConfiguration>` containing key-value configuration entries scoped to individual tenants.

#### `public DbSet<TenantConnectionString> TenantConnectionStrings`
Gets or sets the `DbSet<TenantConnectionString>` holding database connection strings assigned to tenants, supporting per-tenant database isolation strategies.

#### `public DbSet<Organization> Organizations`
Gets or sets the `DbSet<Organization>` for organizational hierarchies within tenants, enabling multi-level grouping of users and resources.

#### `public DbSet<User> Users`
Gets or sets the `DbSet<User>` representing user accounts associated with tenants and organizations.

#### `public DbSet<DataIsolationPolicy> DataIsolationPolicies`
Gets or sets the `DbSet<DataIsolationPolicy>` defining the isolation rules applied to tenant data, such as shared-database, separate-schema, or separate-database strategies.

#### `public DbSet<TenantFeature> TenantFeatures`
Gets or sets the `DbSet<TenantFeature>` tracking feature flags or capabilities enabled on a per-tenant basis.

### Methods

#### `public override int SaveChanges()`
Saves all pending entity changes to the underlying database synchronously. Overrides the base `DbContext.SaveChanges` to allow interception or augmentation of the save pipeline specific to tenant isolation logic.

- **Returns**: The number of state entries written to the database.
- **Throws**:
  - `DbUpdateException`: When an error occurs while saving changes, typically wrapping provider-specific exceptions.
  - `DbUpdateConcurrencyException`: When an optimistic concurrency conflict is detected.

#### `public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)`
Saves all pending entity changes to the underlying database asynchronously. Overrides the base `DbContext.SaveChangesAsync` to allow interception or augmentation of the save pipeline specific to tenant isolation logic.

- **Parameters**:
  - `cancellationToken` (`CancellationToken`, optional): A token to observe while waiting for the task to complete.
- **Returns**: A task representing the asynchronous save operation. The task result contains the number of state entries written to the database.
- **Throws**:
  - `DbUpdateException`: When an error occurs while saving changes.
  - `DbUpdateConcurrencyException`: When an optimistic concurrency conflict is detected.
  - `OperationCanceledException`: When `cancellationToken` is triggered.

## Usage

### Example 1: Registering a New Tenant with Configuration and Connection String

```csharp
using var context = new TenantDbContext(options);

var tenant = new Tenant
{
    Id = Guid.NewGuid(),
    Name = "Acme Corp",
    CreatedAt = DateTime.UtcNow
};

var config = new TenantConfiguration
{
    TenantId = tenant.Id,
    Key = "Theme",
    Value = "Dark"
};

var connectionString = new TenantConnectionString
{
    TenantId = tenant.Id,
    ConnectionString = "Server=acme-db;Database=AcmeData;...",
    IsolationMode = IsolationMode.SeparateDatabase
};

context.Tenants.Add(tenant);
context.TenantConfigurations.Add(config);
context.TenantConnectionStrings.Add(connectionString);

int rowsAffected = await context.SaveChangesAsync();
Console.WriteLine($"Persisted {rowsAffected} changes.");
```

### Example 2: Querying Users with Their Tenant and Organization

```csharp
using var context = new TenantDbContext(options);

var usersInTenant = await context.Users
    .Include(u => u.Organization)
    .ThenInclude(o => o.Tenant)
    .Where(u => u.Organization.Tenant.Name == "Acme Corp")
    .Select(u => new
    {
        u.Email,
        OrganizationName = u.Organization.Name,
        TenantName = u.Organization.Tenant.Name,
        u.IsActive
    })
    .ToListAsync();

foreach (var user in usersInTenant)
{
    Console.WriteLine($"{user.Email} -> {user.OrganizationName} ({user.TenantName})");
}
```

## Notes

- **Thread Safety**: `TenantDbContext` is not thread-safe. Instances should be scoped per unit of work (e.g., per HTTP request or per operation) and must not be shared across concurrent threads. This follows standard Entity Framework Core guidance.
- **SaveChanges Override Behavior**: The overridden `SaveChanges` and `SaveChangesAsync` methods may contain custom logic such as automatic tenant context stamping, audit tracking, or validation. Callers should not assume behavior identical to the base implementation.
- **Disposal**: `TenantDbContext` implements `IAsyncDisposable` and `IDisposable` through its base class. Instances created with `new` or dependency injection should be disposed promptly to release database connections.
- **Large Transactions**: When saving changes across multiple `DbSet` properties in a single `SaveChanges` call, all modifications participate in the same transaction. Partial failures will roll back the entire batch.
- **Concurrency Conflicts**: If multiple processes modify the same tenant, user, or configuration row concurrently, `SaveChanges` may throw `DbUpdateConcurrencyException`. Callers should implement retry logic or conflict resolution strategies where appropriate.
- **Connection String Sensitivity**: `TenantConnectionStrings` stores raw connection strings. Ensure appropriate encryption at rest and access controls are applied to prevent credential exposure.
