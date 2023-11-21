using System;
using System.Text.Json;

namespace TenantIsolation.Models
{
	/// <summary>
	/// Provides JSON serialization and deserialization extensions for <see cref="Organization"/> objects.
	/// </summary>
	public static class OrganizationJsonExtensions
	{
		private static readonly JsonSerializerOptions _options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		/// <summary>
		/// Serializes an <see cref="Organization"/> instance to a JSON string.
		/// </summary>
		/// <param name="value">The organization to serialize.</param>
		/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
		/// <returns>A JSON string representation of the organization.</returns>
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
		/// Deserializes an <see cref="Organization"/> instance from a JSON string.
		/// </summary>
		/// <param name="json">The JSON string to deserialize.</param>
		/// <returns>The deserialized organization, or <see langword="null"/> if the JSON represents a null value.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
		/// <exception cref="JsonException">The JSON is invalid or cannot be deserialized to an <see cref="Organization"/>.</exception>
		public static Organization? FromJson(string json)
		{
			ArgumentNullException.ThrowIfNull(json);

			return JsonSerializer.Deserialize<Organization>(json, _options);
		}

		/// <summary>
		/// Attempts to deserialize an <see cref="Organization"/> instance from a JSON string.
		/// </summary>
		/// <param name="json">The JSON string to deserialize.</param>
		/// <param name="value">Receives the deserialized organization if successful; otherwise, <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
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