// existing content ...

## StringBenchmarks

The `StringBenchmarks` class provides a set of benchmarks for evaluating the performance of string manipulation operations. It includes tests for slug generation, hash calculation, data masking, and string formatting.

### Example Usage

```csharp
// Create a new instance of StringBenchmarks
var stringBenchmarks = new StringBenchmarks();

// Test slug generation with ASCII characters
var slugAscii = stringBenchmarks.ToSlug_Ascii("Hello World");

// Test slug generation with Unicode characters
var slugUnicode = stringBenchmarks.ToSlug_Unicode("Bonjour Monde");

// Test hash calculation
var hash = stringBenchmarks.GetDeterministicHashCode("Hello World");

// Test data masking
var maskedData = stringBenchmarks.MaskSensitiveData("Hello World");

// Test human-readable string formatting
var humanReadable = stringBenchmarks.ToHumanReadable("Hello World");

// Test removal of special characters
var cleanedString = stringBenchmarks.RemoveSpecialCharacters("Hello World!");

// Test email validation
var isValidEmail = stringBenchmarks.IsValidEmail("hello@example.com");

// Test URL validation
var isValidUrl = stringBenchmarks.IsValidUrl("https://example.com");

// Test Pascal case conversion
var pascalCase = stringBenchmarks.ToPascalCase("hello world");
```

// existing content ...
