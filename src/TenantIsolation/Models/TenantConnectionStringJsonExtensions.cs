#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenantIsolation.Models;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="TenantConnectionString"/>
/// </summary>
public static class TenantConnectionStringJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Serializes a <see cref="TenantConnectionString"/> instance to a JSON string
    /// </summary>
    /// <param name="value">The connection string to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>A JSON string representation of the connection string</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static string ToJson(this TenantConnectionString value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="TenantConnectionString"/> instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>The deserialized connection string, or null if the JSON is null or empty</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized</exception>
    public static TenantConnectionString? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<TenantConnectionString>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="TenantConnectionString"/> instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">Receives the deserialized connection string, or null if deserialization fails</param>
    /// <returns>True if deserialization succeeded; otherwise, false</returns>
    public static bool TryFromJson(string json, out TenantConnectionString? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<TenantConnectionString>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}