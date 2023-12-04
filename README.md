// existing content ...

## TenantResolutionBenchmarks

The `TenantResolutionBenchmarks` class provides a set of benchmarks for evaluating the performance of the tenant resolution process. It includes tests for resolving tenants from headers, routes, claims, and subdomains, as well as tests for getting the current tenant and checking if a tenant exists.

### Example Usage

```csharp
// Create a new instance of TenantResolutionBenchmarks
var tenantResolutionBenchmarks = new TenantResolutionBenchmarks();

// Setup the tenant resolution benchmarks
tenantResolutionBenchmarks.Setup();

// Resolve a tenant from the header
var tenantFromHeader = await tenantResolutionBenchmarks.ResolveTenant_FromHeader();

// Resolve a tenant from the route
var tenantFromRoute = await tenantResolutionBenchmarks.ResolveTenant_FromRoute();

// Resolve a tenant from claims
var tenantFromClaims = await tenantResolutionBenchmarks.ResolveTenant_FromClaims();

// Resolve a tenant from the subdomain
var tenantFromSubdomain = await tenantResolutionBenchmarks.ResolveTenant_FromSubdomain();

// Get the current tenant
var currentTenant = tenantResolutionBenchmarks.GetCurrentTenant();

// Check if a tenant exists
var hasTenant = tenantResolutionBenchmarks.HasTenant;

// Cleanup the tenant resolution benchmarks
tenantResolutionBenchmarks.Cleanup();

// Dispose of the tenant resolution benchmarks
tenantResolutionBenchmarks.Dispose();
```

// existing content ...
