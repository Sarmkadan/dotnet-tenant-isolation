# Tenant cleanup worker

`TenantCleanupWorker` is a hosted background service that periodically attempts to permanently remove expired tenant data. It is implemented in `src/TenantIsolation/BackgroundTasks/TenantCleanupWorker.cs` and resolves a new scoped `TenantDbContext` for every cleanup pass.

## Schedule and retention

- The worker starts with the application and waits for the first timer tick; it does not run cleanup immediately at startup.
- The timer's default interval is one day. Each later pass starts on another timer tick, so a slow pass is not run concurrently with itself.
- The default retention period is 30 days. At the start of a pass, the cutoff is calculated as `DateTime.UtcNow - RetentionPeriod`.
- A record is eligible only when its `UpdatedAt` value is strictly earlier than the cutoff. A value exactly equal to the cutoff is not eligible.
- Host cancellation stops the wait or the current pass. The timer is disposed during shutdown and disposal.

The `AddTenantCleanupWorker` overloads expose a retention period and, optionally, a check interval. The retention value is applied to cleanup. In the current implementation, however, the `PeriodicTimer` is constructed in the worker constructor before the object initializer assigns `CheckInterval`. Consequently, changing `CheckInterval` through this extension changes the reported property but not the active timer: the effective interval remains one day.

The `AddTenantIsolationPhase2Services` registration adds this worker only when `EnableBackgroundTasks` is enabled. Applications should not also register it separately unless they intentionally want multiple cleanup workers.

## Data purged by each pass

The pass performs these operations in order:

1. **Expired soft-deleted tenants.** It queries for tenants where `IsDeleted` is `true` and `UpdatedAt` is older than the cutoff. Each selected tenant is hard-deleted and saved separately. Database-configured cascading relationships can also delete that tenant's configurations, connection strings, organizations (and their users), isolation policies, features, and usage records. The direct tenant-to-user relationship uses restrictive deletion, so remaining direct user rows can prevent a tenant deletion.
2. **Old orphaned users.** It executes SQL that permanently deletes rows from `[User]` when no `[Tenant]` row has the same `TenantId` and the user's `UpdatedAt` is older than the same cutoff. This condition does not require the user to be soft-deleted.
3. **Database statistics.** It invokes the `sp_updatestats` stored procedure. This is maintenance rather than data purging and is provider-specific.

No cleanup is performed here for old audit logs, notifications, subscriptions, or merely inactive tenants.

### Soft-delete query-filter limitation

`TenantDbContext` applies a global tenant filter of `!IsDeleted`. The worker's tenant query does not call `IgnoreQueryFilters()`. With that context configuration active, soft-deleted tenants are filtered out before the worker's `IsDeleted` predicate is evaluated, so the first operation normally selects no tenants. The orphaned-user SQL is raw SQL and is not affected by EF Core query filters.

This documents the current behavior; operators should not assume that the retention period guarantees tenant erasure without verifying the resulting database records.

## Failure handling and logging

Tenant selection, orphan cleanup, and statistics maintenance have separate exception handling. A failure in one stage is logged and the pass continues to the next stage. Tenant deletion is also isolated per tenant, so one failed deletion does not stop attempts for later selected tenants. Unexpected pass-level errors are logged and the worker waits for the next tick. Cancellation is logged and stops the service.

The raw orphan cleanup statement and `sp_updatestats` use SQL Server syntax. On another database provider those stages may fail; the worker logs the errors and continues.

## Registration examples

Enable the default hosted-worker registration through the library options:

```csharp
services.AddTenantIsolationPhase2Services(options =>
{
    options.EnableBackgroundTasks = true;
});
```

Alternatively, when configuring an `IHostBuilder`, register the worker with an explicit retention period:

```csharp
hostBuilder.AddTenantCleanupWorker(retentionPeriod: TimeSpan.FromDays(60));
```

The second form still uses an effective one-day timer interval, as described above.
