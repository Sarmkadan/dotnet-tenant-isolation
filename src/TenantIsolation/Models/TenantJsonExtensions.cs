using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenantIsolation.Models
{
    /// <summary>
    /// Provides JSON serialization and deserialization extensions for <see cref="Tenant"/> instances.
    /// </summary>
    public static class TenantJsonExtensions
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Serialize enums as camel-case strings if any are present
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <summary>
        /// Serializes the <see cref="Tenant"/> instance to JSON.
        /// </summary>
        /// <param name="value">The tenant to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the tenant.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        public static string ToJson(this Tenant value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_options) { WriteIndented = true }
                : _options;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string into a <see cref="Tenant"/> instance.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>The deserialized tenant, or <c>null</c> if the JSON represents a null value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
        public static Tenant? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            return JsonSerializer.Deserialize<Tenant>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string into a <see cref="Tenant"/> instance.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="value">When the method returns, contains the deserialized tenant if successful; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
        public static bool TryFromJson(string json, out Tenant? value)
        {
            ArgumentNullException.ThrowIfNull(json);

            try
            {
                value = JsonSerializer.Deserialize<Tenant>(json, _options);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }
}