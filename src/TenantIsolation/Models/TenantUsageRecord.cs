#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;

namespace TenantIsolation.Models;

/// <summary>
/// Billing period over which usage is accumulated before the counter resets
/// </summary>
public enum UsagePeriod
{
    /// <summary>Resets every hour</summary>
    Hourly,
    /// <summary>Resets every calendar day (UTC)</summary>
    Daily,
    /// <summary>Resets on the first day of each calendar month (UTC)</summary>
    Monthly,
    /// <summary>Resets on 1 January each year (UTC)</summary>
    Yearly,
    /// <summary>Never resets; accumulates for the lifetime of the tenant</summary>
    Lifetime
}

/// <summary>
/// Tracks accumulated usage for a single named metric scoped to one tenant.
/// </summary>
public class TenantUsageRecord
{
    /// <summary>Unique identifier</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Owning tenant identifier</summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>Metric name, e.g. "api_calls", "storage_gb", "active_users"</summary>
    [Required]
    [StringLength(100)]
    public string MetricKey { get; set; } = null!;

    /// <summary>Accumulated value for the current period</summary>
    public long CurrentValue { get; set; }

    /// <summary>Maximum allowed value; <c>null</c> means unlimited</summary>
    public long? QuotaLimit { get; set; }

    /// <summary>Granularity at which the counter resets</summary>
    public UsagePeriod Period { get; set; } = UsagePeriod.Monthly;

    /// <summary>UTC start of the current billing period</summary>
    public DateTime PeriodStart { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the most recent reset; <c>null</c> if never reset</summary>
    public DateTime? ResetAt { get; set; }

    /// <summary>When this record was first created</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When this record was last modified</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Percentage of quota consumed, clamped to [0, 100].
    /// Returns <c>0</c> when no limit is configured.
    /// </summary>
    public double UsagePercentage =>
        QuotaLimit.HasValue && QuotaLimit.Value > 0
            ? Math.Min(100.0, (double)CurrentValue / QuotaLimit.Value * 100.0)
            : 0.0;

    /// <summary>Returns <c>true</c> when <see cref="CurrentValue"/> has reached or exceeded <see cref="QuotaLimit"/>.</summary>
    public bool IsQuotaExceeded => QuotaLimit.HasValue && CurrentValue >= QuotaLimit.Value;

    /// <summary>
    /// Returns <c>true</c> when <see cref="UsagePercentage"/> is at or above <paramref name="thresholdPercent"/>.
    /// </summary>
    public bool IsApproachingLimit(int thresholdPercent = 80) =>
        QuotaLimit.HasValue && UsagePercentage >= thresholdPercent;
}

/// <summary>
/// Immutable result returned by a quota check operation.
/// </summary>
public sealed class QuotaCheckResult
{
    /// <summary>Whether the operation is permitted to proceed</summary>
    public bool IsAllowed { get; init; }

    /// <summary>Whether the quota limit has been reached or exceeded</summary>
    public bool IsExceeded { get; init; }

    /// <summary>Current consumption as a percentage of the limit (0–100)</summary>
    public double UsagePercentage { get; init; }

    /// <summary>Absolute number of units consumed in the current period</summary>
    public long CurrentUsage { get; init; }

    /// <summary>Configured limit; <c>null</c> when unlimited</summary>
    public long? QuotaLimit { get; init; }

    /// <summary>Metric that was evaluated</summary>
    public string MetricKey { get; init; } = null!;

    /// <summary>Human-readable explanation when <see cref="IsAllowed"/> is <c>false</c></summary>
    public string? ViolationMessage { get; init; }

    /// <summary>Creates an allowed result</summary>
    public static QuotaCheckResult Allow(string metricKey, long current, long? limit) => new()
    {
        IsAllowed = true,
        IsExceeded = false,
        CurrentUsage = current,
        QuotaLimit = limit,
        MetricKey = metricKey,
        UsagePercentage = limit.HasValue && limit.Value > 0
            ? Math.Min(100.0, (double)current / limit.Value * 100.0) : 0.0
    };

    /// <summary>Creates a denied result due to quota violation</summary>
    public static QuotaCheckResult Deny(string metricKey, long current, long limit) => new()
    {
        IsAllowed = false,
        IsExceeded = true,
        CurrentUsage = current,
        QuotaLimit = limit,
        MetricKey = metricKey,
        UsagePercentage = 100.0,
        ViolationMessage = $"Quota limit of {limit:N0} has been reached for metric '{metricKey}' (current: {current:N0})"
    };
}
