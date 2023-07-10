# API Reference

Complete API documentation for the dotnet-tenant-isolation framework.

## TenantService

Core service for managing tenant lifecycle and operations.

### CreateTenantAsync

```csharp
Task<Tenant> CreateTenantAsync(string name, string slug, string adminEmail)
```

Creates a new tenant with default configuration.

**Parameters:**
- `name` (string): Display name of the tenant
- `slug` (string): URL-friendly identifier (must be unique)
- `adminEmail` (string): Email address of primary administrator

**Returns:** `Tenant` object with auto-assigned ID

**Throws:**
- `TenantIsolationException`: If validation fails (invalid name, duplicate slug, etc.)

**Example:**
```csharp
var tenant = await tenantService.CreateTenantAsync(
    "Acme Corporation",
    "acme-corp",
    "admin@acme.com");

Console.WriteLine($"Created tenant {tenant.Slug} with ID {tenant.Id}");
```

### GetTenantAsync

```csharp
Task<Tenant> GetTenantAsync(Guid tenantId)
```

Retrieves a specific tenant by ID.

**Parameters:**
- `tenantId` (Guid): Unique identifier of the tenant

**Returns:** `Tenant` object or `null` if not found

**Example:**
```csharp
var tenant = await tenantService.GetTenantAsync(tenantId);

if (tenant == null)
    Console.WriteLine("Tenant not found");
```

### GetTenantBySlugAsync

```csharp
Task<Tenant> GetTenantBySlugAsync(string slug)
```

Retrieves a tenant by its unique slug.

**Parameters:**
- `slug` (string): URL-friendly identifier

**Returns:** `Tenant` object or `null` if not found

**Example:**
```csharp
var tenant = await tenantService.GetTenantBySlugAsync("acme-corp");
```

### GetActiveTenantAsync

```csharp
Task<Tenant> GetActiveTenantAsync(Guid tenantId)
```

Retrieves a tenant only if its status is "Active".

**Parameters:**
- `tenantId` (Guid): Unique identifier of the tenant

**Returns:** `Tenant` object or throws exception if not active

**Throws:** `TenantNotActiveException`

**Example:**
```csharp
try
{
    var tenant = await tenantService.GetActiveTenantAsync(tenantId);
}
catch (TenantNotActiveException ex)
{
    Console.WriteLine($"Tenant is {ex.Status}");
}
```

### ActivateTenantAsync

```csharp
Task ActivateTenantAsync(Guid tenantId)
```

Activates a tenant, changing its status to "Active".

**Parameters:**
- `tenantId` (Guid): Unique identifier of the tenant

**Returns:** `Task` (no return value)

**Throws:**
- `TenantNotResolvedException`: If tenant doesn't exist
- `TenantNotActiveException`: If already active

**Example:**
```csharp
await tenantService.ActivateTenantAsync(tenantId);
Console.WriteLine("Tenant activated successfully");
```

### SuspendTenantAsync

```csharp
Task SuspendTenantAsync(Guid tenantId)
```

Suspends a tenant, preventing further access.

**Parameters:**
- `tenantId` (Guid): Unique identifier of the tenant

**Returns:** `Task` (no return value)

**Throws:** `TenantNotResolvedException`

**Example:**
```csharp
await tenantService.SuspendTenantAsync(tenantId);
```

### DeleteTenantAsync

```csharp
Task DeleteTenantAsync(Guid tenantId)
```

Soft-deletes a tenant (marks as deleted without removing from database).

**Parameters:**
- `tenantId` (Guid): Unique identifier of the tenant

**Returns:** `Task` (no return value)

**Throws:** `TenantNotResolvedException`

**Example:**
```csharp
await tenantService.DeleteTenantAsync(tenantId);
```

### IsSubscriptionValidAsync

```csharp
Task<bool> IsSubscriptionValidAsync(Guid tenantId)
```

Checks if a tenant's subscription is active and not expired.

**Parameters:**
- `tenantId` (Guid): Unique identifier of the tenant

**Returns:** `bool` - true if valid, false if expired or not set

**Example:**
```csharp
if (await tenantService.IsSubscriptionValidAsync(tenantId))
{
    Console.WriteLine("Subscription is active");
}
```

### GetTenantStatisticsAsync

```csharp
Task<TenantStatistics> GetTenantStatisticsAsync(Guid tenantId)
```

Retrieves statistics and metrics for a tenant.

**Parameters:**
- `tenantId` (Guid): Unique identifier of the tenant

**Returns:** `TenantStatistics` object containing:
- `UserCount` (int): Total number of users
- `OrganizationCount` (int): Total organizations
- `StorageUsedMb` (double): Total storage used
- `CreatedAt` (DateTime): Tenant creation date
- `LastActivityAt` (DateTime?): Last API activity

