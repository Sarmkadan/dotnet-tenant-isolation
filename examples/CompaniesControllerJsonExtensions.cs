#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TenantIsolation.Examples;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for CompaniesController.
/// </summary>
public static class CompaniesControllerJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the CompaniesController instance to a JSON string.
    /// </summary>
    /// <param name="value">The controller instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the controller.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this CompaniesController value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a CompaniesController instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A CompaniesController instance populated from the JSON data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static CompaniesController? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<CompaniesController>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a CompaniesController instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized CompaniesController instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out CompaniesController? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            value = JsonSerializer.Deserialize<CompaniesController>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}