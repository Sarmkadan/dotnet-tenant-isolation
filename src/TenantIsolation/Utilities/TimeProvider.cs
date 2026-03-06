// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace TenantIsolation.Utilities;

/// <summary>
/// Time provider interface for dependency injection of time operations
/// Enables testing and mocking of time-dependent code
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Get current UTC time
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Get current local time
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Get current date
    /// </summary>
    DateTime Today { get; }

    /// <summary>
    /// Get current timezone
    /// </summary>
    TimeZoneInfo TimeZone { get; }
}

/// <summary>
/// System time provider implementation
/// Provides real system time
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
    public DateTime Today => DateTime.Today;
    public TimeZoneInfo TimeZone => TimeZoneInfo.Local;
}

/// <summary>
/// Mock time provider for testing
/// Allows setting and controlling time
/// </summary>
public class MockTimeProvider : ITimeProvider
{
    private DateTime _currentTime = DateTime.UtcNow;

    public DateTime UtcNow => _currentTime;
    public DateTime Now => _currentTime;
    public DateTime Today => _currentTime.Date;
    public TimeZoneInfo TimeZone => TimeZoneInfo.Local;

    /// <summary>
    /// Set the mock time
    /// </summary>
    public void SetCurrentTime(DateTime time)
    {
        _currentTime = time;
    }

    /// <summary>
    /// Advance time by specified amount
    /// </summary>
    public void AdvanceTime(TimeSpan amount)
    {
        _currentTime = _currentTime.Add(amount);
    }

    /// <summary>
    /// Reset to system time
    /// </summary>
    public void Reset()
    {
        _currentTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Time manipulation utilities
/// </summary>
public static class TimeProviderExtensions
{
    /// <summary>
    /// Convert UTC time to tenant's timezone
    /// </summary>
    public static DateTime ConvertToTenantTime(this ITimeProvider timeProvider, DateTime utcTime, string tenantTimeZoneId)
    {
        try
        {
            var tenantTimeZone = TimeZoneInfo.FindSystemTimeZoneById(tenantTimeZoneId);
            return TimeZoneInfo.ConvertTime(utcTime, TimeZoneInfo.Utc, tenantTimeZone);
        }
        catch
        {
            return utcTime;
        }
    }

    /// <summary>
    /// Convert tenant's local time to UTC
    /// </summary>
    public static DateTime ConvertFromTenantTime(this ITimeProvider timeProvider, DateTime localTime, string tenantTimeZoneId)
    {
        try
        {
            var tenantTimeZone = TimeZoneInfo.FindSystemTimeZoneById(tenantTimeZoneId);
            return TimeZoneInfo.ConvertTime(localTime, tenantTimeZone, TimeZoneInfo.Utc);
        }
        catch
        {
            return localTime;
        }
    }

    /// <summary>
    /// Check if time is within business hours
    /// </summary>
    public static bool IsBusinessHours(this ITimeProvider timeProvider, DateTime time, int startHour = 9, int endHour = 17)
    {
        var hour = time.Hour;
        return hour >= startHour && hour < endHour && time.DayOfWeek != DayOfWeek.Saturday && time.DayOfWeek != DayOfWeek.Sunday;
    }

    /// <summary>
    /// Get remaining time until deadline
    /// </summary>
    public static TimeSpan GetTimeUntilDeadline(this ITimeProvider timeProvider, DateTime deadline)
    {
        var remaining = deadline - timeProvider.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Check if deadline has passed
    /// </summary>
    public static bool IsDeadlineExceeded(this ITimeProvider timeProvider, DateTime deadline)
    {
        return timeProvider.UtcNow > deadline;
    }

    /// <summary>
    /// Calculate SLA deadline (considering business hours)
    /// </summary>
    public static DateTime CalculateSlaDeadline(this ITimeProvider timeProvider, int businessHoursNeeded)
    {
        var deadline = timeProvider.UtcNow;
        var hoursAdded = 0;

        while (hoursAdded < businessHoursNeeded)
        {
            deadline = deadline.AddHours(1);
            if (timeProvider.IsBusinessHours(deadline))
                hoursAdded++;
        }

        return deadline;
    }
}

/// <summary>
/// Extension method to register time provider
/// </summary>
public static class TimeProviderServiceExtensions
{
    public static IServiceCollection AddTimeProvider(this IServiceCollection services, bool useMock = false)
    {
        if (useMock)
            services.AddSingleton<ITimeProvider, MockTimeProvider>();
        else
            services.AddSingleton<ITimeProvider, SystemTimeProvider>();

        return services;
    }
}