**Example:**
```csharp
var stats = await tenantService.GetTenantStatisticsAsync(tenantId);

Console.WriteLine($"Users: {stats.UserCount}");
Console.WriteLine($"Organizations: {stats.OrganizationCount}");
Console.WriteLine($"Storage: {stats.StorageUsedMb}MB");
```

---

## TenantResolutionService

Service for automatically detecting which tenant a request belongs to.

### ResolveTenantAsync

```csharp
Task<Tenant> ResolveTenantAsync()
```

Automatically resolves the current tenant from the HTTP request using cascading strategies.

**Returns:** `Tenant` object or `null`

**Resolution Order:**
1. `X-Tenant-Id` or `X-Tenant-Slug` HTTP header
2. User claims (`tenant_id`, `tenant_slug`)
3. Route parameters (`tenantId`, `slug`)
4. Subdomain extraction
5. Returns `null` if no strategy succeeds

**Throws:** `TenantNotResolvedException` if no tenant found

**Example:**
```csharp
var tenant = await tenantResolution.ResolveTenantAsync();

if (tenant == null)
{
    // Handle missing tenant
    return BadRequest("Tenant could not be resolved");
}

Console.WriteLine($"Resolved tenant: {tenant.Slug}");
```

### GetCurrentTenant

```csharp
Tenant GetCurrentTenant()
```

Retrieves the previously resolved tenant from HTTP context cache.

**Returns:** `Tenant` object or `null`

**Example:**
```csharp
var tenant = tenantResolution.GetCurrentTenant();
```

### GetCurrentTenantId

```csharp
Guid GetCurrentTenantId()
```

Retrieves the ID of the previously resolved tenant.

**Returns:** `Guid` (returns `Guid.Empty` if no tenant)

**Example:**
```csharp
var tenantId = tenantResolution.GetCurrentTenantId();

if (tenantId == Guid.Empty)
{
    return BadRequest("No tenant resolved");
}
```

### HasTenant

```csharp
bool HasTenant()
```

Checks if a tenant was successfully resolved for the current request.

**Returns:** `bool`

**Example:**
```csharp
if (!tenantResolution.HasTenant())
{
    return BadRequest("Tenant resolution failed");
}
```

---

## ConfigurationService

Service for managing per-tenant configuration with type-safe access and caching.

### SetConfigurationAsync

```csharp
Task<TenantConfiguration> SetConfigurationAsync(
    Guid tenantId,
    string key,
    string value,
    string valueType = "string",
    bool isEncrypted = false)
```

Sets a configuration value for a tenant.

**Parameters:**
- `tenantId` (Guid): Tenant to configure
- `key` (string): Configuration key (hierarchical with `:` delimiter)
- `value` (string): Configuration value
- `valueType` (string, optional): Type hint ("string", "int", "bool", "double")
- `isEncrypted` (bool, optional): Encrypt sensitive values

**Returns:** `TenantConfiguration` entity

**Example:**
```csharp
// Set string configuration
await configService.SetConfigurationAsync(
    tenantId, "email:sender", "noreply@example.com");

// Set numeric configuration
await configService.SetConfigurationAsync(
    tenantId, "api:rateLimit", "1000", valueType: "int");

// Set encrypted sensitive value
await configService.SetConfigurationAsync(
    tenantId, "stripe:apiKey", "sk_live_...", isEncrypted: true);
```

### GetConfigurationAsync

```csharp
Task<T> GetConfigurationAsync<T>(
    Guid tenantId,
    string key,
    T defaultValue = null)
```

Retrieves a strongly-typed configuration value with caching.

**Parameters:**
- `tenantId` (Guid): Tenant to retrieve configuration for
- `key` (string): Configuration key
- `defaultValue` (T, optional): Value to return if key not found

**Returns:** Configuration value or default (cached for 1 hour)

**Type Support:** string, int, bool, double, DateTime, Guid

**Example:**
```csharp
var rateLimit = await configService.GetConfigurationAsync<int>(
    tenantId, "api:rateLimit", defaultValue: 100);

var emailSender = await configService.GetConfigurationAsync<string>(
    tenantId, "email:sender", defaultValue: "admin@example.com");

var enableBeta = await configService.GetConfigurationAsync<bool>(
    tenantId, "features:beta:enabled", defaultValue: false);
```

### DeleteConfigurationAsync

```csharp
Task<bool> DeleteConfigurationAsync(Guid tenantId, string key)
```

Deletes a configuration entry.

**Parameters:**
- `tenantId` (Guid): Tenant
- `key` (string): Configuration key

