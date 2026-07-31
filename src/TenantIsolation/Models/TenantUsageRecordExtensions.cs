#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TenantIsolation.Models;

/// <summary>
/// Extension methods for aggregating <see cref="TenantUsageRecord"/> collections.
/// </summary>
public static class TenantUsageRecordExtensions
{
    /// <summary>
    /// Calculates the total <see cref="TenantUsageRecord.CurrentValue"/> for a given metric key
    /// across the supplied collection.
    /// </summary>
    /// <param name="records">The collection of usage records.</param>
    /// <param name="metricKey">The metric key to sum values for.</param>
    /// <returns>The sum of <c>CurrentValue</c> for matching records, or <c>0</c> if none match.</returns>
    public static long TotalByMetric(this IEnumerable<TenantUsageRecord> records, string metricKey)
    {
        if (records is null) throw new ArgumentNullException(nameof(records));
        if (metricKey is null) throw new ArgumentNullException(nameof(metricKey));

        return records
            .Where(r => string.Equals(r.MetricKey, metricKey, StringComparison.OrdinalIgnoreCase))
            .Sum(r => r.CurrentValue);
    }

    /// <summary>
    /// Groups usage records by the UTC calendar day of their <see cref="TenantUsageRecord.PeriodStart"/>.
    /// The key of each group is the date component (midnight UTC) of <c>PeriodStart</c>.
    /// </summary>
    /// <param name="records">The collection of usage records.</param>
    /// <returns>
    /// An <see cref="IEnumerable{IGrouping{DateTime,TenantUsageRecord}}"/> where each grouping
    /// represents a single UTC day.
    /// </returns>
    public static IEnumerable<IGrouping<DateTime, TenantUsageRecord>> GroupByDay(this IEnumerable<TenantUsageRecord> records)
    {
        if (records is null) throw new ArgumentNullException(nameof(records));

        return records.GroupBy(r => r.PeriodStart.Date);
    }
}
