# StringExtensions

A static utility class providing common string manipulation and validation methods for .NET applications, particularly in multi-tenant scenarios where consistent string formatting and sanitization are required.

## API

### `public static string ToSlug(string input)`

Converts a string to a URL-friendly slug by replacing spaces with hyphens, removing special characters, and converting to lowercase.

- **Parameters**
  - `input` (string): The string to convert to a slug.
- **Return value**
  - A URL-friendly slug string, or `null` if `input` is `null`.
- **Exceptions**
  - Throws `ArgumentNullException` if `input` is `null`.

---

### `public static string Truncate(string input, int maxLength, string suffix = "...")`

Truncates a string to a specified maximum length and appends an optional suffix if truncation occurs.

- **Parameters**
  - `input` (string): The string to truncate.
  - `maxLength` (int): The maximum length of the resulting string, including the suffix.
  - `suffix` (string, optional): The suffix to append if truncation occurs. Defaults to `"..."`.
- **Return value**
  - The truncated string with suffix if applicable, or the original string if no truncation is needed. Returns `null` if `input` is `null`.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `maxLength` is less than the length of `suffix`.
  - Throws `ArgumentNullException` if `input` is `null`.

---

### `public static bool IsValidEmail(string email)`

Validates whether a string is a well-formed email address.

- **Parameters**
  - `email` (string): The string to validate as an email address.
- **Return value**
  - `true` if the string is a valid email address; otherwise, `false`. Returns `false` if `email` is `null`.
- **Exceptions**
  - None.

---

### `public static bool IsValidUrl(string url)`

Validates whether a string is a well-formed URL.

- **Parameters**
  - `url` (string): The string to validate as a URL.
- **Return value**
  - `true` if the string is a valid URL; otherwise, `false`. Returns `false` if `url` is `null`.
- **Exceptions**
  - None.

---

### `public static string SafeSubstring(string input, int startIndex, int length)`

Safely extracts a substring without throwing exceptions for out-of-range indices.

- **Parameters**
  - `input` (string): The string to extract from.
  - `startIndex` (int): The zero-based starting character position.
  - `length` (int): The number of characters to return.
- **Return value**
  - A substring starting at `startIndex` with up to `length` characters. Returns `null` if `input` is `null`.
- **Exceptions**
  - None.

---
### `public static string RemoveSpecialCharacters(string input, string replacement = "")`

Removes all non-alphanumeric characters from a string, optionally replacing them with a specified string.

- **Parameters**
  - `input` (string): The string to process.
  - `replacement` (string, optional): The string to substitute for removed characters. Defaults to an empty string.
- **Return value**
  - A new string with special characters removed or replaced. Returns `null` if `input` is `null`.
- **Exceptions**
  - None.

---
### `public static string MaskSensitiveData(string input, int keepStart = 3, int keepEnd = 2)`

Masks sensitive data by replacing the middle portion of a string with asterisks.

- **Parameters**
  - `input` (string): The string to mask.
  - `keepStart` (int, optional): Number of characters to retain at the start. Defaults to `3`.
  - `keepEnd` (int, optional): Number of characters to retain at the end. Defaults to `2`.
- **Return value**
  - A masked string, or `null` if `input` is `null`.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `keepStart` or `keepEnd` are negative, or if their sum exceeds the length of `input`.

---
### `public static string ToPascalCase(string input)`

Converts a string to PascalCase by capitalizing the first letter of each word and removing separators.

- **Parameters**
  - `input` (string): The string to convert.
- **Return value**
  - A PascalCase string, or `null` if `input` is `null`.
- **Exceptions**
  - None.

---
### `public static string ToHumanReadable(string input)`

Converts a PascalCase or camelCase string to a human-readable format with spaces between words.

- **Parameters**
  - `input` (string): The string to convert.
- **Return value**
  - A human-readable string, or `null` if `input` is `null`.
- **Exceptions**
  - None.

---
### `public static int GetDeterministicHashCode(string input)`

Computes a deterministic hash code for a string using a consistent algorithm.

- **Parameters**
  - `input` (string): The string to hash.
- **Return value**
  - A 32-bit signed integer hash code. Returns `0` if `input` is `null`.
- **Exceptions**
  - None.

## Usage
