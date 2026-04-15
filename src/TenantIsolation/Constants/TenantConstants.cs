#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace TenantIsolation.Constants;

/// <summary>
/// Core constants for tenant isolation framework
/// </summary>
public static class TenantConstants
{
    /// <summary>
    /// HTTP header name for tenant identifier
    /// </summary>
    public const string TenantIdHeader = "X-Tenant-Id";

    /// <summary>
    /// HTTP header name for tenant slug/subdomain
    /// </summary>
    public const string TenantSlugHeader = "X-Tenant-Slug";

    /// <summary>
    /// HTTP context item key for current tenant
    /// </summary>
    public const string CurrentTenantContextKey = "tenant:current";

    /// <summary>
    /// HTTP context item key for tenant configuration
    /// </summary>
    public const string TenantConfigContextKey = "tenant:config";

    /// <summary>
    /// Route parameter name for tenant identifier
    /// </summary>
    public const string TenantRouteParameter = "tenantId";

    /// <summary>
    /// Route parameter name for tenant slug
    /// </summary>
    public const string TenantSlugRouteParameter = "slug";

    /// <summary>
    /// Claim type for tenant identifier
    /// </summary>
    public const string TenantIdClaimType = "tenant_id";

    /// <summary>
    /// Claim type for tenant slug
    /// </summary>
    public const string TenantSlugClaimType = "tenant_slug";
}

/// <summary>
/// Tenant isolation strategy enumerations
/// </summary>
public enum TenantIsolationStrategy
{
    /// <summary>
    /// Database-per-tenant: Each tenant has dedicated database instance
    /// </summary>
    DatabasePerTenant = 1,

    /// <summary>
    /// Schema-per-tenant: Single database, separate schemas per tenant
    /// </summary>
    SchemaPerTenant = 2,

    /// <summary>
    /// Row-level security: Single database/schema, tenant identified by column
    /// </summary>
    RowLevelSecurity = 3,

    /// <summary>
    /// Hybrid: Combination of strategies
    /// </summary>
    Hybrid = 4
}

/// <summary>
/// Tenant status enumeration
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// Tenant is actively running
    /// </summary>
    Active = 1,

    /// <summary>
    /// Tenant is suspended (no access allowed)
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Tenant is in trial period
    /// </summary>
    Trial = 3,

    /// <summary>
    /// Tenant is inactive but can be reactivated
    /// </summary>
    Inactive = 4,

    /// <summary>
    /// Tenant has been archived
    /// </summary>
    Archived = 5,

    /// <summary>
    /// Tenant is being provisioned
    /// </summary>
    Provisioning = 6
}

/// <summary>
/// Data isolation policy types
/// </summary>
public enum DataIsolationPolicyType
{
    /// <summary>
    /// Strict isolation - no cross-tenant access
    /// </summary>
    Strict = 1,

    /// <summary>
    /// Relaxed isolation - specific cross-tenant access allowed
    /// </summary>
    Relaxed = 2,

    /// <summary>
    /// Custom isolation - application defined rules
    /// </summary>
    Custom = 3
}

/// <summary>
/// Configuration keys for tenant isolation
/// </summary>
public static class ConfigurationKeys
{
    public const string Section = "TenantIsolation";
    public const string DefaultStrategy = $"{Section}:DefaultStrategy";
    public const string EnabledStrategies = $"{Section}:EnabledStrategies";
    public const string MaxTenantsPerInstance = $"{Section}:MaxTenantsPerInstance";
    public const string EnableCaching = $"{Section}:Caching:Enabled";
    public const string CacheDuration = $"{Section}:Caching:Duration";
    public const string ConnectionStringTemplate = $"{Section}:ConnectionStrings:Template";
}

/// <summary>
/// Feature flag constants for tenant features
/// </summary>
public static class TenantFeatureFlags
{
    public const string MultiTenancy = "multi-tenancy";
    public const string AdvancedSecurity = "advanced-security";
    public const string CustomBranding = "custom-branding";
    public const string DataRetention = "data-retention";
    public const string ApiAccess = "api-access";
    public const string SsoIntegration = "sso-integration";
    public const string AuditLogging = "audit-logging";
}
