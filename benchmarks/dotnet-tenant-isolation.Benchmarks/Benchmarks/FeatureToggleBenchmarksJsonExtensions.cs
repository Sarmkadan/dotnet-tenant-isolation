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
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serializes the <see cref="FeatureToggleBenchmarks"/> instance to JSON.
    /// </summary>
    /// <param name="value">The feature toggle benchmarks to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the feature toggle benchmarks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this FeatureToggleBenchmarks value, bool indented = false)
        => JsonSerializer.Serialize(value, indented ? new JsonSerializerOptions(_options) { WriteIndented = true } : _options);

    /// <summary>
    /// Deserializes a JSON string into a <see cref="FeatureToggleBenchmarks"/> instance.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>The deserialized feature toggle benchmarks, or <see langword="null"/> if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static FeatureToggleBenchmarks? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<FeatureToggleBenchmarks>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="FeatureToggleBenchmarks"/> instance.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <param name="value">When the method returns, contains the deserialized feature toggle benchmarks if successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
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
