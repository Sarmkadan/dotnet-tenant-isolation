using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenantIsolation.Models
{
    public static class TenantJsonExtensions
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Serialize enums as camel‑case strings if any are present
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <summary>
        /// Serialises the <see cref="Tenant"/> instance to JSON.
        /// </summary>
        /// <param name="value">The tenant to serialise.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the tenant.</returns>
        public static string ToJson(this Tenant value, bool indented = false)
        {
            var options = indented
                ? new JsonSerializerOptions(_options) { WriteIndented = true }
                : _options;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserialises a JSON string into a <see cref="Tenant"/> instance.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>The deserialised tenant, or <c>null</c> if the JSON represents a null value.</returns>
        public static Tenant? FromJson(string json)
        {
            return JsonSerializer.Deserialize<Tenant>(json, _options);
        }

        /// <summary>
        /// Tries to deserialise a JSON string into a <see cref="Tenant"/> instance.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="value">When the method returns, contains the deserialised tenant if successful; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
        public static bool TryFromJson(string json, out Tenant? value)
        {
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
