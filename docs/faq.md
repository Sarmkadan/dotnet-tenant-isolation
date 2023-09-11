# Frequently Asked Questions

## General Questions

### Q: What is multi-tenancy?

**A:** Multi-tenancy is an architecture where a single application instance serves multiple independent customers (tenants). Each tenant's data is isolated and invisible to others, while sharing infrastructure to reduce costs.

### Q: Why use dotnet-tenant-isolation?

**A:** This framework handles complex multi-tenancy concerns transparently:
- Automatic tenant detection from requests
- Data isolation policies with type safety
- Per-tenant configuration management
- Feature toggles per tenant
- Built-in caching and performance optimization
- Production-ready middleware

### Q: Is this framework suitable for my SaaS application?

**A:** Yes, if you're building an ASP.NET Core 10 SaaS application with:
- Multiple independent customers
- Need for per-tenant configuration
- Data isolation requirements
- Feature rollout capabilities

It's used in production by companies serving hundreds of thousands of users.

### Q: What versions of .NET are supported?

**A:** .NET 10 and later. The framework uses the latest C# 14 features and Entity Framework Core 10.

### Q: What databases are supported?

**A:** 
- SQL Server 2019+
- PostgreSQL 12+
- MySQL 8.0+
- In-Memory (for testing)

## Installation & Setup

### Q: How do I install the framework?

**A:** Via NuGet:
```bash
dotnet add package dotnet-tenant-isolation
```

Or via Package Manager Console:
```powershell
Install-Package dotnet-tenant-isolation
```

### Q: Where do I register the framework?

**A:** In `Program.cs`:
```csharp
builder.Services.AddTenantIsolationSqlServer(connectionString, options => { ... });
app.UseTenantResolution();
```

### Q: Can I use it with existing projects?

**A:** Yes! You can add it to existing ASP.NET Core projects. It integrates as middleware and services without requiring major refactoring.

### Q: Do I need to create migrations manually?

**A:** No, with `AutoMigrate: true` in configuration, migrations run automatically on startup. You can also run `dotnet ef database update` manually.

## Architecture & Design

### Q: What data isolation strategy should I use?

**A:** It depends on your use case:

- **Database-per-Tenant**: Highest security, easiest separation, higher infrastructure cost. Best for: Enterprise customers, HIPAA compliance.
- **Schema-per-Tenant**: Balanced approach, moderate cost, reasonable isolation. Best for: Mid-market SaaS.
- **Row-Level Security**: Lowest cost, requires careful coding, easiest to accidentally leak data. Best for: Small numbers of tenants, tight cost control.
- **Hybrid**: Combine strategies for different entity types. Best for: Complex requirements.

### Q: How does tenant resolution work?

**A:** The framework uses a cascading strategy:
1. Checks `X-Tenant-Id` or `X-Tenant-Slug` header
2. Checks user claims if authenticated
3. Checks route parameters (`{tenantId}`, `{slug}`)
4. Extracts from subdomain
5. Throws exception if all fail

### Q: Can I customize tenant resolution?

**A:** Yes, you can:
- Implement `ITenantResolver` interface
- Register custom resolver in DI
- Use in middleware before framework resolution

### Q: What's the difference between Strict and Relaxed isolation policies?

**A:** 
- **Strict**: No cross-tenant access allowed (default). Safe but restrictive.
- **Relaxed**: Allows access from specific allow-listed tenants. Useful for shared resources.
- **Custom**: Define custom filter rules. For complex scenarios.

## Performance & Caching

### Q: Is configuration cached?

**A:** Yes, 1-hour TTL by default. Cache is invalidated when configuration changes. You can adjust:
```csharp
options.CacheDurationMinutes = 120;  // 2 hours
```

### Q: How does feature toggle rollout work?

**A:** Rollout percentage is probabilistic:
- 25% rollout: `IsFeatureEnabled()` returns true ~25% of calls
- Use for A/B testing and gradual rollouts
- For deterministic testing, set to 0% or 100%

### Q: What happens when cache expires?

**A:** The next request triggers a database query to refresh the cache. This is transparent to the application.

### Q: Can I use distributed caching (Redis)?

