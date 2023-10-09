# ITenantUsageMeteringService

The `ITenantUsageMeteringService` interface defines operations for tracking tenant-specific usage metrics, enforcing quota limits, and managing quota configurations. It is intended for use in multi‑tenant applications where consumption of resources must be measured, reported, and governed per tenant.

## API

### TenantUsageMeteringService
**Purpose:** Initializes a new instance of the metering service.  
**Parameters:** None (instances are typically supplied via dependency injection).  
**Return:** A new `ITenantUsageMeteringService` implementation.  
**Throws:** May throw if required dependencies cannot be resolved during construction.

### RecordUsageAsync
**Purpose:** Records a usage metric for a given tenant.  
**Parameters:** Tenant identifier, metric name, amount to record, and optional timestamp (as defined by the implementing class).  
**Return:** A `Task<TenantUsageRecord>` that completes with the recorded usage entry.  
**Throws:**  
- `ArgumentException` if the tenant identifier or metric name is null or empty.  
- `ArgumentOutOfRangeException` if the amount is negative.  
- `IOException` or derived exceptions if persisting the record fails.

### GetUsageAsync
**Purpose:** Retrieves the most recent usage record for a tenant and metric.  
**Parameters:** Tenant identifier and metric name.  
**Return:** A `Task<TenantUsageRecord?>` that completes with the usage record if one exists, otherwise `null`.  
**Throws:**  
- `ArgumentException` if the tenant identifier or metric name is null or empty.  
- `IOException` if reading the record fails.

### CheckQuotaAsync
**Purpose:** Determines whether a proposed usage amount would exceed the tenant's quota.  
**Parameters:** Tenant identifier, metric name, and the amount to check.  
**Return:** A `Task<QuotaCheckResult>` indicating whether the operation is allowed and the remaining quota.  
**Throws:**  
- `ArgumentException` if the tenant identifier or metric name is null or empty.  
- `ArgumentOutOfRangeException` if the amount is negative.

### EnforceQuotaAsync
**Purpose:** Verifies that a usage operation complies with the tenant's quota, throwing if it would exceed the limit.  
**Parameters:** Tenant identifier, metric name, and the amount to enforce.  
**Return:** A `Task` that completes successfully when the operation is within quota.  
**Throws:**  
- `ArgumentException` if the tenant identifier or metric name is null or empty.  
- `ArgumentOutOfRangeException` if the amount is negative.  
- `QuotaExceededException` (or equivalent) if the operation would exceed the configured quota.

### GetAllMetricsAsync
**Purpose:** Retrieves all usage records for a given tenant.  
**Parameters:** Tenant identifier.  
**Return:** A `Task<IReadOnlyList<TenantUsageRecord>>` containing the complete set of metrics for the tenant.  
**Throws:**  
- `ArgumentException` if the tenant identifier is null or empty.  
- `IOException` if reading the metrics fails.

### ResetUsageAsync
**Purpose:** Clears the recorded usage for a tenant and metric, resetting counters to zero.  
**Parameters:** Tenant identifier and metric name.  
**Return:** A `Task` that completes when the reset operation has been persisted.  
**Throws:**  
- `ArgumentException` if the tenant identifier or metric name is null or empty.  
- `IOException` if the reset cannot be persisted.

### SetQuotaAsync
**Purpose:** Assigns or updates the quota limit for a tenant and metric.  
**Parameters:** Tenant identifier, metric name, and the quota limit to apply.  
**Return:** A `Task` that completes when the quota has been stored.  
**Throws:**  
- `ArgumentException` if the tenant identifier or metric name is null or empty.  
- `ArgumentOutOfRangeException` if the quota limit is negative.

## Usage

### Example 1: Recording usage and enforcing quota
```csharp
using System.Threading.Tasks;

public class UsageController
{
    private readonly ITenantUsageMeteringService _metering;

    public UsageController(ITenantUsageMeteringService metering)
    {
        _metering = metering;
    }

    public async Task ProcessRequestAsync(string tenantId, string metric, double amount)
    {
        // Ensure the operation does not exceed quota
        await _metering.EnforceQuotaAsync(tenantId, metric, amount);

        // Record the actual usage
        await _metering.RecordUsageAsync(tenantId, metric, amount);
    }
}
```

### Example 2: Retrieving metrics and resetting usage
```csharp
using System.Threading.Tasks;

public class ReportingService
{
    private readonly ITenantUsageMeteringService _metering;

    public ReportingService(ITenantUsageMeteringService metering)
    {
        _metering = metering;
    }

    public async Task<double> GetTotalUsageAsync(string tenantId, string metric)
    {
        var records = await _metering.GetAllMetricsAsync(tenantId);
        double total = 0;
        foreach (var r in records)
        {
            if (r.MetricName == metric)
                total += r.Amount;
        }
        return total;
    }

    public async Task ResetTenantMetricAsync(string tenantId, string metric)
    {
        await _metering.ResetUsageAsync(tenantId, metric);
    }
}
```

## Notes
- All methods are asynchronous; callers should `await` them to avoid blocking threads.  
- The interface itself is stateless; thread‑safety depends on the concrete implementation. Implementations that store state internally must ensure concurrent access is properly synchronized (e.g., using locks or concurrent collections).  
- Passing `null` or empty strings for tenant identifiers or metric names will result in an `ArgumentException`.  
- Negative values for usage amounts or quota limits are invalid and will trigger an `ArgumentOutOfRangeException`.  
- `GetUsageAsync` may return `null` when no usage has been recorded for the requested tenant/metric; callers must handle this case.  
- `EnforceQuotaAsync` throws a domain‑specific exception (e.g., `QuotaExceededException`) when the requested amount would exceed the quota; callers should catch this exception to implement retry or fallback logic.  
- `ResetUsageAsync` removes all previously recorded usage for the specified tenant/metric; subsequent calls to `GetUsageAsync` will return `null` until new usage is recorded.  
- `SetQuotaAsync` overwrites any existing quota for the tenant/metric; setting the quota to zero effectively disallows any further usage until a new quota is applied.
