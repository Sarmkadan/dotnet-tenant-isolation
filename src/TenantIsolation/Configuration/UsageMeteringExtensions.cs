// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Models;
using TenantIsolation.Services;

namespace TenantIsolation.Configuration;

/// <summary>
/// Configuration options for tenant usage metering and quota enforcement.
/// </summary>
public sealed class UsageMeteringOptions
{
    /// <summary>
    /// Percentage of quota consumption at which a warning is logged.
    /// Must be between 1 and 100. Defaults to <c>80</c>.
    /// </summary>
    public int WarningThresholdPercent { get; set; } = 80;

    /// <summary>
    /// Default billing period applied to new usage records when not specified explicitly.
    /// Defaults to <see cref="UsagePeriod.Monthly"/>.
    /// </summary>
    public UsagePeriod DefaultPeriod { get; set; } = UsagePeriod.Monthly;

    /// <summary>
    /// When <c>true</c>, calls to <see cref="ITenantUsageMeteringService.RecordUsageAsync"/> that
    /// push a tenant over their quota also throw immediately, combining recording and enforcement
    /// in a single step. Defaults to <c>false</c>.
    /// </summary>
    public bool ThrowOnQuotaExceeded { get; set; } = false;

    /// <summary>
    /// Maximum number of distinct metric keys tracked per tenant before older entries are pruned.
    /// Set to <c>0</c> to disable pruning. Defaults to <c>500</c>.
    /// </summary>
    public int MaxMetricsPerTenant { get; set; } = 500;
}

/// <summary>
/// <see cref="IServiceCollection"/> extension methods for registering tenant usage metering services.
/// </summary>
public static class UsageMeteringExtensions
{
    /// <summary>
    /// Registers <see cref="ITenantUsageMeteringService"/> with default options.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddTenantUsageMetering(this IServiceCollection services)
        => services.AddTenantUsageMetering(_ => { });

    /// <summary>
    /// Registers <see cref="ITenantUsageMeteringService"/> with configurable options.
    /// </summary>
    /// <remarks>
    /// The default implementation stores records in-process via a
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/> and is therefore
    /// registered as a <b>singleton</b>. For multi-node deployments swap the implementation for one
    /// backed by a distributed cache or a persistent store before production use.
    /// </remarks>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureOptions">Delegate to customise <see cref="UsageMeteringOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddTenantUsageMetering(
        this IServiceCollection services,
        Action<UsageMeteringOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new UsageMeteringOptions();
        configureOptions(options);

        ValidateOptions(options);

        services.AddSingleton(options);
        services.AddSingleton<ITenantUsageMeteringService, TenantUsageMeteringService>();

        return services;
    }

    /// <summary>
    /// Registers a custom <typeparamref name="TImplementation"/> as the
    /// <see cref="ITenantUsageMeteringService"/> singleton, using default options.
    /// </summary>
    /// <typeparam name="TImplementation">
    /// Concrete type that implements <see cref="ITenantUsageMeteringService"/>.
    /// </typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddTenantUsageMetering<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class, ITenantUsageMeteringService
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(new UsageMeteringOptions());
        services.AddSingleton<ITenantUsageMeteringService, TImplementation>();

        return services;
    }

    private static void ValidateOptions(UsageMeteringOptions options)
    {
        if (options.WarningThresholdPercent is < 1 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(UsageMeteringOptions.WarningThresholdPercent),
                options.WarningThresholdPercent,
                "Warning threshold must be between 1 and 100.");

        if (options.MaxMetricsPerTenant < 0)
            throw new ArgumentOutOfRangeException(
                nameof(UsageMeteringOptions.MaxMetricsPerTenant),
                options.MaxMetricsPerTenant,
                "MaxMetricsPerTenant cannot be negative.");
    }
}
