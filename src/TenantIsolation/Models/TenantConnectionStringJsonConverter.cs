#nullable enable

using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenantIsolation.Models;

/// <summary>
/// Custom JSON converter for TenantConnectionString that redacts sensitive connection string details
/// </summary>
public class TenantConnectionStringJsonConverter : JsonConverter<TenantConnectionString>
{
    /// <summary>
    /// Reads and converts the JSON to a TenantConnectionString
    /// </summary>
    /// <param name="reader">The reader</param>
    /// <param name="typeToConvert">The type to convert</param>
    /// <param name="options">Serializer options</param>
    /// <returns>The deserialized TenantConnectionString</returns>
    public override TenantConnectionString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<TenantConnectionString>(ref reader, options);
    }

    /// <summary>
    /// Writes a TenantConnectionString to JSON with redacted connection string
    /// </summary>
    /// <param name="writer">The writer to write to</param>
    /// <param name="value">The value to serialize</param>
    /// <param name="options">Serializer options</param>
    public override void Write(Utf8JsonWriter writer, TenantConnectionString value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Create a redacted copy that excludes the sensitive ConnectionString property
        var redactedValue = new
        {
            value.Id,
            value.TenantId,
            value.DatabaseType,
            value.Name,
            value.SchemaName,
            value.DatabaseName,
            value.ServerHost,
            value.ServerPort,
            value.ConnectionTimeout,
            value.CommandTimeout,
            value.MaxPoolSize,
            value.UseConnectionPooling,
            value.IsPrimary,
            value.IsActive,
            value.CreatedAt,
            value.LastTestedAt,
            value.LastTestResult,
            HasConnectionString = !string.IsNullOrEmpty(value.ConnectionString)
        };

        JsonSerializer.Serialize(writer, redactedValue, options);
    }
}