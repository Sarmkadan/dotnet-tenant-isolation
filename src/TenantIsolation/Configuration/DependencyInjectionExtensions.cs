#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
    /// <param name="services">The service collection</param>
    /// <param name="configureDbContext">Configuration action for DbContext options</param>
    /// <param name="configureOptions">Optional configuration action for TenantIsolationOptions</param>
    /// <exception cref="ArgumentNullException">Thrown if services or configureDbContext is null</exception>
    /// <returns>The configured service collection</returns>
    public static IServiceCollection AddTenantIsolation(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<TenantIsolationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        var options = new TenantIsolationOptions();
        configureOptions?.Invoke(options);

        // Register the base DbContextOptions for TenantDbContext.
        // This will be used by the TenantDbContextFactory.
        services.AddSingleton(sp =>
        {
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            configureDbContext(dbContextOptionsBuilder);
            return dbContextOptionsBuilder.Options;
        });

        // Register the tenant-aware DbContext factory
        services.TryAddScoped<ITenantDbContextFactory<TenantDbContext>, TenantDbContextFactory>();

        // Register TenantDbContext to be resolved from the factory for scoped lifetime
        services.AddScoped(sp => sp.GetRequiredService<ITenantDbContextFactory<TenantDbContext>>().Create());

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

        // Register memory cache if not already registered (this is handled by ServiceRegistrationExtensions now)
        // if (!services.Any(x => x.ServiceType == typeof(IMemoryCache)))
        //     services.AddMemoryCache();

        // Store options in services for later use
        services.AddSingleton(options);
        services.AddSingleton<IOptions<TenantIsolationOptions>>(Options.Create(options));

        return services;
    }

    /// <summary>
    /// Use tenant resolution middleware
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The configured application builder</returns>
    /// <exception cref="ArgumentNullException">Thrown if app is null</exception>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<TenantResolutionMiddleware>();
        return app;
    }

    /// <summary>
    /// Configure tenant isolation with in-memory database (for testing)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="databaseName">The in-memory database name</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The configured service collection</returns>
    /// <exception cref="ArgumentNullException">Thrown if services is null</exception>
    public static IServiceCollection AddTenantIsolationInMemory(
        this IServiceCollection services,
        string databaseName = "TenantIsolationDb",
        Action<TenantIsolationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddTenantIsolation(
            options => options.UseInMemoryDatabase(databaseName),
            configureOptions);
    }

    /// <summary>
    /// Configure tenant isolation with SQL Server
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The SQL Server connection string</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The configured service collection</returns>
    /// <exception cref="ArgumentNullException">Thrown if services or connectionString is null</exception>
    public static IServiceCollection AddTenantIsolationSqlServer(
        this IServiceCollection services,
        string connectionString,
        Action<TenantIsolationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        return services.AddTenantIsolation(
            options => options.UseSqlServer(connectionString),
            configureOptions);
    }

    /// <summary>
    /// Configure tenant isolation with PostgreSQL
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <exception cref="ArgumentNullException">Thrown if services or connectionString is null</exception>
    /// <exception cref="InvalidOperationException">Thrown if Npgsql provider is not referenced</exception>
    public static IServiceCollection AddTenantIsolationPostgres(
        this IServiceCollection services,
        string connectionString,
        Action<TenantIsolationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        throw new InvalidOperationException(
            "PostgreSQL support requires adding the Npgsql.EntityFrameworkCore.PostgreSQL package. " +
            "Add package reference: Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    /// <summary>
    /// Register tenant feature service
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The configured service collection</returns>
    /// <exception cref="ArgumentNullException">Thrown if services is null</exception>
    public static IServiceCollection AddTenantFeatureToggle(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

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

    /// <summary>
    /// Enable webhook support
    /// </summary>
    public bool EnableWebhooks { get; set; } = true;

    /// <summary>
    /// Enable caching layer
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Enable event bus
    /// </summary>
    public bool EnableEventBus { get; set; } = true;

    /// <summary>
    /// Enable background task processing
    /// </summary>
    public bool EnableBackgroundTasks { get; set; } = true;

    /// <summary>
    /// Enable notification service
    /// </summary>
    public bool EnableNotifications { get; set; } = true;

    /// <summary>
    /// Enable distributed tracing
    /// </summary>
    public bool EnableDistributedTracing { get; set; } = true;

    /// <summary>
    /// Enable health checks
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Enable external API client
    /// </summary>
    public bool EnableExternalApiClient { get; set; } = true;

    /// <summary>
    /// Cache expiration time in minutes
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum background task queue size
    /// </summary>
    public int MaxBackgroundTaskQueueSize { get; set; } = 1000;

    /// <summary>
    /// Interval in minutes for the dynamic tenant store to reload tenant data.
    /// Set to 0 or less to disable dynamic reloading.
    /// </summary>
    public int DynamicTenantStoreReloadIntervalMinutes { get; set; } = 5;
}
