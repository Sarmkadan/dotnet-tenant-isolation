#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using System.Text;

namespace TenantIsolation.Utilities;

/// <summary>
/// Cryptographic utility for hashing, encryption, and security operations
/// Implements secure algorithms for data protection and identity verification
/// </summary>
public static class CryptographyUtility
{
    /// <summary>
    /// Generate SHA256 hash of string
    /// Used for password hashing, content verification, and data fingerprinting
    /// </summary>
    public static string GenerateSha256Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashedBytes);
    }

    /// <summary>
    /// Generate SHA512 hash of string (stronger than SHA256)
    /// </summary>
    public static string GenerateSha512Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using var sha512 = SHA512.Create();
        var hashedBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashedBytes);
    }

    /// <summary>
    /// Generate random secure token for API keys, tokens, and temporary credentials
    /// </summary>
    public static string GenerateSecureToken(int lengthInBytes = 32)
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenData = new byte[lengthInBytes];
        rng.GetBytes(tokenData);
        return Convert.ToBase64String(tokenData);
    }

    /// <summary>
    /// Generate HMAC-SHA256 signature for message authentication
    /// Used for webhook signing and API request verification
    /// </summary>
    public static string GenerateHmacSha256(string message, string secretKey)
    {
        if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(secretKey))
            return string.Empty;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Verify HMAC-SHA256 signature
    /// </summary>
    public static bool VerifyHmacSha256(string message, string signature, string secretKey)
    {
        var expectedSignature = GenerateHmacSha256(message, secretKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature)
        );
    }

    /// <summary>
    /// Generate cryptographically secure random string using alphanumeric characters
    /// Useful for passwords, verification codes, and temporary identifiers
    /// </summary>
    public static string GenerateRandomString(int length = 16, bool includeSpecialChars = false)
    {
        var characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        if (includeSpecialChars)
            characters += "!@#$%^&*()-_=+";

        using var rng = RandomNumberGenerator.Create();
        var result = new StringBuilder(length);
        var buffer = new byte[4];

        while (result.Length < length)
        {
            rng.GetBytes(buffer);
            var randomNumber = BitConverter.ToUInt32(buffer, 0) % characters.Length;
            result.Append(characters[(int)randomNumber]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Generate random numeric code (e.g., for OTP, verification codes)
    /// </summary>
    public static string GenerateRandomNumericCode(int length = 6)
    {
        using var rng = RandomNumberGenerator.Create();
        var result = new StringBuilder(length);
        var buffer = new byte[4];

        while (result.Length < length)
        {
            rng.GetBytes(buffer);
            var randomNumber = BitConverter.ToUInt32(buffer, 0) % 10;
            result.Append(randomNumber);
        }

        return result.ToString();
    }

    /// <summary>
    /// Encrypt string using AES-256
    /// </summary>
    public static string EncryptAes256(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(key))
            return string.Empty;

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var keyBytes = new byte[32];
        var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        Array.Copy(keyHash, keyBytes, 32);

        aes.Key = keyBytes;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs);

        sw.Write(plainText);
        sw.Flush();
        cs.FlushFinalBlock();

        var iv = aes.IV;
        var cipherText = ms.ToArray();
        var result = new byte[iv.Length + cipherText.Length];

        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(cipherText, 0, result, iv.Length, cipherText.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypt string using AES-256
    /// </summary>
    public static string DecryptAes256(string cipherText, string key)
    {
        if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(key))
            return string.Empty;

        var buffer = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var keyBytes = new byte[32];
        var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        Array.Copy(keyHash, keyBytes, 32);

        aes.Key = keyBytes;

        var iv = new byte[aes.IV.Length];
        Array.Copy(buffer, 0, iv, 0, iv.Length);
        aes.IV = iv;

        var cipherBytes = new byte[buffer.Length - iv.Length];
        Array.Copy(buffer, iv.Length, cipherBytes, 0, cipherBytes.Length);

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }

    /// <summary>
    /// Generate cryptographically secure UUID/GUID
    /// </summary>
    public static Guid GenerateSecureGuid()
    {
        return Guid.NewGuid();
    }

    /// <summary>
    /// Compute fingerprint hash from multiple data sources
    /// Useful for device identification and integrity checking
    /// </summary>
    public static string ComputeFingerprint(params string[] inputs)
    {
        var combined = string.Concat(inputs);
        return GenerateSha256Hash(combined);
    }

    /// <summary>
    /// Hash password with salt for secure storage
    /// Uses PBKDF2 for resistance against brute-force attacks
    /// </summary>
    public static (string Hash, string Salt) HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(20);

        var hashBytes = new byte[36];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 20);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(salt));
    }

    /// <summary>
    /// Verify password against hash
    /// </summary>
    public static bool VerifyPassword(string password, string hash, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
        var hash2 = pbkdf2.GetBytes(20);

        var hashBytes = Convert.FromBase64String(hash);
        return CryptographicOperations.FixedTimeEquals(
            hashBytes.AsSpan(16, 20),
            hash2
        );
    }
}
