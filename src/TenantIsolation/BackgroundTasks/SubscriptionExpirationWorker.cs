// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TenantIsolation.Services;

namespace TenantIsolation.BackgroundTasks;

/// <summary>
/// Background worker for checking tenant subscription expirations
/// Sends notifications and automatically suspends expired subscriptions
/// Runs periodically to ensure timely notifications and enforcement
/// </summary>
public class SubscriptionExpirationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionExpirationWorker> _logger;
    private readonly PeriodicTimer _timer;

    // Check every 6 hours
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    public SubscriptionExpirationWorker(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionExpirationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timer = new PeriodicTimer(CheckInterval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription expiration worker started");

        // Run immediately, then on interval
        await CheckSubscriptionsAsync(stoppingToken);

        try
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken))
            {
                await CheckSubscriptionsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Subscription expiration worker stopped");
        }
    }

    /// <summary>
    /// Check for expiring subscriptions and take action
    /// </summary>
    private async Task CheckSubscriptionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var tenantService = scope.ServiceProvider.GetRequiredService<TenantService>();

            _logger.LogInformation("Checking for expiring subscriptions");

            // Get subscriptions expiring within 30 days
            var expiringTenants = await tenantService.GetExpiringSubscriptionsAsync(30);
            if (expiringTenants.Count == 0)
            {
                _logger.LogDebug("No expiring subscriptions found");
                return;
            }

            _logger.LogInformation("Found {Count} tenants with expiring subscriptions", expiringTenants.Count);

            // Get subscriptions that have already expired
            var expiredTenants = await tenantService.GetExpiringSubscriptionsAsync(0);

            // Handle expired subscriptions
            foreach (var tenant in expiredTenants)
            {
                try
                {
                    _logger.LogWarning("Subscription expired for tenant {TenantId}. Suspending...",
                        tenant.Id);

                    // Suspend the tenant due to expired subscription
                    await tenantService.SuspendTenantAsync(tenant.Id, "Subscription expired");

                    _logger.LogInformation("Suspended tenant {TenantId} due to expired subscription",
                        tenant.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error suspending tenant {TenantId} with expired subscription",
                        tenant.Id);
                }
            }

            // Log warning for soon-to-expire subscriptions (for notification purposes)
            var soonToExpire = expiringTenants
                .Where(t => !expiredTenants.Contains(t))
                .ToList();

            if (soonToExpire.Count > 0)
            {
                _logger.LogWarning("Found {Count} tenants with subscriptions expiring soon",
                    soonToExpire.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in subscription expiration check");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Extension method to register subscription expiration worker
/// </summary>
public static class SubscriptionExpirationWorkerExtensions
{
    public static IHostBuilder AddSubscriptionExpirationWorker(this IHostBuilder builder)
    {
        return builder.ConfigureServices((context, services) =>
        {
            services.AddHostedService<SubscriptionExpirationWorker>();
        });
    }
}
