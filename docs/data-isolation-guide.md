# Data Isolation Strategies Comparison Guide

Multi-tenancy introduces complexities in how tenant-specific data is stored and isolated. This guide compares the three primary data isolation strategies supported by this framework, outlining their trade-offs to help you choose the best approach for your application.

## 1. Shared Database with Row-Level Filtering

In this strategy, all tenants share a single database and schema. Tenant data is distinguished by a `TenantId` column in every relevant table, and queries are automatically filtered to ensure users only access their own tenant's data.

**Pros:**
*   **Lowest Cost:** Requires minimal database infrastructure (single database instance).
*   **Simplest Initial Setup:** Easy to get started as all tenants use the same connection string and schema.
*   **Simplified Database Management:** Backups, maintenance, and schema updates apply to all tenants simultaneously.
*   **Efficient Cross-Tenant Queries:** Useful for analytics or super-admin functions that need to query across all tenants.

**Cons:**
*   **Highest Complexity in Application Logic:** Requires vigilant application of `TenantId` filters on every query to prevent data leakage. (The framework aims to automate this, but developers must remain aware).
*   **Query Performance Overhead:** Every query includes a `WHERE TenantId = @TenantId` clause, which can impact performance on very large datasets if not properly indexed.
*   **Security Risk:** Higher risk of data leakage if filters are accidentally omitted or misconfigured. Requires robust query filtering (e.g., global query filters in EF Core, or database-level Row-Level Security).
*   **Compliance Challenges:** May not meet strict data residency or compliance requirements (e.g., GDPR, HIPAA) where physical separation of data is mandated.
*   **Noisy Neighbor Effect:** A single tenant with high database usage can impact the performance of other tenants.

**Use Cases:**
*   SaaS applications with many small tenants.
*   Applications where data residency is not a strict requirement.
*   Budget-constrained projects.
*   Rapid prototyping.

**Example Configuration (Conceptual):**

`TenantDbContext.cs`:
```csharp
public class TenantDbContext : DbContext
{
    private readonly Guid _tenantId;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, Guid tenantId) : base(options)
    {
        _tenantId = tenantId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ... other configurations ...
        modelBuilder.Entity<Order>().HasQueryFilter(e => e.TenantId == _tenantId);
        modelBuilder.Entity<Product>().HasQueryFilter(e => e.TenantId == _tenantId);
    }
}
```

## 2. Shared Database with Schema-Per-Tenant

In this strategy, all tenants share a single database instance, but each tenant has its own dedicated schema within that database. This provides a logical separation of data at the database level.

**Pros:**
*   **Good Data Isolation:** Data is logically separated by schema, reducing the risk of accidental cross-tenant data access.
*   **Improved Security:** Database permissions can be granted at the schema level, adding an extra layer of security.
*   **Simpler Queries (within a tenant):** Queries do not explicitly need `TenantId` filters, as the connection itself is pointed to the tenant's schema.
*   **Easier Compliance:** Offers a stronger argument for data separation for some compliance requirements compared to row-level filtering.

**Cons:**
*   **Increased Management Overhead:** Managing schemas (creation, migration, backup) for each tenant adds complexity.
*   **More Complex Database Migrations:** Schema changes must be applied to each tenant's schema individually, which can be time-consuming for many tenants.
*   **Higher Database Resource Consumption:** Each schema adds some overhead, and object names must be unique within each schema.
*   **Challenging Cross-Tenant Queries:** Querying across all tenants becomes more complex as it requires querying multiple schemas.

**Use Cases:**
*   SaaS applications requiring better data isolation than row-level filtering.
*   Compliance requirements that prefer logical data separation.
*   Applications with a moderate number of tenants.
*   When a "noisy neighbor" effect needs mitigation at the schema level.

**Example Configuration (Conceptual):**

Connection string for a tenant: `Server=my_db;Database=shared_database;User Id=tenant_user;Password=tenant_password;Search Path=tenant_schema_A;`

`TenantDbContext.cs`:
```csharp
public class TenantDbContext : DbContext
{
    private readonly string _tenantSchema;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, string tenantSchema) : base(options)
    {
        _tenantSchema = tenantSchema;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType == null) // Only map root entities to schemas
            {
                entityType.SetSchema(_tenantSchema);
            }
        }
        base.OnModelCreating(modelBuilder);
    }
}
```

## 3. Dedicated Database Per Tenant

Each tenant has its own entirely separate database instance. This is the highest level of isolation.

**Pros:**
*   **Highest Data Isolation:** Complete physical separation of data, offering the strongest security and compliance guarantees (e.g., GDPR data residency).
*   **Maximum Security:** Database-level security, backups, and restores are performed per tenant.
*   **Optimal Performance:** Eliminates the "noisy neighbor" effect as each tenant has dedicated resources. Performance scales linearly with the number of database instances.
*   **Simplified Single-Tenant Development:** Within a tenant's database, development feels like a single-tenant application.
*   **Easier Customization:** Allows for tenant-specific database customizations or schema variations if needed.

**Cons:**
*   **Highest Cost:** Requires a separate database instance for each tenant, leading to significant infrastructure costs as the number of tenants grows.
*   **Highest Operational Complexity:** Managing, monitoring, backing up, and migrating many database instances is complex and resource-intensive.
*   **Challenging Cross-Tenant Operations:** Extremely difficult to perform cross-tenant queries or aggregate data across all tenants.
*   **Resource Utilization:** May lead to underutilized database instances for small tenants.

**Use Cases:**
*   Applications with very strict security, compliance, or data residency requirements.
*   Applications with a small number of large, high-value tenants.
*   Situations where performance isolation is critical for each tenant.
*   When tenants demand full control over their database environment.

**Example Configuration (Conceptual):**

Connection string for a tenant: `Server=tenant_db_server;Database=tenant_database_A;User Id=user;Password=password;`

The connection string would be dynamically determined by the `TenantResolutionService` and used to construct `DbContextOptions`.

## Decision Matrix

| Feature / Strategy       | Shared DB (Row-Level) | Shared DB (Schema-Per-Tenant) | Dedicated DB Per Tenant |
| :----------------------- | :-------------------- | :---------------------------- | :---------------------- |
| **Cost**                 | Low                   | Medium                        | High                    |
| **Complexity**           | Medium (App filters)  | Medium-High (Schema mgmt)     | High (Infra mgmt)       |
| **Data Isolation**       | Low (Logical)         | Medium (Logical/Physical)     | High (Physical)         |
| **Security**             | Medium                | Good                          | Excellent               |
| **Query Performance**    | Good (with indexes)   | Very Good                     | Excellent               |
| **Compliance (GDPR etc.)** | Challenging           | Moderate                      | Strong                  |
| **Cross-Tenant Queries** | Easy                  | Complex                       | Extremely Difficult     |
| **"Noisy Neighbor"**     | High Risk             | Medium Risk                   | Low Risk                |
| **Scalability**          | Vertical Only         | Vertical/Horizontal (DB)      | Horizontal (DB Instances) |

## Conclusion

Choosing the right data isolation strategy depends heavily on your application's specific requirements, especially concerning security, compliance, performance, and budget. The framework provides the tools to implement any of these, but understanding their implications is crucial for a successful multi-tenant architecture.
