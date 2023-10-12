#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenantIsolation.Benchmarks;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="FeatureToggleBenchmarks"/>.
/// </summary>
public static class FeatureToggleBenchmarksJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Serialize enums as camel-case strings if any are present
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serialises the <see cref="FeatureToggleBenchmarks"/> instance to JSON.
    /// </summary>
    /// <param name="value">The feature toggle benchmarks to serialise.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the feature toggle benchmarks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static string ToJson(this FeatureToggleBenchmarks value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_options) { WriteIndented = true }
            : _options;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserialises a JSON string into a <see cref="FeatureToggleBenchmarks"/> instance.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>The deserialised feature toggle benchmarks, or <c>null</c> if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
    public static FeatureToggleBenchmarks? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<FeatureToggleBenchmarks>(json, _options);
    }

    /// <summary>
    /// Tries to deserialise a JSON string into a <see cref="FeatureToggleBenchmarks"/> instance.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <param name="value">When the method returns, contains the deserialised feature toggle benchmarks if successful; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
    public static bool TryFromJson(string json, out FeatureToggleBenchmarks? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            value = JsonSerializer.Deserialize<FeatureToggleBenchmarks>(json, _options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