**A:** Yes, the framework integrates with `IDistributedCache`:
```bash
dotnet add package StackExchange.Redis
```

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = connectionString;
});
```

### Q: How many tenants can one instance support?

**A:** Depends on:
- Database size and query performance
- Configuration cache size
- Memory available
- Default: tested up to 5,000 tenants per instance
- With optimization: 10,000+

## Data & Queries

### Q: How do I query data for the current tenant?

**A:** The framework automatically filters queries:
```csharp
var tenant = await tenantResolution.ResolveTenantAsync();
dbContext.SetCurrentTenant(tenant.Id);

// Queries automatically include TenantId filter
var users = await dbContext.Users.ToListAsync();
```

### Q: Can I accidentally leak data to other tenants?

**A:** The framework prevents this through:
- Global query filters (automatic WHERE TenantId = @TenantId)
- Data isolation policies with field-level ACLs
- Exception throwing on isolation violations
- Tests recommended for validation

### Q: How do I handle cross-tenant queries?

**A:** Use Relaxed or Custom isolation policies:
```csharp
var policy = await isolationService.CreatePolicyAsync(
    tenantId, "Document", DataIsolationPolicyType.Relaxed,
    allowedCrossTenantAccess: "other-tenant-id");
```

### Q: What if I need to query all tenants' data?

**A:** Temporarily clear the tenant context:
```csharp
dbContext.ClearCurrentTenant();

var allUsers = await dbContext.Users.ToListAsync();

dbContext.SetCurrentTenant(tenantId);
```

## Configuration

### Q: How do I store per-tenant settings?

**A:** Use `ConfigurationService`:
```csharp
await configService.SetConfigurationAsync(
    tenantId, "api:rateLimit", "1000", valueType: "int");

var rateLimit = await configService.GetConfigurationAsync<int>(
    tenantId, "api:rateLimit");
```

### Q: Can I encrypt sensitive configuration?

**A:** Yes:
```csharp
await configService.SetConfigurationAsync(
    tenantId, "apiKey", "secret", isEncrypted: true);
```

### Q: How do I migrate configuration between tenants?

**A:**
```csharp
// Export from source
var json = await configService.ExportConfigurationAsync(sourceTenant);

// Import to destination
await configService.ImportConfigurationAsync(destTenant, json);
```

## Feature Toggles

### Q: How do I implement feature flags?

**A:**
```csharp
if (await featureService.IsFeatureEnabledAsync(tenantId, "advanced-analytics"))
{
    // Show feature
}
```

### Q: How do I gradually roll out features?

**A:** Use rollout percentage:
```csharp
// Week 1: 10% rollout
await featureService.SetRolloutPercentageAsync(tenantId, "feature", 10);

// Week 2: 50% rollout
await featureService.SetRolloutPercentageAsync(tenantId, "feature", 50);

// Week 3: 100% rollout
await featureService.SetRolloutPercentageAsync(tenantId, "feature", 100);
```

### Q: Can I track feature usage?

**A:** Yes:
```csharp
await featureService.RecordFeatureUsageAsync(tenantId, "feature-key");

var stats = await featureService.GetStatisticsAsync(tenantId);
// stats.Features[0].UsageCount
```

### Q: What's the difference between Enable and Rollout?

**A:**
- **Enable**: Feature is always available (100% rollout)
- **Rollout Percentage**: Feature available ~X% of the time (probabilistic)
- Use Rollout for: A/B testing, gradual rollouts, canary deployments
- Use Enable for: Full feature activation

## Testing

### Q: How do I test multi-tenant code?

**A:** Use in-memory database:
```csharp
var services = new ServiceCollection();

services.AddTenantIsolationInMemory("TestDb", options =>
{
    options.AutoMigrate = false;
});

var provider = services.BuildServiceProvider();
var context = provider.GetRequiredService<TenantDbContext>();
await context.Database.EnsureCreatedAsync();
```

### Q: How do I mock tenant resolution in tests?

**A:**
```csharp
var mockResolution = new Mock<TenantResolutionService>();
mockResolution.Setup(x => x.ResolveTenantAsync())
    .ReturnsAsync(new Tenant { Id = testTenantId });

