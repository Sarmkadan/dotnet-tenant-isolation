# DateTimeExtensions

Provides a set of utility methods for common `DateTime` operations, including day/week/month/year boundaries, business day calculations, relative time formatting, and age/expiration checks.

## API

### `StartOfDay(DateTime date)`
Returns a `DateTime` representing the start of the specified day (midnight, 00:00:00).
**Parameters:** `date` – The input date.
**Returns:** A `DateTime` at the start of the day.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `EndOfDay(DateTime date)`
Returns a `DateTime` representing the end of the specified day (23:59:59.9999999).
**Parameters:** `date` – The input date.
**Returns:** A `DateTime` at the end of the day.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `StartOfWeek(DateTime date)`
Returns a `DateTime` representing the start of the week (Sunday at 00:00:00) for the specified date.
**Parameters:** `date` – The input date.
**Returns:** A `DateTime` at the start of the week.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `EndOfWeek(DateTime date)`
Returns a `DateTime` representing the end of the week (Saturday at 23:59:59.9999999) for the specified date.
**Parameters:** `date` – The input date.
**Returns:** A `DateTime` at the end of the week.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `StartOfMonth(DateTime date)`
Returns a `DateTime` representing the start of the month (1st day at 00:00:00) for the specified date.
**Parameters:** `date` – The input date.
**Returns:** A `DateTime` at the start of the month.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `EndOfMonth(DateTime date)`
Returns a `DateTime` representing the end of the month (last day at 23:59:59.9999999) for the specified date.
**Parameters:** `date` – The input date.
**Returns:** A `DateTime` at the end of the month.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `StartOfYear(DateTime date)`
Returns a `DateTime` representing the start of the year (January 1st at 00:00:00) for the specified date.
**Parameters:** `date` – The input date.
**Returns:** A `DateTime` at the start of the year.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `EndOfYear(DateTime date)`
Returns a `DateTime` representing the end of the year (December 31st at 23:59:59.9999999) for the specified date.
**Parameters:** `date` – The input date.
**Returns:** A `DateTime` at the end of the year.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `IsToday(DateTime date)`
Determines whether the specified date is today.
**Parameters:** `date` – The input date.
**Returns:** `true` if the date is today; otherwise, `false`.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `IsPast(DateTime date)`
Determines whether the specified date is in the past (before `DateTime.UtcNow`).
**Parameters:** `date` – The input date.
**Returns:** `true` if the date is in the past; otherwise, `false`.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `IsFuture(DateTime date)`
Determines whether the specified date is in the future (after `DateTime.UtcNow`).
**Parameters:** `date` – The input date.
**Returns:** `true` if the date is in the future; otherwise, `false`.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `ToRelativeTime(DateTime date)`
Converts the specified date to a human-readable relative time string (e.g., "2 hours ago", "in 3 days").
**Parameters:** `date` – The input date.
**Returns:** A localized relative time string.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `IsInRange(DateTime date, DateTime start, DateTime end)`
Determines whether the specified date falls within the range `[start, end]` (inclusive).
**Parameters:**
- `date` – The date to check.
- `start` – The start of the range.
- `end` – The end of the range.
**Returns:** `true` if the date is within the range; otherwise, `false`.
**Throws:** `ArgumentOutOfRangeException` if any argument is outside the valid range for `DateTime`.

### `AddBusinessDays(DateTime date, int days)`
Adds the specified number of business days to the input date, skipping weekends and optionally configured holidays.
**Parameters:**
- `date` – The starting date.
- `days` – The number of business days to add (can be negative).
**Returns:** A `DateTime` representing the resulting date.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `GetBusinessDaysBetween(DateTime start, DateTime end)`
Calculates the number of business days between two dates, excluding weekends and optionally configured holidays.
**Parameters:**
- `start` – The start date.
- `end` – The end date.
**Returns:** The count of business days between the dates (inclusive if `start` ≤ `end`).
**Throws:**
- `ArgumentOutOfRangeException` if either date is outside the valid range for `DateTime`.
- `ArgumentException` if `start` > `end`.

### `IsExpiringWithin(DateTime date, TimeSpan threshold)`
Determines whether the specified date is within the given threshold of expiring (i.e., within `threshold` of `DateTime.UtcNow`).
**Parameters:**
- `date` – The expiration date.
- `threshold` – The time span to check against.
**Returns:** `true` if the date is within `threshold` of `DateTime.UtcNow`; otherwise, `false`.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `HasExpired(DateTime date)`
Determines whether the specified date has already passed (i.e., is before `DateTime.UtcNow`).
**Parameters:** `date` – The expiration date.
**Returns:** `true` if the date is in the past; otherwise, `false`.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

### `GetAgeInYears(DateTime birthDate)`
Calculates the age in full years from the given birth date to the current date.
**Parameters:** `birthDate` – The birth date.
**Returns:** The age in years.
**Throws:** `ArgumentOutOfRangeException` if `birthDate` is outside the valid range for `DateTime`.

### `ToIso8601String(DateTime date)`
Converts the specified date to an ISO 8601-compliant string (e.g., "2023-10-05T14:30:00Z").
**Parameters:** `date` – The input date.
**Returns:** An ISO 8601-formatted string.
**Throws:** `ArgumentOutOfRangeException` if `date` is outside the valid range for `DateTime`.

## Usage

### Example 1: Business Day Calculations
