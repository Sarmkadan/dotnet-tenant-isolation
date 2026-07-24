using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TenantIsolation.Utilities;

/// <summary>
/// Extension methods for <see cref="WebhookPayload"/> that provide common helper functionality.
/// </summary>
namespace TenantIsolation.Integration
{
    /// <summary>
    /// Provides utility extensions for <see cref="WebhookPayload"/>.
    /// </summary>
    public static class WebhookPayloadExtensions
    {
        /// <summary>
        /// Generates HMAC-SHA256 signature for the webhook payload using the provided secret.
        /// </summary>
        /// <param name="payload">The webhook payload to sign.</param>
        /// <param name="secret">The secret key used to compute the HMAC.</param>
        /// <returns>The hex-encoded HMAC-SHA256 signature.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> or <paramref name="secret"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="secret"/> is an empty string.</exception>
        public static string GenerateSignature(this WebhookPayload payload, string secret)
        {
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentException.ThrowIfNullOrEmpty(secret);

            var message = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
            return CryptographyUtility.GenerateHmacSha256(message, secret);
        }
        /// <summary>
        /// Validates the payload's <c>Signature</c> using the supplied secret.
        /// The signature is expected to be a hex‑encoded HMAC‑SHA256 of the JSON-serialized payload.
        /// </summary>
        /// <param name="payload">The webhook payload to validate.</param>
        /// <param name="secret">The secret key used to compute the HMAC.</param>
        /// <returns><c>true</c> if the computed signature matches the payload's <c>Signature</c>; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> or <paramref name="secret"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="secret"/> is an empty string.</exception>
        public static bool ValidateSignature(this WebhookPayload payload, string secret)
        {
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentException.ThrowIfNullOrEmpty(secret);

            var message = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
            return CryptographyUtility.VerifyHmacSha256(message, payload.Signature, secret);
        }

        /// <summary>
        /// Serializes the <c>Data</c> property of the payload to a JSON string.
        /// </summary>
        /// <param name="payload">The webhook payload whose data should be serialized.</param>
        /// <returns>A JSON representation of <c>Data</c>, or the JSON literal <c>null</c> if <c>Data</c> is <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is <c>null</c>.</exception>
        public static string ToJson(this WebhookPayload payload)
        {
            ArgumentNullException.ThrowIfNull(payload);

            return payload.Data switch
            {
                null => "null",
                _ => JsonSerializer.Serialize(payload.Data, new JsonSerializerOptions { WriteIndented = false })
            };
        }

        /// <summary>
        /// Determines whether the payload's <c>Timestamp</c> is within the specified maximum age relative to the current UTC time.
        /// </summary>
        /// <param name="payload">The webhook payload to evaluate.</param>
        /// <param name="maxAge">The maximum allowed age.</param>
        /// <returns><c>true</c> if the payload is newer than <paramref name="maxAge"/>; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is <c>null</c>.</exception>
        public static bool IsRecent(this WebhookPayload payload, TimeSpan maxAge)
        {
            ArgumentNullException.ThrowIfNull(payload);
            return DateTime.UtcNow - payload.Timestamp <= maxAge;
        }

        /// <summary>
        /// Generates a concise, human-readable summary of the webhook event.
        /// </summary>
        /// <param name="payload">The webhook payload to summarize.</param>
        /// <returns>A string containing the event type, identifier and timestamp formatted with invariant culture.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is <c>null</c>.</exception>
        public static string GetEventSummary(this WebhookPayload payload)
        {
            ArgumentNullException.ThrowIfNull(payload);

            return $"{payload.EventType} (ID: {payload.EventId}) @ {payload.Timestamp:O}";
        }
    }
}
