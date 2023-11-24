#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace TenantIsolation.Utilities;

/// <summary>
/// DateTime utility extension methods for date/time operations
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Get start of day at midnight
    /// </summary>
    /// <param name="dateTime">The date/time value to get start of day for</param>
    /// <returns>The date with time set to midnight (00:00:00)</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Get end of day at 23:59:59.999
    /// </summary>
    /// <param name="dateTime">The date/time value to get end of day for</param>
    /// <returns>The date with time set to end of day (23:59:59.999)</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddMilliseconds(-1);
    }

    /// <summary>
    /// Get start of week (Monday)
    /// </summary>
    /// <param name="dateTime">The date/time value to get start of week for</param>
    /// <returns>The date representing the start of the week (Monday)</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static DateTime StartOfWeek(this DateTime dateTime)
    {
        var daysOffset = (int)dateTime.DayOfWeek - (int)DayOfWeek.Monday;
        return dateTime.AddDays(-daysOffset).StartOfDay();
    }

    /// <summary>
    /// Get end of week (Sunday)
    /// </summary>
    /// <param name="dateTime">The date/time value to get end of week for</param>
    /// <returns>The date representing the end of the week (Sunday)</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static DateTime EndOfWeek(this DateTime dateTime)
    {
        return dateTime.StartOfWeek().AddDays(6).EndOfDay();
    }

    /// <summary>
    /// Get start of month
    /// </summary>
    /// <param name="dateTime">The date/time value to get start of month for</param>
    /// <returns>The date representing the first day of the month</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Get end of month
    /// </summary>
    /// <param name="dateTime">The date/time value to get end of month for</param>
    /// <returns>The date representing the last day of the month</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return dateTime.StartOfMonth().AddMonths(1).AddDays(-1).EndOfDay();
    }

    /// <summary>
    /// Get start of year
    /// </summary>
    /// <param name="dateTime">The date/time value to get start of year for</param>
    /// <returns>The date representing the first day of the year</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static DateTime StartOfYear(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 1, 1);
    }

    /// <summary>
    /// Get end of year
    /// </summary>
    /// <param name="dateTime">The date/time value to get end of year for</param>
    /// <returns>The date representing the last day of the year</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static DateTime EndOfYear(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 12, 31).EndOfDay();
    }

    /// <summary>
    /// Check if date is today
    /// </summary>
    /// <param name="dateTime">The date/time value to check</param>
    /// <returns>True if the date is today; otherwise false</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static bool IsToday(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.UtcNow.Date;
    }

    /// <summary>
    /// Check if date is in past
    /// </summary>
    /// <param name="dateTime">The date/time value to check</param>
    /// <returns>True if the date is in the past; otherwise false</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static bool IsPast(this DateTime dateTime)
    {
        return dateTime < DateTime.UtcNow;
    }

    /// <summary>
    /// Check if date is in future
    /// </summary>
    /// <param name="dateTime">The date/time value to check</param>
    /// <returns>True if the date is in the future; otherwise false</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static bool IsFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Get human-readable relative time (e.g., "2 days ago", "in 3 hours")
    /// </summary>
    /// <param name="dateTime">The date/time value to convert</param>
    /// <returns>A human-readable string representing the relative time</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static string ToRelativeTime(this DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMilliseconds < 1000)
            return "just now";

        if (timeSpan.TotalSeconds < 60)
            return $"{(int)timeSpan.TotalSeconds} seconds ago";

        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";

        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";

        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";

        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";

        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} months ago";

        return $"{(int)(timeSpan.TotalDays / 365)} years ago";
    }

    /// <summary>
    /// Check if date is within range
    /// </summary>
    /// <param name="dateTime">The date/time value to check</param>
    /// <param name="start">The start date of the range (inclusive)</param>
    /// <param name="end">The end date of the range (inclusive)</param>
    /// <returns>True if the date is within the specified range; otherwise false</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/>, <paramref name="start"/>, or <paramref name="end"/> is not a valid DateTime value</exception>
    public static bool IsInRange(this DateTime dateTime, DateTime start, DateTime end)
    {
        return dateTime >= start && dateTime <= end;
    }

    /// <summary>
    /// Add business days to date (excluding weekends)
    /// Useful for SLA calculations and deadline tracking
    /// </summary>
    /// <param name="dateTime">The date/time value to add business days to</param>
    /// <param name="days">Number of business days to add</param>
    /// <returns>A new DateTime with the specified number of business days added</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="days"/> is negative</exception>
    public static DateTime AddBusinessDays(this DateTime dateTime, int days)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(days);

        var result = dateTime;
        int count = 0;
        while (count < days)
        {
            result = result.AddDays(1);
            if (result.DayOfWeek != DayOfWeek.Saturday && result.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }
        return result;
    }

    /// <summary>
    /// Get number of business days between two dates
    /// </summary>
    /// <param name="startDate">The start date of the range</param>
    /// <param name="endDate">The end date of the range (inclusive)</param>
    /// <returns>Number of business days between the two dates</returns>
    /// <exception cref="ArgumentException"><paramref name="startDate"/> or <paramref name="endDate"/> is not a valid DateTime value</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="endDate"/> is before <paramref name="startDate"/></exception>
    public static int GetBusinessDaysBetween(this DateTime startDate, DateTime endDate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(endDate, startDate);

        int count = 0;
        var current = startDate;
        while (current <= endDate)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                count++;
            current = current.AddDays(1);
        }
        return count;
    }

    /// <summary>
    /// Check if date is expiring within days
    /// Useful for subscription and license renewal notifications
    /// </summary>
    /// <param name="expiryDate">The expiry date to check</param>
    /// <param name="days">Number of days to check within</param>
    /// <returns>True if the expiry date is within the specified number of days; otherwise false</returns>
    /// <exception cref="ArgumentException"><paramref name="expiryDate"/> is not a valid DateTime value</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="days"/> is negative</exception>
    public static bool IsExpiringWithin(this DateTime expiryDate, int days)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(days);

        var daysUntilExpiry = (expiryDate - DateTime.UtcNow).TotalDays;
        return daysUntilExpiry >= 0 && daysUntilExpiry <= days;
    }

    /// <summary>
    /// Check if date has expired
    /// </summary>
    /// <param name="expiryDate">The expiry date to check</param>
    /// <returns>True if the expiry date is in the past; otherwise false</returns>
    /// <exception cref="ArgumentException"><paramref name="expiryDate"/> is not a valid DateTime value</exception>
    public static bool HasExpired(this DateTime expiryDate)
    {
        return expiryDate < DateTime.UtcNow;
    }

    /// <summary>
    /// Get age in years from birth date
    /// </summary>
    /// <param name="birthDate">The birth date</param>
    /// <returns>Age in years</returns>
    /// <exception cref="ArgumentException"><paramref name="birthDate"/> is not a valid DateTime value</exception>
    public static int GetAgeInYears(this DateTime birthDate)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age))
            age--;
        return age;
    }

    /// <summary>
    /// Convert to ISO 8601 format string
    /// </summary>
    /// <param name="dateTime">The date/time value to convert</param>
    /// <returns>ISO 8601 formatted date/time string</returns>
    /// <exception cref="ArgumentException"><paramref name="dateTime"/> is not a valid DateTime value</exception>
    public static string ToIso8601String(this DateTime dateTime)
    {
        return dateTime.ToString("O");
    }
}