**Returns:** `bool` - true if deleted, false if not found

**Example:**
```csharp
var deleted = await configService.DeleteConfigurationAsync(tenantId, "api:rateLimit");
```

### GetAllConfigurationsAsync

```csharp
Task<Dictionary<string, string>> GetAllConfigurationsAsync(Guid tenantId)
```

Retrieves all configuration values for a tenant.

**Parameters:**
- `tenantId` (Guid): Tenant

**Returns:** Dictionary of key-value pairs

**Example:**
```csharp
var allConfigs = await configService.GetAllConfigurationsAsync(tenantId);

foreach (var kvp in allConfigs)
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}
```

### ImportConfigurationAsync

```csharp
Task ImportConfigurationAsync(Guid tenantId, string jsonContent, bool overwrite = false)
```

Bulk-imports configuration from JSON.

**Parameters:**
- `tenantId` (Guid): Tenant
- `jsonContent` (string): JSON configuration object
- `overwrite` (bool, optional): Overwrite existing values

**Example:**
```csharp
var json = @"{
  ""api:rateLimit"": ""2000"",
  ""email:sender"": ""noreply@example.com"",
  ""features:beta:enabled"": ""true""
}";

await configService.ImportConfigurationAsync(tenantId, json, overwrite: true);
```

### ExportConfigurationAsync

```csharp
Task<string> ExportConfigurationAsync(Guid tenantId)
```

Exports all configuration as JSON for backup/migration.

**Parameters:**
- `tenantId` (Guid): Tenant

**Returns:** JSON string

**Example:**
```csharp
var json = await configService.ExportConfigurationAsync(tenantId);
System.IO.File.WriteAllText("tenant-config.json", json);
```

---

## TenantFeatureService

Service for managing feature toggles with rollout percentages and usage limits.

### IsFeatureEnabledAsync

```csharp
Task<bool> IsFeatureEnabledAsync(Guid tenantId, string featureKey)
```

Checks if a feature is enabled for a tenant, respecting rollout percentage.

**Parameters:**
- `tenantId` (Guid): Tenant
- `featureKey` (string): Feature identifier

**Returns:** `bool`

**Behavior:** If rollout is 50%, returns true ~50% of calls (probabilistic)

**Example:**
```csharp
if (await featureService.IsFeatureEnabledAsync(tenantId, "advanced-analytics"))
{
    // Show advanced analytics UI
}
```

### EnableFeatureAsync

```csharp
Task<TenantFeature> EnableFeatureAsync(Guid tenantId, string featureKey)
```

Explicitly enables a feature for a tenant (100% rollout).

**Parameters:**
- `tenantId` (Guid): Tenant
- `featureKey` (string): Feature identifier

**Returns:** `TenantFeature` entity

**Example:**
```csharp
await featureService.EnableFeatureAsync(tenantId, "experimental-ui");
```

### DisableFeatureAsync

```csharp
Task<TenantFeature> DisableFeatureAsync(Guid tenantId, string featureKey)
```

Explicitly disables a feature for a tenant.

**Parameters:**
- `tenantId` (Guid): Tenant
- `featureKey` (string): Feature identifier

**Returns:** `TenantFeature` entity

**Example:**
```csharp
await featureService.DisableFeatureAsync(tenantId, "legacy-ui");
```

### SetRolloutPercentageAsync

```csharp
Task<TenantFeature> SetRolloutPercentageAsync(
    Guid tenantId,
    string featureKey,
    int percentage)
```

Sets the rollout percentage (0-100) for gradual feature rollout.

**Parameters:**
- `tenantId` (Guid): Tenant
- `featureKey` (string): Feature identifier
- `percentage` (int): Rollout percentage (0-100)

**Returns:** `TenantFeature` entity

**Example:**
```csharp
// Gradually roll out to 25% of users
await featureService.SetRolloutPercentageAsync(
    tenantId, "new-dashboard", 25);

// Next week, increase to 50%
await featureService.SetRolloutPercentageAsync(
    tenantId, "new-dashboard", 50);

// Finally, 100% rollout
await featureService.SetRolloutPercentageAsync(
    tenantId, "new-dashboard", 100);
```

### RecordFeatureUsageAsync

```csharp
Task RecordFeatureUsageAsync(Guid tenantId, string featureKey)
```

Records that a feature was used by a tenant.

**Parameters:**
- `tenantId` (Guid): Tenant
- `featureKey` (string): Feature identifier

**Returns:** `Task` (no return value)

**Example:**
```csharp
// User clicked on analytics button
await featureService.RecordFeatureUsageAsync(tenantId, "analytics");

// Check if they exceeded limit
var feature = await featureService.GetFeatureAsync(tenantId, "analytics");
if (feature.UsageCount > feature.UsageLimit)
{
    return Forbid("Usage limit exceeded for this feature");
}
```

