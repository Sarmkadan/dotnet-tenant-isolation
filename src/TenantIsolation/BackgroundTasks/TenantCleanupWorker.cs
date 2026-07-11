#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TenantIsolation.Data;

namespace TenantIsolation.BackgroundTasks;

/// <summary>
/// Background worker for cleaning up soft-deleted tenants and orphaned data
/// Permanently deletes old soft-deleted records after retention period
/// Prevents database bloat from deleted tenant data
/// </summary>
public class TenantCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantCleanupWorker> _logger;
    private readonly PeriodicTimer _timer;

    // Run cleanup weekly
    private static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromDays(1);

    // Retention period for soft-deleted records (30 days)
    private static readonly TimeSpan DefaultRetentionPeriod = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or sets the interval at which cleanup checks are performed
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = DefaultCheckInterval;

    /// <summary>
    /// Gets or sets the retention period for soft-deleted records before permanent deletion
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = DefaultRetentionPeriod;

    public TenantCleanupWorker(
        IServiceProvider serviceProvider,
        ILogger<TenantCleanupWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timer = new PeriodicTimer(CheckInterval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Tenant cleanup worker started");

        try
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken))
            {
                await PerformCleanupAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Tenant cleanup worker stopped");
        }
    }

    /// <summary>
    /// Perform cleanup operations
    /// </summary>
    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

            _logger.LogInformation("Starting tenant cleanup operation");

            var cutoffDate = DateTime.UtcNow.Subtract(RetentionPeriod);
            var deletedCount = 0;

            // Hard delete soft-deleted tenants older than retention period
            try
            {
                var tenantsToDelete = await dbContext.Tenants
                    .Where(t => t.IsDeleted && t.UpdatedAt < cutoffDate)
                    .ToListAsync(cancellationToken);

                if (tenantsToDelete.Any())
                {
                    dbContext.Tenants.RemoveRange(tenantsToDelete);
                    deletedCount = await dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Permanently deleted {Count} old tenant records", deletedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expired tenant records");
            }

            // Clean up orphaned users without valid tenants
            try
            {
                var orphanedUsersQuery = @"
                    DELETE FROM [User] u
                    WHERE NOT EXISTS (
                        SELECT 1 FROM [Tenant] t WHERE t.Id = u.TenantId
                    )
                    AND u.UpdatedAt < @CutoffDate
                ";

                var orphanedUserCount = await dbContext.Database
                    .ExecuteSqlRawAsync(orphanedUsersQuery,
                        new Microsoft.Data.SqlClient.SqlParameter("@CutoffDate", cutoffDate),
                        cancellationToken);

                if (orphanedUserCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} orphaned user records", orphanedUserCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up orphaned users");
            }

            // Rebuild statistics
            try
            {
                await RebuildStatisticsAsync(dbContext, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebuilding statistics");
            }

            _logger.LogInformation("Tenant cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tenant cleanup operation");
        }
    }

    /// <summary>
    /// Rebuild database statistics after cleanup
    /// </summary>
    private async Task RebuildStatisticsAsync(TenantDbContext dbContext, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Rebuilding database statistics");

        // This is a simplified example; in production you might want to call SQL DBCC DBREINDEX
        try
        {
            var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "sp_updatestats";
            command.CommandType = System.Data.CommandType.StoredProcedure;

            if (command.Connection?.State != System.Data.ConnectionState.Open)
                await command.Connection?.OpenAsync(cancellationToken)!;

            await command.ExecuteNonQueryAsync(cancellationToken);
            await command.Connection?.CloseAsync()!;

            _logger.LogDebug("Database statistics updated");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not update statistics (may not be supported by database)");
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
/// Extension methods for TenantCleanupWorker providing additional configuration and utility methods
/// </summary>
public static class TenantCleanupWorkerExtensions
{
    /// <summary>
    /// Configures the tenant cleanup worker with custom interval and retention period
    /// </summary>
    /// <param name="builder">The host builder</param>
    /// <param name="checkInterval">How often to run the cleanup check</param>
    /// <param name="retentionPeriod">How long to keep soft-deleted records before permanent deletion</param>
    /// <returns>The host builder for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown if builder is null</exception>
    public static IHostBuilder AddTenantCleanupWorker(
        this IHostBuilder builder,
        TimeSpan checkInterval,
        TimeSpan retentionPeriod)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices((context, services) =>
        {
            services.AddHostedService<TenantCleanupWorker>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<TenantCleanupWorker>>();
                return new TenantCleanupWorker(provider, logger)
                {
                    CheckInterval = checkInterval,
                    RetentionPeriod = retentionPeriod
                };
            });
        });
    }

    /// <summary>
    /// Configures the tenant cleanup worker with custom retention period but default interval
    /// </summary>
    /// <param name="builder">The host builder</param>
    /// <param name="retentionPeriod">How long to keep soft-deleted records before permanent deletion</param>
    /// <returns>The host builder for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown if builder is null</exception>
    public static IHostBuilder AddTenantCleanupWorker(
        this IHostBuilder builder,
        TimeSpan retentionPeriod)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddTenantCleanupWorker(TimeSpan.FromDays(1), retentionPeriod);
    }

    /// <summary>
    /// Gets the current retention period from the TenantCleanupWorker instance
    /// </summary>
    /// <param name="worker">The tenant cleanup worker instance</param>
    /// <returns>The retention period</returns>
    /// <exception cref="ArgumentNullException">Thrown if worker is null</exception>
    public static TimeSpan GetRetentionPeriod(this TenantCleanupWorker worker)
    {
        ArgumentNullException.ThrowIfNull(worker);

        return worker.RetentionPeriod;
    }

    /// <summary>
    /// Gets the current check interval from the TenantCleanupWorker instance
    /// </summary>
    /// <param name="worker">The tenant cleanup worker instance</param>
    /// <returns>The check interval</returns>
    /// <exception cref="ArgumentNullException">Thrown if worker is null</exception>
    public static TimeSpan GetCheckInterval(this TenantCleanupWorker worker)
    {
        ArgumentNullException.ThrowIfNull(worker);

        return worker.CheckInterval;
    }
}
