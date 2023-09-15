#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TenantIsolation.BackgroundTasks;
using TenantIsolation.Caching;
using TenantIsolation.Configuration;
using TenantIsolation.Events;
using TenantIsolation.Formatters;
using TenantIsolation.Integration;
using TenantIsolation.Services;
using TenantIsolation.Utilities;

namespace TenantIsolation.Configuration;

/// <summary>
/// Configuration options for tenant isolation services
/// </summary>
public class TenantIsolationOptions
{
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
    /// Enable audit logging
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

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
}

/// <summary>
/// Comprehensive service registration extension for Phase 2 features
/// Simplifies setup by registering all framework services in one call
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Register all Phase 2 services with default options
    /// </summary>
    public static IServiceCollection AddTenantIsolationPhase2Services(this IServiceCollection services)
    {
        return AddTenantIsolationPhase2Services(services, _ => { });
    }

    /// <summary>
    /// Register all Phase 2 services with custom options
    /// </summary>
    public static IServiceCollection AddTenantIsolationPhase2Services(
        this IServiceCollection services,
        Action<TenantIsolationOptions> configureOptions)
    {
        var options = new TenantIsolationOptions();
        configureOptions(options);

        // Register configuration validation
        services.AddConfigurationValidator();

        // Register utilities
        RegisterUtilities(services);

        // Register middleware-related services
        RegisterMiddlewareServices(services);

        // Register caching services
        if (options.EnableCaching)
        {
            services.AddTenantAwareCachingService();
        }

        // Register event bus
        if (options.EnableEventBus)
        {
            services.AddEventBus();
        }

        // Register formatters
        services.AddResponseFormatter();

        // Register integration services
        if (options.EnableWebhooks)
        {
            services.AddScoped<IWebhookHandler, WebhookHandler>();
        }

        services.AddTenantIsolationHttpClientFactory();

        if (options.EnableExternalApiClient)
        {
            services.AddExternalApiClient();
        }

        // Register background tasks
        if (options.EnableBackgroundTasks)
        {
            services.AddBackgroundTaskQueue();
            services.AddHostedService<SubscriptionExpirationWorker>();
            services.AddHostedService<TenantCleanupWorker>();
        }

        // Register application services
        if (options.EnableAuditLogging)
        {
            services.AddAuditLogger();
        }

        if (options.EnableNotifications)
        {
            services.AddNotificationService();
        }

        if (options.EnableHealthChecks)
        {
            services.AddHealthCheckService();
        }

        services.AddExportService();

        // Register utilities
        services.AddTimeProvider();

        if (options.EnableDistributedTracing)
        {
            services.AddDistributedTracing();
        }

        return services;
    }

    /// <summary>
    /// Registers the appropriate cache provider (distributed or in-memory) based on
    /// whether IDistributedCache has been registered by the application.
    /// </summary>
    public static IServiceCollection AddTenantAwareCacheProvider(this IServiceCollection services)
    {
        // Check if IDistributedCache is already registered by the application.
        // If so, use our tenant-aware distributed cache provider.
        if (services.Any(s => s.ServiceType == typeof(IDistributedCache)))
        {
            services.TryAddSingleton<ITenantAwareDistributedCacheProvider, TenantAwareDistributedCacheProvider>();
            services.TryAddSingleton<ICacheProvider>(sp => sp.GetRequiredService<ITenantAwareDistributedCacheProvider>());
        }
        else
        {
            // Otherwise, fall back to the in-memory cache provider.
            services.TryAddSingleton<ICacheProvider, MemoryCacheProvider>();
        }

        return services;
    }

    /// <summary>
    /// Register utility services
    /// </summary>
    private static void RegisterUtilities(IServiceCollection services)
    {
        // All utilities are static extension methods, but we can register helpers if needed
        services.AddHttpContextAccessor();
    }

    /// <summary>
    /// Register middleware-related services
    /// </summary>
    private static void RegisterMiddlewareServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
    }

    /// <summary>
    /// Configure request pipeline with Phase 2 middleware
    /// </summary>
    public static IApplicationBuilder UseTenantIsolationPhase2Middleware(this IApplicationBuilder app)
    {
        // Error handling - should be first
        app.UseMiddleware<ErrorHandlingMiddleware>();

        // Request context and correlation IDs
        app.UseMiddleware<RequestContextMiddleware>();

        // Distributed tracing
        app.UseDistributedTracing();

        // Request logging
        app.UseMiddleware<RequestLoggingMiddleware>();

        // Rate limiting
        app.UseMiddleware<RateLimitingMiddleware>();

        // Tenant validation
        app.UseMiddleware<TenantResolutionMiddleware>();

        return app;
    }

    /// <summary>
    /// Log all registered Phase 2 services during startup
    /// </summary>
    public static IApplicationBuilder LogPhase2ServicesOnStartup(
        this IApplicationBuilder app,
        ILogger<IApplicationBuilder>? logger = null)
    {
        var appLogger = logger ?? new NullLogger<IApplicationBuilder>();

        appLogger.LogInformation("=== Phase 2 Services Registered ===");
        appLogger.LogInformation("✓ Configuration Validation");
        appLogger.LogInformation("✓ Utilities (String, DateTime, Validation, Crypto, JSON, Collections)");
        appLogger.LogInformation("✓ Middleware (Error Handling, Logging, Rate Limiting, Request Context)");
        appLogger.LogInformation("✓ Caching (In-Memory, Tenant-Aware)");
        appLogger.LogInformation("✓ Event Bus and Publishing");
        appLogger.LogInformation("✓ Formatters (Response, JSON, CSV, XML)");
        appLogger.LogInformation("✓ Webhook Handler");
        appLogger.LogInformation("✓ HTTP Client Factory");
        appLogger.LogInformation("✓ Background Tasks (Queue, Subscription Expiration, Cleanup)");
        appLogger.LogInformation("✓ Audit Logging");
        appLogger.LogInformation("✓ Notification Service");
        appLogger.LogInformation("✓ Health Checks");
        appLogger.LogInformation("✓ Export Service");
        appLogger.LogInformation("✓ External API Client");
        appLogger.LogInformation("✓ Time Provider");
        appLogger.LogInformation("✓ Distributed Tracing");
        appLogger.LogInformation("✓ Admin, Webhook, Analytics Controllers");
        appLogger.LogInformation("====================================");

        return app;
    }

    /// <summary>
    /// Null logger for startup logging
    /// </summary>
    private class NullLogger<T> : ILogger<T>
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }

        public bool IsEnabled(LogLevel logLevel) => false;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}

