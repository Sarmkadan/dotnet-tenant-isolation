#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenantIsolation.Utilities;

/// <summary>
/// Provides System.Text.Json serialization extensions for CryptographyUtility
/// Enables JSON serialization/deserialization of cryptographic utility operations
/// </summary>
public static class CryptographyUtilityJsonExtensions
{
    /// <summary>
    /// JSON serialization options with camelCase naming convention
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Converts the CryptographyUtility type to JSON string representation
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation for readability</param>
    /// <returns>JSON string representation of the CryptographyUtility type metadata</returns>
    public static string ToJson(bool indented = false)
    {
        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(typeof(CryptographyUtility), options);
    }

    /// <summary>
    /// Deserializes a JSON string into a CryptographyUtility type reference
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Deserialized Type object representing CryptographyUtility, or null if JSON is invalid</returns>
    /// <exception cref="ArgumentException">Thrown when json is null or empty</exception>
    public static Type? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<Type>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a CryptographyUtility type reference
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <param name="value">Output parameter containing the deserialized Type object, or null if deserialization fails</param>
    /// <returns>True if deserialization succeeds; otherwise, false</returns>
    /// <exception cref="ArgumentException">Thrown when json is null or empty</exception>
    public static bool TryFromJson(string json, out Type? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<Type>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}