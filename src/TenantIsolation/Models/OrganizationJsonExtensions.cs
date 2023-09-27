using System;
using System.Text.Json;

namespace TenantIsolation.Models
{
    public static class OrganizationJsonExtensions
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string ToJson(this Organization value, bool indented = false)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var options = new JsonSerializerOptions(_options)
            {
                WriteIndented = indented
            };

            return JsonSerializer.Serialize(value, options);
        }

        public static Organization? FromJson(string json)
        {
            if (json is null) throw new ArgumentNullException(nameof(json));

            return JsonSerializer.Deserialize<Organization>(json, _options);
        }

        public static bool TryFromJson(string json, out Organization? value)
        {
            try
            {
                value = FromJson(json);
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
