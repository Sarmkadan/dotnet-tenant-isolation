# ITimeProvider

Provides a controllable clock and tenant‑aware time conversion utilities for the `dotnet-tenant-isolation` library. The interface allows tests and application code to deterministically set, advance, or reset the current UTC time, while static helpers translate times to and from tenant‑specific zones, evaluate business hours, and compute SLA deadlines.

## API

### SetCurrentTime(DateTime utcTime)
Sets the internal clock to the supplied UTC instant.  
- **utcTime** – The UTC `DateTime` to use as the current time.  
- **Returns** – `void`.  
- **Throws** – `ArgumentOutOfRangeException` if `utcTime` is earlier than `DateTime.MinValue` or later than the provider’s maximum supported date.

### AdvanceTime(TimeSpan offset)
Shifts the internal clock by the given offset.  
- **offset** – A `TimeSpan` to add to the current time (may be negative to move backwards).  
- **Returns** – `void`.  
- **Throws** – `ArgumentException` if applying the offset would result in a time outside the supported range.

### Reset()
Restores the clock to use the system UTC time (`DateTime.UtcNow`).  
- **Parameters** – None.  
- **Returns** – `void`.  
- **Throws** – None.

### static DateTime ConvertToTenantTime(DateTime utcTime, string tenantId)
Converts a UTC instant to the tenant’s local time based on the tenant’s configured time zone.  
- **utcTime** – The UTC `DateTime` to convert.  
- **tenantId** – Identifier of the tenant whose time zone should be applied.  
- **Returns** – A `DateTime` representing the same instant in the tenant’s local time (Kind.Unspecified).  
- **Throws** –  
  - `ArgumentNullException` if `tenantId` is `null` or empty.  
  - `ArgumentException` if the tenant cannot be found or its time zone is invalid.

### static DateTime ConvertFromTenantTime(DateTime tenantTime, string tenantId)
Converts a tenant‑local instant to UTC.  
- **tenantTime** – The `DateTime` in the tenant’s local time (Kind.Unspecified).  
- **tenantId** – Identifier of the tenant.  
- **Returns** – The equivalent UTC `DateTime`.  
- **Throws** – Same exceptions as `ConvertToTenantTime`.

### static bool IsBusinessHours(DateTime utcTime, string tenantId, DayOfWeek[]? businessDays = null, TimeSpan? start = null, TimeSpan? end = null)
Determines whether the supplied UTC time falls within the tenant’s business hours.  
- **utcTime** – The UTC `DateTime` to evaluate.  
- **tenantId** – Identifier of the tenant.  
- **businessDays** – Optional array of days considered business days (default: Monday‑Friday).  
- **start** – Optional start of the business day (default: 09:00).  
- **end** – Optional end of the business day (default: 17:00).  
- **Returns** – `true` if `utcTime` is within business hours for the tenant; otherwise `false`.  
- **Throws** –  
  - `ArgumentNullException` if `tenantId` is `null` or empty.  
  - `ArgumentException` if `start` is supplied and is later than `end`.

### static TimeSpan GetTimeUntilDeadline(DateTime utcTime, DateTime deadlineUtc)
Calculates the remaining time until a deadline.  
- **utcTime** – The current UTC time.  
- **deadlineUtc** – The deadline expressed as UTC.  
- **Returns** – A `TimeSpan` equal to `deadlineUtc - utcTime`. May be zero or negative if the deadline has passed.  
- **Throws** – None.

### static bool IsDeadlineExceeded(DateTime utcTime, DateTime deadlineUtc)
Checks whether a deadline has been reached or passed.  
- **utcTime** – The current UTC time.  
- **deadlineUtc** – The deadline expressed as UTC.  
- **Returns** – `true` if `utcTime >= deadlineUtc`; otherwise `false`.  
- **Throws** – None.

