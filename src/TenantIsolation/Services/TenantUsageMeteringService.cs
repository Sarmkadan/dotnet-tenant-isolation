#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;
using TenantIsolation.Services;

namespace TenantIsolation.Services;

/// <summary>
/// Provides per-tenant usage metering and quota enforcement.
/// </summary>
public interface ITenantUsageMeteringService
{
    /// <summary>Record <paramref name="amount"/> units against <paramref name="metricKey"/> for the given tenant.</summary>
    Task<TenantUsageRecord> RecordUsageAsync(Guid tenantId, string metricKey, long amount = 1, CancellationToken cancellationToken = default);

    /// <summary>Retrieve the current usage snapshot for a single metric; returns <c>null</c> when never recorded.</summary>
    Task<TenantUsageRecord?> GetUsageAsync(Guid tenantId, string metricKey, CancellationToken cancellationToken = default);

    /// <summary>Check whether the tenant is within their allowed quota for <paramref name="metricKey"/>.</summary>
    Task<bool> IsWithinQuotaAsync(Guid tenantId, string metricKey, CancellationToken cancellationToken = default);

    /// <summary>Check whether the tenant is within their allowed quota for <paramref name="metricKey"/> against limits stored on TenantConfiguration.</summary>
    Task<QuotaCheckResult> CheckQuotaAsync(Guid tenantId, string metricKey, CancellationToken cancellationToken = default);

    /// <summary>Enforce the quota, throwing <see cref="TenantIsolationException"/> when the limit is exceeded.</summary>
    Task EnforceQuotaAsync(Guid tenantId, string metricKey, CancellationToken cancellationToken = default);

    /// <summary>Return all usage records for the tenant, ordered by metric key.</summary>
    Task<IReadOnlyList<TenantUsageRecord>> GetAllMetricsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Reset the usage counter for a single metric to zero.</summary>
    Task ResetUsageAsync(Guid tenantId, string metricKey, CancellationToken cancellationToken = default);

