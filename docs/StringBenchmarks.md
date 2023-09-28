# StringBenchmarks

StringBenchmarks is a utility class providing optimized string manipulation methods for performance-critical scenarios, such as generating slugs, validating formats, and transforming strings into standardized representations. These methods are designed for use in benchmarking and high-throughput applications where deterministic behavior and efficiency are prioritized.

## API

### ToSlug_Ascii

**Purpose**: Converts a string to a URL-friendly ASCII-only slug by removing or replacing non-alphanumeric characters.

**Parameters**: `string input` - The input string to transform.

**Return Value**: A string containing only ASCII characters suitable for use in URLs.

**Exceptions**: Throws `ArgumentNullException` if `input` is null.

---

### ToSlug_Unicode

**Purpose**: Converts a string to a URL-friendly slug while preserving Unicode characters.

**Parameters**: `string input` - The input string to transform.

**Return Value**: A string with Unicode characters retained but normalized for URL compatibility.

**Exceptions**: Throws `ArgumentNullException` if `input` is null.

---

### GetDeterministicHashCode

**Purpose**: Computes a hash code for a string in a way that produces consistent results across application restarts.

**Parameters**: `string input` - The string to hash.

**Return Value**: An integer representing the deterministic hash code.

**Exceptions**: Throws `ArgumentNullException` if `input` is null.

---

### MaskSensitiveData

**Purpose**: Replaces sensitive portions of a string (e.g., credit card numbers, passwords) with masked characters.

**Parameters**: `string input` - The string to mask.

**Return Value**: A string with sensitive data obscured.

**Exceptions**: Throws `ArgumentNullException` if `input` is null.

---

### ToHumanReadable

**Purpose**: Transforms a technical or encoded string into a human-readable format (e.g., camelCase to "Camel Case").

**Parameters**: `string input` - The string to format.

**Return Value**: A string formatted for readability.

**Exceptions**: Throws `ArgumentNullException` if `input` is null.

---

### RemoveSpecialCharacters

**Purpose**: Strips non-alphanumeric characters from a string, leaving only letters and digits.

**Parameters**: `string input` - The input string to process.

**Return Value**: A string containing only alphanumeric characters.

**Exceptions**: Throws `ArgumentNullException` if `input` is null.

---

### IsValidEmail

**Purpose**: Validates whether a string conforms to standard email address formats.

**Parameters**: `string input` - The string to validate.

**Return Value**: `true` if the string is a valid email; otherwise, `false`.

**Exceptions**: None. Returns `false` for null or empty inputs.

---

### IsValidUrl

**Purpose**: Validates whether a string is a well-formed URL.

**Parameters**: `string input` - The string to validate.

**Return Value**: `true` if the string is a valid URL; otherwise, `false`.

**Exceptions**: None. Returns `false` for null or empty inputs.

---

### ToPascalCase

**Purpose**: Converts a string to PascalCase (e.g., "hello_world" becomes "HelloWorld").

**Parameters**: `string input` - The input string to transform.

**Return Value**: A string in PascalCase format.

**Exceptions**: Throws `ArgumentNullException` if `input` is null.

---

## Usage

```csharp
// Example 1: Generate a URL slug and validate an email
string title = "C# Performance Tips & Tricks!";
string slug = StringBenchmarks.ToSlug_Unicode(title); // "c-performance-tips-tricks"
bool isEmailValid = StringBenchmarks.IsValidEmail("user@example.com"); // true
```

```csharp
// Example 2: Mask sensitive data and convert to PascalCase
string sensitive = "CreditCard: 1234-5678-9012-3456";
string masked = StringBenchmarks.MaskSensitiveData(sensitive); // "CreditCard: ****-****-****-****"
string pascal = StringBenchmarks.ToPascalCase("tenant_id"); // "TenantId"
```

---

## Notes

- **Null Handling**: All methods except `IsValidEmail` and `IsValidUrl` throw `ArgumentNullException` if the input string is null. Validation methods return `false` for null or empty inputs.
- **Thread Safety**: All methods are thread-safe as they are stateless and do not rely on mutable shared resources.
- **Edge Cases**: 
  - `ToSlug_Ascii` and `ToSlug_Unicode` may return an empty string if the input contains no alphanumeric characters.
  - `RemoveSpecialCharacters` will return an empty string for inputs with no alphanumeric characters.
  - `GetDeterministicHashCode` ensures consistent hash values across application domains, making it suitable for caching scenarios.