### static DateTime CalculateSlaDeadline(DateTime utcTime, TimeSpan sla, string tenantId, DayOfWeek[]? businessDays = null, TimeSpan? start = null, TimeSpan? end = null)
Computes an SLA deadline by adding the SLA interval to `utcTime`, adjusting for non‑business hours and the tenant’s time zone.  
- **utcTime** – The start time in UTC.  
- **sla** – The SLA duration to add. Must be zero or positive.  
- **tenantId** – Identifier of the tenant (used for time zone and business‑hour rules).  
- **businessDays** – Optional business days override.  
- **start** – Optional start of the business day.  
- **end** – Optional end of the business day.  
- **Returns** – The resulting deadline as a UTC `DateTime`.  
- **Throws** –  
  - `ArgumentNullException` if `tenantId` is `null` or empty.  
  - `ArgumentException` if `sla` is negative or if `start` is later than `end`.

### static IServiceCollection AddTimeProvider(this IServiceCollection services, Action<TimeProviderOptions>? configure = null)
Registers the `ITimeProvider` implementation with the dependency‑injection container.  
- **services** – The `IServiceCollection` to register into.  
- **configure** – Optional delegate to configure `TimeProviderOptions` (e.g., default time‑zone mappings).  
- **Returns** – The same `IServiceCollection` instance to allow chaining.  
- **Throws** – `ArgumentNullException` if `services` is `null`.

## Usage

### Example 1: Controlling time in unit tests
```csharp
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class TimeProviderTests
{
    [Fact]
    public void AdvanceTime_MovesClockForward()
    {
        var services = new ServiceCollection();
        services.AddTimeProvider();
        var provider = services.BuildServiceProvider()
                               .GetRequiredService<ITimeProvider>();

        // Set a known start time
        provider.SetCurrentTime(new DateTime(2024, 01, 01, 12, 0, 0, DateTimeKind.Utc));

        // Advance by 3 hours
        provider.AdvanceTime(TimeSpan.FromHours(3));

        // Retrieve the adjusted time via a helper that reads the provider's clock
        Assert.Equal(new DateTime(2024, 01, 01, 15, 0, 0, DateTimeKind.Utc),
                     DateTime.UtcNow); // assuming the helper reads from the provider
    }
}
```

### Example 2: Computing an SLA deadline respecting tenant business hours
```csharp
using Microsoft.Extensions.DependencyInjection;

public class SlaService
{
    private readonly ITimeProvider _timeProvider;

    public SlaService(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateTime GetSlaDeadlineUtc(string tenantId, TimeSpan sla)
    {
        // Use the provider's current UTC time as the SLA start point
        var now = DateTime.UtcNow; // in real code, read from _timeProvider if it exposes a getter
        return ITimeProvider.CalculateSlaDeadline(
            utcTime: now,
            sla: sla,
            tenantId: tenantId,
            businessDays: new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            start: TimeSpan.FromHours(9),
            end: TimeSpan.FromHours(17));
    }
}
```

## Notes
- **Thread safety** – The static conversion and calculation methods do not modify shared state and are safe to call concurrently. The instance methods (`SetCurrentTime`, `AdvanceTime`, `Reset`) mutate the provider’s internal clock; if the same `ITimeProvider` instance is shared across threads, external synchronization is required. In typical DI scenarios the service is registered as a singleton, so callers should treat these methods as affecting a global test clock and avoid invoking them from production code paths that run in parallel.
- **Edge cases** –  
  - Supplying a `tenantId` that is not configured will cause the static conversion methods to throw an `ArgumentException`.  
  - Business‑hour calculations assume the supplied `start` and `end` times are within the same day; crossing midnight (e.g., a night shift) is not supported without splitting the interval into two ranges.  
  - When `AdvanceTime` would move the clock beyond the provider’s limits (currently `DateTime.MinValue` to `DateTime.MaxValue`), an `ArgumentException` is raised.  
  - The `CalculateSlaDeadline` method treats the SLA as a pure duration; it does not automatically retry or extend the deadline if the calculated instant falls inside a non‑business period—instead it shifts the deadline forward to the next business moment.  
- **Time‑zone handling** – Conversions rely on the tenant’s IANA time‑zone identifier stored in the application’s configuration. Changes to a tenant’s time zone after registration will affect subsequent calls but not previously calculated values.  
- **Disposal** – The interface does not expose any disposable resources; implementing classes should clean up any internal timers or subscriptions if needed.
