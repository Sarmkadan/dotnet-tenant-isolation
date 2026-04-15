#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Data;
using TenantIsolation.Middleware;
using TenantIsolation.Services;

namespace TenantIsolation.Configuration;

/// <summary>
/// Extension methods for registering tenant isolation services
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Register tenant isolation services with DbContext
    /// </summary>
    public static IServiceCollection AddTenantIsolation(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<TenantIsolationOptions>? configureOptions = null)
    {
        var options = new TenantIsolationOptions();
        configureOptions?.Invoke(options);

        // Register DbContext
        services.AddDbContext<TenantDbContext>(configureDbContext);

        // Register repositories
        services.AddScoped<TenantRepository>();
        services.AddScoped<UserRepository>();
        services.AddScoped<OrganizationRepository>();

        // Register services
        services.AddScoped<TenantService>();
        services.AddScoped<TenantResolutionService>();
        services.AddScoped<DataIsolationService>();
        services.AddScoped<ConfigurationService>();

        // Register HTTP context accessor
        services.AddHttpContextAccessor();

        // Register memory cache if not already registered
        if (!services.Any(x => x.ServiceType == typeof(IMemoryCache)))
            services.AddMemoryCache();

        // Store options in services for later use
        services.AddSingleton(options);

        return services;
    }

    /// <summary>
    /// Use tenant resolution middleware
    /// </summary>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantResolutionMiddleware>();
        return app;
    }

    /// <summary>
    /// Configure tenant isolation with in-memory database (for testing)
    /// </summary>
    public static IServiceCollection AddTenantIsolationInMemory(
        this IServiceCollection services,
        string databaseName = "TenantIsolationDb",
        Action<TenantIsolationOptions>? configureOptions = null)
    {
        return services.AddTenantIsolation(
            options => options.UseInMemoryDatabase(databaseName),
            configureOptions);
    }

    /// <summary>
    /// Configure tenant isolation with SQL Server
    /// </summary>
    public static IServiceCollection AddTenantIsolationSqlServer(
        this IServiceCollection services,
        string connectionString,
        Action<TenantIsolationOptions>? configureOptions = null)
    {
        return services.AddTenantIsolation(
            options => options.UseSqlServer(connectionString),
            configureOptions);
    }

    /// <summary>
    /// Configure tenant isolation with PostgreSQL
    /// </summary>
    public static IServiceCollection AddTenantIsolationPostgres(
        this IServiceCollection services,
        string connectionString,
        Action<TenantIsolationOptions>? configureOptions = null)
    {
        return services.AddTenantIsolation(
            options => options.UseNpgsql(connectionString),
            configureOptions);
    }

    /// <summary>
    /// Register tenant feature service
    /// </summary>
    public static IServiceCollection AddTenantFeatureToggle(
        this IServiceCollection services)
    {
        services.AddScoped<TenantFeatureService>();
        return services;
    }
}

/// <summary>
/// Configuration options for tenant isolation framework
/// </summary>
public class TenantIsolationOptions
{
    /// <summary>
    /// Enable automatic database migration on startup
    /// </summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent tenant contexts
    /// </summary>
    public int MaxConcurrentTenants { get; set; } = 1000;

    /// <summary>
    /// Enable query filtering for soft deletes
    /// </summary>
    public bool EnableSoftDeleteFilter { get; set; } = true;

    /// <summary>
    /// Enable tenant isolation audit logging
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Default tenant isolation strategy
    /// </summary>
    public string DefaultIsolationStrategy { get; set; } = "DatabasePerTenant";

    /// <summary>
    /// Configuration cache duration in minutes
    /// </summary>
    public int ConfigurationCacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to validate tenant on every request
    /// </summary>
    public bool ValidateTenantOnEveryRequest { get; set; } = true;

    /// <summary>
    /// Paths to exclude from tenant resolution
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/api/health",
        "/.well-known"
    };
}