### GetStatisticsAsync

```csharp
Task<FeatureStatistics> GetStatisticsAsync(Guid tenantId)
```

Gets statistics for all features for a tenant.

**Parameters:**
- `tenantId` (Guid): Tenant

**Returns:** `FeatureStatistics` containing:
- `EnabledCount` (int): Number of enabled features
- `DisabledCount` (int): Number of disabled features
- `TotalUsage` (int): Total feature usage events
- `Features` (List<FeatureDetail>): Details per feature

**Example:**
```csharp
var stats = await featureService.GetStatisticsAsync(tenantId);

Console.WriteLine($"Enabled: {stats.EnabledCount}");
Console.WriteLine($"Disabled: {stats.DisabledCount}");
Console.WriteLine($"Total usage: {stats.TotalUsage}");
```

---

## DataIsolationService

Service for enforcing data isolation policies.

### CreatePolicyAsync

```csharp
Task<DataIsolationPolicy> CreatePolicyAsync(
    Guid tenantId,
    string entityType,
    DataIsolationPolicyType policyType,
    string allowedCrossTenantAccess = null)
```

Creates a data isolation policy for an entity type.

**Parameters:**
- `tenantId` (Guid): Tenant
- `entityType` (string): Entity class name ("User", "Document", etc.)
- `policyType` (enum): Strict, Relaxed, or Custom
- `allowedCrossTenantAccess` (string, optional): Comma-separated tenant IDs for Relaxed policies

**Returns:** `DataIsolationPolicy` entity

**Policy Types:**
- **Strict**: No cross-tenant access
- **Relaxed**: Allow explicit list of tenants
- **Custom**: Custom filter rules

**Example:**
```csharp
// Strict policy
var policy = await isolationService.CreatePolicyAsync(
    tenantId, "User", DataIsolationPolicyType.Strict);

// Relaxed policy allowing access from shared tenant
var policy = await isolationService.CreatePolicyAsync(
    tenantId, "ReferenceData", DataIsolationPolicyType.Relaxed,
    allowedCrossTenantAccess: "00000000-0000-0000-0000-000000000001");
```

### IsFieldAccessAllowedAsync

```csharp
Task<bool> IsFieldAccessAllowedAsync(
    Guid tenantId,
    string entityType,
    string fieldName)
```

Checks if field access is allowed by data isolation policy.

**Parameters:**
- `tenantId` (Guid): Tenant
- `entityType` (string): Entity class name
- `fieldName` (string): Property name

**Returns:** `bool`

**Example:**
```csharp
if (await isolationService.IsFieldAccessAllowedAsync(
    tenantId, "User", "Email"))
{
    var email = user.Email;
}
```

### CanAccessCrossTenantAsync

```csharp
Task<bool> CanAccessCrossTenantAsync(Guid tenantId, Guid targetTenantId)
```

Checks if cross-tenant access is allowed.

**Parameters:**
- `tenantId` (Guid): Requesting tenant
- `targetTenantId` (Guid): Target tenant

**Returns:** `bool`

**Example:**
```csharp
if (await isolationService.CanAccessCrossTenantAsync(tenantId, otherTenantId))
{
    // Retrieve cross-tenant data
}
```

---

## HTTP Status Codes

The framework returns standard HTTP status codes:

| Code | Meaning | When |
|------|---------|------|
| 200 | OK | Successful request |
| 201 | Created | Resource created |
| 204 | No Content | Successful with no body |
| 400 | Bad Request | Invalid parameters |
| 401 | Unauthorized | Authentication required |
| 403 | Forbidden | Access denied / Isolation policy violation |
| 404 | Not Found | Resource not found |
| 409 | Conflict | Duplicate slug, policy conflict |
| 500 | Server Error | Unexpected error |

---

## Exception Hierarchy

All framework exceptions inherit from `TenantIsolationException`:

- `TenantNotResolvedException` - Tenant not found in request
- `TenantNotActiveException` - Tenant is suspended/inactive
- `TenantConfigurationException` - Configuration error
- `DataIsolationViolationException` - Isolation policy violation
- `TenantDatabaseException` - Database operation failure

**Example:**
```csharp
try
{
    var tenant = await tenantService.GetActiveTenantAsync(tenantId);
}
catch (TenantNotActiveException ex)
{
    Console.WriteLine($"Error code: {ex.ErrorCode}");
    Console.WriteLine($"Tenant status: {ex.Status}");
}
catch (TenantIsolationException ex)
{
    Console.WriteLine($"Framework error: {ex.Message}");
}
```
