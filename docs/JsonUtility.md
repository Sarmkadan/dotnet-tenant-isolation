# JsonUtility

The `JsonUtility` class provides a static, centralized abstraction for common JSON serialization, deserialization, and manipulation operations within the `dotnet-tenant-isolation` project. It wraps standard `System.Text.Json` functionality to enforce consistent configuration, simplify dynamic data handling, and offer safe parsing mechanisms that prevent exceptions during invalid input scenarios. This utility is designed to reduce boilerplate code while ensuring strict type safety and predictable behavior across tenant isolation boundaries.

## API

### Serialize<T>
Serializes an object of type `T` into a compact JSON string.
- **Parameters**: `T value` – The object to serialize.
- **Returns**: `string` – The JSON representation of the object.
- **Throws**: `NotSupportedException` if the type `T` is not supported by the configured serializer; standard serialization exceptions if the object graph contains circular references or unsupported types.

### SerializePretty<T>
Serializes an object of type `T` into a formatted (indented) JSON string.
- **Parameters**: `T value` – The object to serialize.
- **Returns**: `string` – The indented JSON representation of the object.
- **Throws**: Same exceptions as `Serialize<T>`.

### Deserialize<T>
Deserializes a JSON string into an object of type `T`.
- **Parameters**: `string json` – The JSON string to parse.
- **Returns**: `T?` – The deserialized object, or `null` if the JSON represents a null value.
- **Throws**: `JsonException` if the JSON is malformed or cannot be mapped to type `T`; `ArgumentNullException` if the input string is null.

### DeserializeSafe<T>
Attempts to deserialize a JSON string into an object of type `T` without throwing exceptions on failure.
- **Parameters**: `string json` – The JSON string to parse.
- **Returns**: `T?` – The deserialized object if successful; otherwise, `default(T)` (typically `null` for reference types).
- **Throws**: No exceptions are thrown; parsing errors result in a default return value.

### DeserializeToDynamic
Deserializes a JSON string into a `JsonElement` for dynamic inspection.
- **Parameters**: `string json` – The JSON string to parse.
- **Returns**: `JsonElement?` – The parsed element, or `null` if the input is null or empty.
- **Throws**: `JsonException` if the JSON is malformed.

### ConvertToDictionary<T>
Converts a JSON string or an object of type `T` into a flat dictionary representation.
- **Parameters**: `T obj` – The object to convert (or JSON string depending on overload implementation context).
- **Returns**: `Dictionary<string, object?>` – A dictionary where keys are property names and values are the corresponding property values.
- **Throws**: `InvalidOperationException` if the type `T` cannot be represented as a dictionary (e.g., primitive types or arrays).

### MergeJsonObjects<T1, T2>
Merges two objects of different types into a single dictionary, combining their properties.
- **Parameters**: `T1 first`, `T2 second` – The two objects to merge.
- **Returns**: `Dictionary<string, object?>` – A combined dictionary. If duplicate keys exist, the value from the second object typically overwrites the first.
- **Throws**: Exceptions related to serialization of either input type.

### ExtractValue
Extracts a specific value from a JSON structure based on a path or key.
- **Parameters**: `string json`, `string key` (or path) – The source JSON and the identifier to extract.
- **Returns**: `object?` – The extracted value, or `null` if the key does not exist.
- **Throws**: `JsonException` if the source JSON is invalid.

### IsValidJson
Validates whether a given string is well-formed JSON.
- **Parameters**: `string json` – The string to validate.
- **Returns**: `bool` – `true` if the string is valid JSON; otherwise, `false`.
- **Throws**: No exceptions; returns `false` on any parsing error.

### PrettyPrint
Formats a compact JSON string into an indented, human-readable format.
- **Parameters**: `string json` – The compact JSON string.
- **Returns**: `string` – The formatted JSON string.
- **Throws**: `JsonException` if the input string is not valid JSON.

### Minify
Removes whitespace from a JSON string to reduce its size.
- **Parameters**: `string json` – The formatted JSON string.
- **Returns**: `string` – The minified JSON string.
- **Throws**: `JsonException` if the input string is not valid JSON.

### GetPropertyValue
Retrieves the value of a specific property from a JSON string by name.
- **Parameters**: `string json`, `string propertyName` – The JSON source and the property name.
- **Returns**: `object?` – The value of the property, or `null` if not found.
- **Throws**: `JsonException` if the JSON is invalid.

### CreateCustomOptions
Generates a `JsonSerializerOptions` instance pre-configured with project-specific defaults (e.g., case-insensitive property matching, specific converters).
- **Parameters**: None (or optional configuration flags depending on internal implementation).
- **Returns**: `JsonSerializerOptions` – A configured options object.
- **Throws**: No exceptions under normal circumstances.

## Usage

### Example 1: Safe Deserialization and Validation
This example demonstrates how to safely parse incoming tenant configuration data without risking application crashes due to malformed JSON, followed by validation.

```csharp
using DotNetTenantIsolation.Utilities;

public class TenantConfig
{
    public string TenantId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public void ProcessConfig(string rawJson)
{
    // Use DeserializeSafe to prevent crashes on bad input
    var config = JsonUtility.DeserializeSafe<TenantConfig>(rawJson);

    if (config == null)
    {
        // Fallback logic or logging
        Console.WriteLine("Invalid configuration received.");
        return;
    }

    if (JsonUtility.IsValidJson(rawJson))
    {
        var prettyJson = JsonUtility.PrettyPrint(rawJson);
        Console.WriteLine($"Processed config for tenant: {config.TenantId}");
    }
}
```

### Example 2: Merging Tenant Overrides
This example shows how to merge a base configuration object with a tenant-specific override object into a single dictionary for dynamic processing.

```csharp
using DotNetTenantIsolation.Utilities;

public class BaseSettings { public int Timeout { get; set; } = 30; public string Region { get; set; } = "US"; }
public class OverrideSettings { public int Timeout { get; set; } = 60; public bool Debug { get; set; } = true; }

public void ApplyOverrides()
{
    var baseConfig = new BaseSettings();
    var tenantOverrides = new OverrideSettings();

    // Merge both objects; tenantOverrides values will take precedence on conflicts
    var mergedData = JsonUtility.MergeJsonObjects<BaseSettings, OverrideSettings>(baseConfig, tenantOverrides);

    // Extract a specific value dynamically
    var effectiveTimeout = JsonUtility.ExtractValue(
        JsonUtility.Serialize(mergedData), 
        "Timeout"
    );

    Console.WriteLine($"Effective timeout: {effectiveTimeout}");
}
```

## Notes

- **Thread Safety**: As `JsonUtility` consists entirely of static methods utilizing stateless `System.Text.Json` APIs (or locally instantiated options), it is inherently thread-safe for concurrent read/write operations provided the input objects themselves are not being modified concurrently by other threads.
- **Null Handling**: Methods returning nullable types (`T?`, `object?`, `JsonElement?`) will return `null` rather than throwing for missing data or explicit JSON `null` values, with the exception of `Deserialize<T>`, which throws on structural mismatches.
- **Performance**: `DeserializeSafe<T>` incurs a slight performance overhead compared to `Deserialize<T>` due to internal try-catch blocks; it should be used primarily for untrusted input sources (e.g., external API payloads).
- **Dynamic Limitations**: When using `ConvertToDictionary` or `MergeJsonObjects`, complex nested objects may be flattened or represented as nested dictionaries depending on the internal recursion strategy. Circular references in input objects will cause serialization methods to throw.
- **Encoding**: All string operations assume UTF-8 encoding standard for JSON. Passing binary data or non-text strings will result in `JsonException`.
