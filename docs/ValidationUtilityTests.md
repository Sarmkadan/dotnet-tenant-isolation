# ValidationUtilityTests

Unit tests for the `ValidationUtility` class, verifying email validation, slug generation/conversion, GUID parsing, string truncation, and range/value validation methods used for tenant isolation scenarios.

## API

### `IsValidEmail_WithVariousInputs_ReturnsExpectedResult`
Validates that the `ValidationUtility.IsValidEmail` method correctly identifies valid and invalid email addresses across a variety of input cases. Returns `void`.

### `IsValidSlug_WithVariousInputs_ReturnsExpectedResult`
Validates that the `ValidationUtility.IsValidSlug` method correctly identifies valid and invalid slug formats. Returns `void`.

### `RequireNotEmpty_WhenNull_ThrowsTenantIsolationExceptionMentioningFieldName`
Verifies that `ValidationUtility.RequireNotEmpty` throws a `TenantIsolationException` with a message that includes the field name when the input value is `null`. Returns `void`.

### `RequireNotEmpty_WhenWhitespaceOnly_ThrowsTenantIsolationException`
Verifies that `ValidationUtility.RequireNotEmpty` throws a `TenantIsolationException` when the input string contains only whitespace. Returns `void`.

### `RequirePositive_WhenZero_ThrowsTenantIsolationException`
Verifies that `ValidationUtility.RequirePositive` throws a `TenantIsolationException` when the input numeric value is zero. Returns `void`.

### `RequirePositive_WhenPositiveValue_DoesNotThrow`
Verifies that `ValidationUtility.RequirePositive` does not throw an exception when the input numeric value is positive. Returns `void`.

### `RequireRange_WhenValueBelowMinimum_ThrowsTenantIsolationException`
Verifies that `ValidationUtility.RequireRange` throws a `TenantIsolationException` when the input value is below the specified minimum. Returns `void`.

### `RequireRange_WhenValueAboveMaximum_ThrowsTenantIsolationException`
Verifies that `ValidationUtility.RequireRange` throws a `TenantIsolationException` when the input value exceeds the specified maximum. Returns `void`.

### `RequireValidSlug_WhenInvalidFormat_ThrowsExceptionWithHelpfulMessage`
Verifies that `ValidationUtility.RequireValidSlug` throws an exception with a detailed message when the input slug does not conform to expected format rules. Returns `void`.

### `IsValidGuid_WithWellFormedGuid_ReturnsTrue`
Validates that `ValidationUtility.IsValidGuid` returns `true` for a properly formatted GUID string. Returns `void`.

### `IsValidGuid_WithMalformedString_ReturnsFalse`
Validates that `ValidationUtility.IsValidGuid` returns `false` for a malformed or invalid GUID string. Returns `void`.

### `ToSlug_WithSpacesAndUppercase_ReturnsLowercaseHyphenated`
Verifies that `ValidationUtility.ToSlug` converts a string containing spaces and uppercase letters into a lowercase, hyphen-separated slug. Returns `void`.

### `ToSlug_WithEmptyString_ReturnsEmptyString`
Verifies that `ValidationUtility.ToSlug` returns an empty string when the input is empty. Returns `void`.

### `ToSlug_WithSpecialCharacters_RemovesNonAlphanumeric`
Verifies that `ValidationUtility.ToSlug` removes or replaces non-alphanumeric characters when converting a string to a slug. Returns `void`.

### `Truncate_WhenLongerThanMaxLength_TruncatesAndAddsEllipsis`
Verifies that `ValidationUtility.Truncate` truncates an input string to the specified maximum length and appends an ellipsis (`…`) when the input exceeds that length. Returns `void`.

### `Truncate_WhenShorterThanMaxLength_ReturnsOriginalString`
Verifies that `ValidationUtility.Truncate` returns the original string unchanged when its length is less than or equal to the specified maximum length. Returns `void`.

### `MaskSensitiveData_MasksAllCharactersAfterVisibleCount`
Verifies that `ValidationUtility.MaskSensitiveData` masks all characters after the first `visibleCount` characters with asterisks. Returns `void`.

### `IsValidEmail_ExtensionMethod_WithValidAddress_ReturnsTrue`
Validates that the extension method `string.IsValidEmail()` returns `true` for a valid email address. Returns `void`.

### `IsValidUrl_WithHttpsScheme_ReturnsTrue`
Validates that `ValidationUtility.IsValidUrl` returns `true` for a URL with the `https` scheme. Returns `void`.

### `IsValidUrl_WithFtpScheme_ReturnsFalse`
Validates that `ValidationUtility.IsValidUrl` returns `false` for a URL with the `ftp` scheme. Returns `void`.

## Usage
