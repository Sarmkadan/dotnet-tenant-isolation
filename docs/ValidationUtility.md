# ValidationUtility

`ValidationUtility` is a static utility class that provides centralized, reusable validation logic for common data types and constraints within the `dotnet-tenant-isolation` project. It offers both boolean check methods that return a result without throwing, and `Require*` guard methods that throw descriptive exceptions when validation fails, enabling consistent precondition enforcement across the codebase.

## API

### `IsValidEmail(string email)`

Returns `true` if the provided string is a non-null, non-empty email address conforming to a standard email format; otherwise `false`. This method performs a structural check only and does not verify domain reachability.

### `IsValidSlug(string slug)`

Returns `true` if the provided string is a non-null, non-empty URL-friendly slug (typically lowercase alphanumeric characters and hyphens, no leading/trailing hyphens, no consecutive hyphens); otherwise `false`.

### `IsValidGuid(string input)`

Returns `true` if the provided string is a non-null, non-empty value that can be successfully parsed as a `Guid`; otherwise `false`.

### `IsValidUrl(string url)`

Returns `true` if the provided string is a non-null, non-empty absolute URL with a valid scheme (HTTP or HTTPS) and well-formed structure; otherwise `false`.

### `RequireNotEmpty(string value, string paramName)`

Throws an `ArgumentException` if `value` is `null`, empty, or consists solely of whitespace. The `paramName` is included in the exception message to identify the offending argument.

### `RequireMinLength(string value, int minLength, string paramName)`

Throws an `ArgumentException` if `value` is `null` or its length is less than `minLength`. The `paramName` is included in the exception message.

### `RequireMaxLength(string value, int maxLength, string paramName)`

Throws an `ArgumentException` if `value` is `null` or its length exceeds `maxLength`. The `paramName` is included in the exception message.

### `RequireLengthBetween(string value, int minLength, int maxLength, string paramName)`

Throws an `ArgumentException` if `value` is `null`, or if its length is strictly less than `minLength` or strictly greater than `maxLength`. The `paramName` is included in the exception message.

### `RequireValidEmail(string email, string paramName)`

Throws an `ArgumentException` if `email` does not satisfy `IsValidEmail`. The `paramName` is included in the exception message.

### `RequireValidSlug(string slug, string paramName)`

Throws an `ArgumentException` if `slug` does not satisfy `IsValidSlug`. The `paramName` is included in the exception message.

### `RequireValidGuid(string input, string paramName)`

Throws an `ArgumentException` if `input` does not satisfy `IsValidGuid`. The `paramName` is included in the exception message.

### `RequireValidUrl(string url, string paramName)`

Throws an `ArgumentException` if `url` does not satisfy `IsValidUrl`. The `paramName` is included in the exception message.

### `RequireNotNull(object value, string paramName)`

Throws an `ArgumentNullException` if `value` is `null`. The `paramName` is used as the parameter name in the exception.

### `RequirePositive(int value, string paramName)`

Throws an `ArgumentOutOfRangeException` if `value` is less than or equal to zero. The `paramName` is included in the exception message.

### `RequireRange(int value, int min, int max, string paramName)`

Throws an `ArgumentOutOfRangeException` if `value` is less than `min` or greater than `max`. The `paramName` is included in the exception message.

### `RequireFutureDate(DateTime date, string paramName)`

Throws an `ArgumentOutOfRangeException` if `date` is not strictly in the future relative to the current system time. The `paramName` is included in the exception message.

### `RequirePastDate(DateTime date, string paramName)`

Throws an `ArgumentOutOfRangeException` if `date` is not strictly in the past relative to the current system time. The `paramName` is included in the exception message.

### `RequireValidDateRange(DateTime start, DateTime end, string startParamName, string endParamName)`

Throws an `ArgumentOutOfRangeException` if `start` is not strictly earlier than `end`. The parameter names are included in the exception message to identify the offending bounds.

### `RequireValidEnum<T>(object value, string paramName)`

Throws an `ArgumentException` if `value` is `null` or not a defined value for the enum type `T`. The `paramName` is included in the exception message.

## Usage

```csharp
// Example 1: Validating tenant creation input
public Tenant CreateTenant(string tenantSlug, string adminEmail, DateTime contractStart)
{
    ValidationUtility.RequireValidSlug(tenantSlug, nameof(tenantSlug));
    ValidationUtility.RequireValidEmail(adminEmail, nameof(adminEmail));
    ValidationUtility.RequireFutureDate(contractStart, nameof(contractStart));

    return new Tenant
    {
        Slug = tenantSlug,
        AdminEmail = adminEmail,
        ContractStart = contractStart
    };
}
```

```csharp
// Example 2: Conditional check before proceeding
public void ProcessUrl(string rawUrl)
{
    if (!ValidationUtility.IsValidUrl(rawUrl))
    {
        logger.Warn("Invalid URL submitted, skipping processing.");
        return;
    }

    // Proceed with URL processing
    FetchAndParse(rawUrl);
}
```

## Notes

- All `Require*` methods are designed as guard clauses and will throw on the calling thread immediately upon failure; they do not perform asynchronous work.
- The `IsValid*` boolean methods never throw and are safe to use in conditional logic without try/catch blocks.
- Date comparisons in `RequireFutureDate` and `RequirePastDate` use the system-local `DateTime.UtcNow` (or equivalent current time) at the moment of invocation, making them sensitive to clock skew and time zone configuration.
- `RequireValidDateRange` only enforces chronological ordering; it does not validate that either date falls within a specific boundary.
- `RequireValidEnum<T>` relies on `Enum.IsDefined`, which may incur boxing and reflection overhead; it is suitable for input validation but not for hot-path performance scenarios.
- The class is stateless and all methods are static, making them inherently thread-safe with no shared mutable state.
