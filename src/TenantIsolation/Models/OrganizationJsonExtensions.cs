using System;
using System.Text.Json;

namespace TenantIsolation.Models
{
	/// <summary>
	/// Provides extension methods for JSON serialization and deserialization of <see cref="Organization"/> instances.
	/// </summary>
	public static class OrganizationJsonExtensions
	{
		private static readonly JsonSerializerOptions _options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		/// <summary>
		/// Serializes the supplied <see cref="Organization"/> instance to a JSON string using camel‑case property naming.
		/// </summary>
		/// <param name="value">The <see cref="Organization"/> instance to serialize. Must not be <see langword="null"/>.</param>
		/// <param name="indented">When <see langword="true"/>, the output JSON is formatted with indentation for readability; otherwise it is compact.</param>
		/// <returns>A JSON string representation of the <paramref name="value"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
		public static string ToJson(this Organization value, bool indented = false)
		{
			ArgumentNullException.ThrowIfNull(value);

			var options = new JsonSerializerOptions(_options)
			{
				WriteIndented = indented
			};

			return JsonSerializer.Serialize(value, options);
		}

		/// <summary>
		/// Deserializes a JSON string into an <see cref="Organization"/> object using the predefined camel‑case serializer options.
		/// </summary>
		/// <param name="json">A JSON string that represents an <see cref="Organization"/>. Must not be <see langword="null"/>.</param>
		/// <returns>The deserialized <see cref="Organization"/> instance, or <see langword="null"/> if the JSON represents a null value.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
		/// <exception cref="JsonException">The JSON is invalid or cannot be deserialized to an <see cref="Organization"/>.</exception>
		public static Organization? FromJson(string json)
		{
			ArgumentNullException.ThrowIfNull(json);

			return JsonSerializer.Deserialize<Organization>(json, _options);
		}

		/// <summary>
		/// Attempts to deserialize a JSON string into an <see cref="Organization"/> instance without propagating exceptions.
		/// </summary>
		/// <param name="json">The JSON string to parse. Must not be <see langword="null"/>.</param>
		/// <param name="value">
		/// When the method returns <see langword="true"/>, receives the deserialized <see cref="Organization"/>; otherwise <see langword="null"/>.
		/// </param>
		/// <returns><see langword="true"/> if deserialization succeeds; otherwise <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
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
