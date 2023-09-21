#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Configuration;
using TenantIsolation.Models;
using TenantIsolation.Services;

namespace TenantIsolation.Benchmarks;

/// <summary>
/// Benchmarks for feature toggle operations.
/// Measures the performance of feature toggle evaluation with different rollout strategies.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class FeatureToggleBenchmarks : IDisposable
{
    private ServiceProvider? _serviceProvider;
    private TenantFeatureService? _featureService;
    private TenantService? _tenantService;
    private IServiceScope? _scope;
    private Guid _tenantId;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Error));

        // Setup in-memory database with feature toggles
        services.AddTenantIsolationInMemory("FeatureBenchmarkDb", options =>
        {
            options.AutoMigrate = true;
            options.EnableAuditLogging = false;
        });

        services.AddTenantFeatureToggle();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();

        _tenantService = _scope.ServiceProvider.GetRequiredService<TenantService>();
        _tenantId = Guid.NewGuid();

        // Create a test tenant
        var tenant = _tenantService.CreateTenantAsync("Benchmark Tenant", "benchmark-tenant", "admin@test.com").Result;
        _tenantId = tenant.Id;

        _featureService = _scope.ServiceProvider.GetRequiredService<TenantFeatureService>();

        // Setup some features with different rollout percentages
        _featureService.EnableFeatureAsync(_tenantId, "new-dashboard").Wait();
        _featureService.SetRolloutPercentageAsync(_tenantId, "new-dashboard", 100).Wait();

        _featureService.EnableFeatureAsync(_tenantId, "experimental-api").Wait();
        _featureService.SetRolloutPercentageAsync(_tenantId, "experimental-api", 50).Wait();

        _featureService.EnableFeatureAsync(_tenantId, "beta-feature").Wait();
        _featureService.SetRolloutPercentageAsync(_tenantId, "beta-feature", 25).Wait();
    }

    /// <summary>
    /// Baseline: Check if feature is enabled with 100% rollout (cache hit).
    /// This is the most common scenario in production.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async ValueTask<bool> IsFeatureEnabled_100Percent()
    {
        return await _featureService!.IsFeatureEnabledAsync(_tenantId, "new-dashboard");
    }

    /// <summary>
    /// Check if feature is enabled with 50% rollout (cache hit).
    /// Tests probabilistic rollout behavior.
    /// </summary>
    [Benchmark]
    public async ValueTask<bool> IsFeatureEnabled_50Percent()
    {
        return await _featureService!.IsFeatureEnabledAsync(_tenantId, "experimental-api");
    }

    /// <summary>
    /// Check if feature is enabled with 25% rollout (cache hit).
    /// Tests low rollout percentage.
    /// </summary>
    [Benchmark]
    public async ValueTask<bool> IsFeatureEnabled_25Percent()
    {
        return await _featureService!.IsFeatureEnabledAsync(_tenantId, "beta-feature");
    }

    /// <summary>
    /// Enable a feature for a tenant (cache miss scenario).
    /// Tests the write path.
    /// </summary>
    [Benchmark]
    public async ValueTask EnableFeature()
    {
        await _featureService!.EnableFeatureAsync(_tenantId, "new-feature");
        await _featureService.IsFeatureEnabledAsync(_tenantId, "new-feature");
    }

    /// <summary>
    /// Set rollout percentage for a feature (cache miss scenario).
    /// Tests configuration updates.
    /// </summary>
    [Benchmark]
    public async ValueTask SetRolloutPercentage()
    {
        await _featureService!.SetRolloutPercentageAsync(_tenantId, "experimental-api", 75);
        await _featureService.IsFeatureEnabledAsync(_tenantId, "experimental-api");
    }

    /// <summary>
    /// Record feature usage (updates LastUsedAt timestamp).
    /// Tests the update path.
    /// </summary>
    [Benchmark]
    public async ValueTask RecordFeatureUsage()
    {
        await _featureService!.RecordFeatureUsageAsync(_tenantId, "new-dashboard");
        await _featureService.IsFeatureEnabledAsync(_tenantId, "new-dashboard");
    }

    /// <summary>
    /// Get statistics for all features of a tenant.
    /// Tests collection operations.
    /// </summary>
    [Benchmark]
    public async ValueTask<int> GetStatistics()
    {
        var stats = await _featureService!.GetStatisticsAsync(_tenantId);
        return (int)stats.GetType().GetProperty("TotalFeatures")?.GetValue(stats)!;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }

    public void Dispose()
    {
        Cleanup();
    }
}