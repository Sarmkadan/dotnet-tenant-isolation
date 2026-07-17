#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenantIsolation.Utilities;

/// <summary>
/// Provides System.Text.Json serialization extensions for CryptographyUtility operation results
/// Enables JSON serialization/deserialization of cryptographic hashes, tokens, and encrypted data
/// </summary>
public static class CryptographyUtilityJsonExtensions
{
    /// <summary>
    /// JSON serialization options with camelCase naming convention
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Serializes a cryptographic hash result to JSON string
    /// </summary>
    /// <param name="hash">The cryptographic hash string to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability</param>
    /// <returns>JSON string representation of the hash</returns>
    /// <exception cref="ArgumentNullException">Thrown when hash is null</exception>
    public static string ToJson(this string hash, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(hash);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(hash, options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a cryptographic hash
    /// </summary>
    /// <param name="json">JSON string containing the hash to deserialize</param>
    /// <param name="hash">Output parameter containing the deserialized hash string, or null if deserialization fails</param>
    /// <returns>True if deserialization succeeds; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when json is null</exception>
    public static bool TryFromJson(string json, out string? hash)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            hash = JsonSerializer.Deserialize<string>(json, _jsonOptions);
            return hash != null;
        }
        catch (JsonException)
        {
            hash = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes a secure token result to JSON string
    /// </summary>
    /// <param name="token">The secure token string to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability</param>
    /// <returns>JSON string representation of the token</returns>
    /// <exception cref="ArgumentNullException">Thrown when token is null</exception>
    public static string ToSecureTokenJson(this string token, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(token);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(token, options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a secure token
    /// </summary>
    /// <param name="json">JSON string containing the token to deserialize</param>
    /// <param name="token">Output parameter containing the deserialized token string, or null if deserialization fails</param>
    /// <returns>True if deserialization succeeds; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when json is null</exception>
    public static bool TryFromSecureTokenJson(string json, out string? token)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            token = JsonSerializer.Deserialize<string>(json, _jsonOptions);
            return token != null;
        }
        catch (JsonException)
        {
            token = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes encrypted data to JSON string
    /// </summary>
    /// <param name="encryptedData">The encrypted data string (Base64 encoded) to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability</param>
    /// <returns>JSON string representation of the encrypted data</returns>
    /// <exception cref="ArgumentNullException">Thrown when encryptedData is null</exception>
    public static string ToEncryptedDataJson(this string encryptedData, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(encryptedData, options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into encrypted data
    /// </summary>
    /// <param name="json">JSON string containing the encrypted data to deserialize</param>
    /// <param name="encryptedData">Output parameter containing the deserialized encrypted data string, or null if deserialization fails</param>
    /// <returns>True if deserialization succeeds; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when json is null</exception>
    public static bool TryFromEncryptedDataJson(string json, out string? encryptedData)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            encryptedData = JsonSerializer.Deserialize<string>(json, _jsonOptions);
            return encryptedData != null;
        }
        catch (JsonException)
        {
            encryptedData = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes a password hash and salt tuple to JSON string
    /// </summary>
    /// <param name="passwordHash">The password hash tuple containing hash and salt</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability</param>
    /// <returns>JSON string representation of the password hash tuple</returns>
    /// <exception cref="ArgumentNullException">Thrown when passwordHash is null</exception>
    public static string ToPasswordHashJson(this (string Hash, string Salt) passwordHash, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(passwordHash.Hash);
        ArgumentNullException.ThrowIfNull(passwordHash.Salt);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(passwordHash, options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a password hash and salt tuple
    /// </summary>
    /// <param name="json">JSON string containing the password hash tuple to deserialize</param>
    /// <param name="passwordHash">Output parameter containing the deserialized password hash tuple</param>
    /// <returns>True if deserialization succeeds; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when json is null</exception>
    public static bool TryFromPasswordHashJson(string json, out (string Hash, string Salt)? passwordHash)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            passwordHash = JsonSerializer.Deserialize<(string Hash, string Salt)>(json, _jsonOptions);
            return passwordHash.HasValue;
        }
        catch (JsonException)
        {
            passwordHash = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes a GUID to JSON string
    /// </summary>
    /// <param name="guid">The GUID to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability</param>
    /// <returns>JSON string representation of the GUID</returns>
    public static string ToGuidJson(this Guid guid, bool indented = false)
    {
        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(guid, options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a GUID
    /// </summary>
    /// <param name="json">JSON string containing the GUID to deserialize</param>
    /// <param name="guid">Output parameter containing the deserialized GUID</param>
    /// <returns>True if deserialization succeeds; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when json is null</exception>
    public static bool TryFromGuidJson(string json, out Guid guid)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            guid = JsonSerializer.Deserialize<Guid>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            guid = Guid.Empty;
            return false;
        }
    }
}