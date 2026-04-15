#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenantIsolation.Utilities;

/// <summary>
/// JSON serialization and deserialization utility
/// Provides consistent JSON handling across the framework with custom converters
/// </summary>
public static class JsonUtility
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Serialize object to JSON string
    /// </summary>
    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, DefaultOptions);
    }

    /// <summary>
    /// Serialize object to pretty JSON string with indentation
    /// Used for logging and API responses for readability
    /// </summary>
    public static string SerializePretty<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, PrettyOptions);
    }

    /// <summary>
    /// Deserialize JSON string to object
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Safely deserialize JSON string, returning null on failure
    /// </summary>
    public static T? DeserializeSafe<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Deserialize JSON to dynamic object
    /// </summary>
    public static JsonElement? DeserializeToDynamic(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(json, DefaultOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Convert object to dictionary
    /// Useful for dynamic JSON manipulation and parameter passing
    /// </summary>
    public static Dictionary<string, object?> ConvertToDictionary<T>(T obj)
    {
        var json = Serialize(obj);
        var element = DeserializeToDynamic(json);

        var result = new Dictionary<string, object?>();
        if (element?.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.Value.EnumerateObject())
            {
                result[property.Name] = ExtractValue(property.Value);
            }
        }

        return result;
    }

    /// <summary>
    /// Merge two JSON objects
    /// </summary>
    public static Dictionary<string, object?> MergeJsonObjects<T1, T2>(T1 obj1, T2 obj2)
    {
        var dict1 = ConvertToDictionary(obj1);
        var dict2 = ConvertToDictionary(obj2);

        foreach (var kvp in dict2)
        {
            dict1[kvp.Key] = kvp.Value;
        }

        return dict1;
    }

    /// <summary>
    /// Get value from JSON element
    /// </summary>
    public static object? ExtractValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => ConvertJsonElementToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ExtractValue).ToList(),
            _ => element.ToString()
        };
    }

    /// <summary>
    /// Convert JSON element to dictionary
    /// </summary>
    private static Dictionary<string, object?> ConvertJsonElementToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object?>();
        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ExtractValue(property.Value);
        }
        return result;
    }

    /// <summary>
    /// Validate if string is valid JSON
    /// </summary>
    public static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            JsonSerializer.Deserialize<JsonElement>(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Pretty print JSON string
    /// </summary>
    public static string PrettyPrint(string json)
    {
        if (!IsValidJson(json))
            return json;

        var element = JsonSerializer.Deserialize<JsonElement>(json);
        return JsonSerializer.Serialize(element, PrettyOptions);
    }

    /// <summary>
    /// Minify JSON string by removing whitespace
    /// </summary>
    public static string Minify(string json)
    {
        if (!IsValidJson(json))
            return json;

        var element = JsonSerializer.Deserialize<JsonElement>(json);
        return JsonSerializer.Serialize(element, DefaultOptions);
    }

    /// <summary>
    /// Get property value from JSON string
    /// </summary>
    public static object? GetPropertyValue(string json, string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(propertyPath))
            return null;

        var element = JsonSerializer.Deserialize<JsonElement>(json);
        if (element?.ValueKind != JsonValueKind.Object)
            return null;

        var parts = propertyPath.Split('.');
        var current = element;

        foreach (var part in parts)
        {
            if (current?.ValueKind != JsonValueKind.Object)
                return null;

            if (!current.Value.TryGetProperty(part, out var next))
                return null;

            current = next;
        }

        return ExtractValue(current.Value);
    }

    /// <summary>
    /// Create custom serializer options for specific use cases
    /// </summary>
    public static JsonSerializerOptions CreateCustomOptions(bool indented = false, bool ignoreNulls = true)
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented,
            DefaultIgnoreCondition = ignoreNulls ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
    }
}