// Use in your test service
```

### Q: What code coverage should I aim for?

**A:** At least 80% for multi-tenancy critical paths:
- Tenant creation/activation
- Data isolation enforcement
- Configuration retrieval
- Feature toggle evaluation

## Migration & Upgrades

### Q: How do I migrate from single-tenant to multi-tenant?

**A:** 
1. Create `Tenant` table
2. Add `TenantId` column to existing tables
3. Backfill `TenantId` with default value
4. Add foreign keys and indexes
5. Implement tenant resolution
6. Update queries to filter by tenant

### Q: Can I upgrade from an older version?

**A:** Yes, the framework maintains backwards compatibility. Always check the CHANGELOG for breaking changes.

### Q: What if I need to merge or split tenants?

**A:** You can manually:
1. Create new tenant
2. Copy data with new TenantId
3. Update references
4. Soft-delete old tenant

The framework provides utilities:
```csharp
// Custom migration logic in services
```

## Troubleshooting

### Q: I get "Tenant could not be resolved" - what should I do?

**A:** 
1. Check if tenant ID is being sent in request (header, route, claim, subdomain)
2. Verify tenant exists: `SELECT * FROM Tenants WHERE Id = @TenantId`
3. Enable debug logging to see resolution attempts
4. Try different resolution strategies

### Q: Database queries are slow - how do I optimize?

**A:**
1. Check if indexes exist on TenantId columns
2. Enable query logging to see generated SQL
3. Increase cache duration for stable configuration
4. Consider connection pooling settings
5. Profile with SQL Server Management Studio

### Q: How do I debug data isolation violations?

**A:**
1. Enable debug logging: `LogLevel: Debug` for framework
2. Catch `DataIsolationViolationException`
3. Review data isolation policies
4. Manually verify query results

### Q: Feature toggles are inconsistent - why?

**A:**
- Rollout percentage is probabilistic - enable 0% or 100% for consistent behavior in tests
- Check feature is enabled in database
- Verify cache hasn't expired (or restart app)

### Q: What if migration fails?

**A:**
```bash
# Check pending migrations
dotnet ef migrations list

# Revert migrations
dotnet ef database update <previous-migration>

# Remove failed migration
dotnet ef migrations remove

# Reapply correctly
dotnet ef migrations add <name>
dotnet ef database update
```

## Best Practices

### Q: What should I avoid in multi-tenant applications?

**A:** Don't:
- Query without tenant filter
- Store unencrypted credentials
- Share caches between tenants
- Log sensitive customer data
- Assume tenant is always resolved

### Q: How should I structure my code?

**A:** 
- Inject `TenantResolutionService` where needed
- Use repositories for data access
- Filter all queries by tenant
- Use data isolation policies early

### Q: Should I use repositories or direct DbContext?

**A:** Use repositories:
- Encapsulate tenant filtering logic
- Easier to test
- Better for complex queries
- Prevents accidental data leaks

### Q: How often should I backup tenant data?

**A:** 
- At least daily for production
- Hourly for critical workloads
- Test restore procedures regularly

### Q: Should I separate audit logs by tenant?

**A:** Yes:
- Each tenant should only see their audit logs
- Can be in separate tables or filtered
- Helps with compliance (GDPR, etc.)

## License & Support

### Q: What license is this under?

**A:** MIT License - free for commercial and personal use. See LICENSE file for details.

### Q: How do I report issues?

**A:** 
1. Check existing GitHub issues first
2. Create new issue with:
   - Minimal reproducible example
   - .NET version: `dotnet --version`
   - Package version
   - Error message and stack trace

### Q: Where can I get help?

**A:**
- GitHub Issues: Bug reports and feature requests
- Discussions: General questions and help
- Documentation: Comprehensive guides
- Examples: Real-world usage patterns

### Q: Can I contribute to the project?

**A:** Yes! Fork the repo, make improvements, and submit a pull request. See CONTRIBUTING.md for guidelines.

### Q: Is there commercial support available?

**A:** Not currently. Community support is available through GitHub Issues and Discussions.