    /// <summary>Set or update the quota limit for a metric; pass <c>null</c> to remove the limit.</summary>
    Task SetQuotaAsync(Guid tenantId, string metricKey, long? quotaLimit, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-process implementation of <see cref="TenantUsageMeteringService"/> backed by a
/// thread-safe in-memory store. Suitable for single-node deployments; replace with a
/// distributed cache or database-backed implementation for multi-node scenarios.
/// </summary>
public sealed class TenantUsageMeteringService : ITenantUsageMeteringService
{
    private readonly ConcurrentDictionary<string, TenantUsageRecord> _store = new();
    private readonly ILogger<TenantUsageMeteringService> _logger;
    private readonly ConfigurationService _configurationService;

    /// <summary>Initialises a new instance of <see cref="TenantUsageMeteringService"/>.</summary>
    public TenantUsageMeteringService(ILogger<TenantUsageMeteringService> logger, ConfigurationService configurationService)
    {
        _logger = logger;
        _configurationService = configurationService;
    }

    /// <inheritdoc/>
    public Task<TenantUsageRecord> RecordUsageAsync(
        Guid tenantId, string metricKey, long amount = 1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (amount <= 0)
            throw new TenantIsolationException($"Usage amount must be positive (got {amount}).", "INVALID_USAGE_AMOUNT");

        var key = StoreKey(tenantId, metricKey);
        var record = _store.AddOrUpdate(
            key,
            _ => new TenantUsageRecord
            {
                TenantId = tenantId,
                MetricKey = metricKey,
                CurrentValue = amount,
                PeriodStart = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.CurrentValue += amount;
                existing.UpdatedAt = DateTime.UtcNow;
                return existing;
            }
        );

        _logger.LogInformation(
            "Usage recorded: tenant={TenantId} metric={MetricKey} delta={Amount} total={Total}",
            tenantId, metricKey, amount, record.CurrentValue);

        if (record.IsQuotaExceeded)
            _logger.LogWarning(
                "Quota exceeded: tenant={TenantId} metric={MetricKey} current={Current} limit={Limit}",
                tenantId, metricKey, record.CurrentValue, record.QuotaLimit);
        else if (record.IsApproachingLimit())
            _logger.LogWarning(
                "Approaching quota limit: tenant={TenantId} metric={MetricKey} usage={Pct:F1}%",
                tenantId, metricKey, record.UsagePercentage);

        return Task.FromResult(record);
    }

    /// <inheritdoc/>
    public Task<TenantUsageRecord?> GetUsageAsync(
        Guid tenantId, string metricKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.TryGetValue(StoreKey(tenantId, metricKey), out var record);
        return Task.FromResult(record);
    }

    /// <inheritdoc/>
    public async Task<bool> IsWithinQuotaAsync(Guid tenantId, string metricKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Get quota limit from TenantConfiguration
        var quotaLimit = await GetQuotaLimitFromConfigurationAsync(tenantId, metricKey, cancellationToken).ConfigureAwait(false);

        // If no quota is configured, tenant is always within quota
        if (!quotaLimit.HasValue)
            return true;

        // Get current usage
        var usageRecord = await GetUsageAsync(tenantId, metricKey, cancellationToken).ConfigureAwait(false);
        var currentUsage = usageRecord?.CurrentValue ?? 0;

        // Check if within quota
        return currentUsage < quotaLimit.Value;
    }

    /// <inheritdoc/>
    public Task<QuotaCheckResult> CheckQuotaAsync(
        Guid tenantId, string metricKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Get quota limit from TenantConfiguration
        var quotaLimitTask = GetQuotaLimitFromConfigurationAsync(tenantId, metricKey, cancellationToken);

        // Get current usage
        var usageTask = GetUsageAsync(tenantId, metricKey, cancellationToken);

        Task.WaitAll(quotaLimitTask, usageTask);
        var quotaLimit = quotaLimitTask.Result;
        var usageRecord = usageTask.Result;
        var currentUsage = usageRecord?.CurrentValue ?? 0;

        QuotaCheckResult result = quotaLimit.HasValue && currentUsage >= quotaLimit.Value
            ? QuotaCheckResult.Deny(metricKey, currentUsage, quotaLimit.Value)
            : QuotaCheckResult.Allow(metricKey, currentUsage, quotaLimit);

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public async Task EnforceQuotaAsync(
        Guid tenantId, string metricKey, CancellationToken cancellationToken = default)
    {
        var result = await CheckQuotaAsync(tenantId, metricKey, cancellationToken).ConfigureAwait(false);
        if (!result.IsAllowed)
            throw new TenantIsolationException(result.ViolationMessage!, "QUOTA_EXCEEDED",
                new Dictionary<string, object?> { ["TenantId"] = tenantId, ["MetricKey"] = metricKey,
                ["CurrentUsage"] = result.CurrentUsage, ["QuotaLimit"] = result.QuotaLimit });
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<TenantUsageRecord>> GetAllMetricsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var prefix = $"{tenantId}:";
        var records = _store
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(kv => kv.Value)
            .OrderBy(r => r.MetricKey)
            .ToList();
        return Task.FromResult<IReadOnlyList<TenantUsageRecord>>(records);
    }

    /// <inheritdoc/>
    public Task ResetUsageAsync(
        Guid tenantId, string metricKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_store.TryGetValue(StoreKey(tenantId, metricKey), out var record))
        {
            record.CurrentValue = 0;
            record.PeriodStart = DateTime.UtcNow;
            record.ResetAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Usage reset: tenant={TenantId} metric={MetricKey}", tenantId, metricKey);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetQuotaAsync(
        Guid tenantId, string metricKey, long? quotaLimit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = _store.GetOrAdd(StoreKey(tenantId, metricKey), _ => new TenantUsageRecord
        {
            TenantId = tenantId,
            MetricKey = metricKey,
            PeriodStart = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        record.QuotaLimit = quotaLimit;
        record.UpdatedAt = DateTime.UtcNow;
        _logger.LogInformation(
            "Quota configured: tenant={TenantId} metric={MetricKey} limit={Limit}",
            tenantId, metricKey, quotaLimit.HasValue ? quotaLimit.Value.ToString("N0") : "unlimited");
        return Task.CompletedTask;
    }

    private async Task<long?> GetQuotaLimitFromConfigurationAsync(Guid tenantId, string metricKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Try to get quota limit from TenantConfiguration
            var config = await _configurationService.GetConfigurationAsync(tenantId, $"quota:{metricKey}").ConfigureAwait(false);

            if (config != null && long.TryParse(config.Value, out var limit))
            {
                return limit;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read quota configuration for tenant {TenantId} metric {MetricKey}",
                tenantId, metricKey);
        }

        return null;
    }

    private static string StoreKey(Guid tenantId, string metricKey) =>
        $"{tenantId}:{metricKey}";
}