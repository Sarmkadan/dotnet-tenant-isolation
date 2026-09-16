# CryptographyUtility

`CryptographyUtility` is a static collection of string-oriented hashing, message-authentication, random-value, AES encryption, fingerprinting, and password-hashing helpers. It lives in the `TenantIsolation.Utilities` namespace and has no dependency-injection setup or shared mutable state.

The methods are low-level helpers. Callers remain responsible for key storage, secret rotation, choosing an appropriate password policy, and deciding whether an authenticated encryption format is required.

## Hashing and fingerprints

### `GenerateSha256Hash(string input)`

Hashes the UTF-8 representation of `input` with SHA-256 and returns 64 uppercase hexadecimal characters. A null input throws `ArgumentNullException`; an empty string returns `string.Empty` rather than the SHA-256 digest of an empty byte sequence.

### `GenerateSha512Hash(string input)`

Hashes the UTF-8 representation of `input` with SHA-512 and returns 128 uppercase hexadecimal characters. Its null and empty-input behavior is the same as `GenerateSha256Hash`.

These unsalted, fast hashes are appropriate for deterministic content checks and non-secret identifiers. They are not suitable for password storage or for authenticating data from an untrusted source. Use `HashPassword` for the utility's password format and HMAC when a shared secret must protect message integrity.

### `ComputeFingerprint(params string[] inputs)`

Concatenates all input strings without separators and returns their SHA-256 hash. The array must be non-null and contain at least one item; otherwise the method throws `ArgumentNullException` or `ArgumentException`, respectively.

Because values are joined without length markers or delimiters, different input sequences can produce the same combined text: `("ab", "c")` and `("a", "bc")` have the same fingerprint. Use this helper only when the input boundaries are already unambiguous, or encode the boundaries before calling it. Null elements are treated as empty strings by `string.Concat`.

```csharp
using TenantIsolation.Utilities;

var contentHash = CryptographyUtility.GenerateSha256Hash(documentText);
var fingerprint = CryptographyUtility.ComputeFingerprint(
    $"{tenantId.Length}:{tenantId}",
    $"{resourceId.Length}:{resourceId}");
```

## HMAC message authentication

`GenerateHmacSha256(string message, string secretKey)` computes HMAC-SHA256 over the UTF-8 message with the UTF-8 secret and returns a 64-character uppercase hexadecimal signature. It returns an empty string when either argument is empty and throws `ArgumentNullException` for null arguments.

`VerifyHmacSha256(string message, string signature, string secretKey)` recomputes that value and compares the UTF-8 bytes of the two hexadecimal strings with `CryptographicOperations.FixedTimeEquals`. The signature must therefore use exactly the uppercase hexadecimal representation returned by `GenerateHmacSha256`; lowercase hex and alternate encodings do not match. A null argument throws `ArgumentNullException`, and an empty signature throws `ArgumentException`.

Use these methods to authenticate a payload shared by systems that use this exact UTF-8 and uppercase-hex convention, such as the library's webhook signing path:

```csharp
var signature = CryptographyUtility.GenerateHmacSha256(payloadJson, webhookSecret);

if (!CryptographyUtility.VerifyHmacSha256(payloadJson, signature, webhookSecret))
{
    throw new InvalidOperationException("The payload signature is invalid.");
}
```

Keep `secretKey` outside source control and tenant data, scope it to its purpose, and rotate it through the application's secret-management system. Reject missing or empty secrets before using these helpers; an empty secret causes generation to return an empty string rather than a usable signature.

## Random values

| Method | Output | Default | Intended use |
| --- | --- | --- | --- |
| `GenerateSecureToken(int lengthInBytes = 32)` | Standard Base64 encoding of random bytes | 32 bytes, producing 44 Base64 characters including padding | Opaque API, reset, or temporary credential material |
| `GenerateRandomString(int length = 16, bool includeSpecialChars = false)` | Letters and digits, optionally plus `!@#$%^&*()-_=+` | 16 characters, no special characters | Human-entered temporary values where that alphabet is acceptable |
| `GenerateRandomNumericCode(int length = 6)` | Decimal digits | 6 digits | Short verification or one-time codes |
| `GenerateSecureGuid()` | `Guid.NewGuid()` | N/A | Random unique identifiers, not bearer secrets |

The three length arguments must be greater than zero and throw `ArgumentOutOfRangeException` otherwise. `GenerateSecureToken` uses `RandomNumberGenerator` directly. The string and numeric helpers also draw from `RandomNumberGenerator`, but map random 32-bit values with modulo arithmetic, which introduces a small distribution bias; use the byte token helper when uniformly distributed secret material matters. Standard Base64 can contain `+`, `/`, and `=` and is not inherently URL-safe.

## AES encryption

`EncryptAes256(string plainText, string key)` encrypts text using AES with a 256-bit key, CBC mode, and PKCS#7 padding. The supplied `key` is not parsed as raw or Base64 key material. Instead, the method hashes the UTF-8 key string with SHA-256 and uses the resulting 32 bytes as the AES key.

Each call generates a random 16-byte initialization vector (IV). The returned value is standard Base64 encoding of this binary layout:

```text
16-byte IV | AES-CBC ciphertext
```

`DecryptAes256(string cipherText, string key)` Base64-decodes that layout, reads the first 16 bytes as the IV, derives the key in the same way, and returns the decrypted UTF-8 text.

```csharp
var encrypted = CryptographyUtility.EncryptAes256(tenantSecret, encryptionSecret);
var decrypted = CryptographyUtility.DecryptAes256(encrypted, encryptionSecret);
```

Null arguments throw `ArgumentNullException`. Either method returns `string.Empty` if either supplied string is empty. Decryption can also throw `FormatException`, `ArgumentException`, or `CryptographicException` when the encoded value is malformed, truncated, corrupted, or decrypted with the wrong key.

This format provides confidentiality but not authenticity: AES-CBC does not detect all ciphertext modification, and the payload contains no authentication tag. Use it only when compatibility with this utility's existing format is required and the ciphertext is protected from tampering by another trusted layer. For new security-sensitive formats, prefer an authenticated-encryption design such as AES-GCM with explicit versioning and independently managed random key material. Never use tenant IDs, usernames, or other predictable values as `key`.

## Password hashing

`HashPassword(string password)` creates a random 16-byte salt, derives 20 bytes with PBKDF2-HMAC-SHA256 using 10,000 iterations, and returns `(Hash, Salt)` as Base64 strings. `Hash` decodes to 36 bytes containing the same 16-byte salt followed by the 20-byte derived value. `Salt` is also returned separately because `VerifyPassword` expects it as a separate argument.

`VerifyPassword(string password, string hash, string salt)` decodes the supplied salt, repeats the 10,000-iteration derivation, and compares the derived value with bytes 16 through 35 of the decoded hash using a fixed-time comparison.

```csharp
var (hash, salt) = CryptographyUtility.HashPassword(password);

// Persist both strings exactly as returned.
var matches = CryptographyUtility.VerifyPassword(candidatePassword, hash, salt);
```

All arguments must be non-null and non-empty. Invalid Base64, an incorrectly sized hash, or other malformed stored values can throw rather than return `false`, so treat stored hash data as a validated format and handle corruption at the storage boundary.

The iteration count and output layout are fixed by the implementation and are primarily useful for reading and writing this library's format. They cannot be upgraded through method parameters. Applications with current password-hardening requirements should use their platform's password hasher or identity system, which can encode algorithm parameters and migrate work factors over time.

## Operational guidance

- Treat tokens, HMAC keys, AES keys, password hashes, and salts as tenant-scoped data. Never allow one tenant's key material or stored credentials to be selected using an unverified tenant identifier.
- Store AES and HMAC secrets in a secret manager, not alongside ciphertext or in application configuration committed to source control.
- Preserve output encodings exactly. Hashes and HMACs are uppercase hex; AES values, tokens, password hashes, and salts use standard Base64.
- Do not log plaintext secrets, encryption keys, bearer tokens, passwords, password hashes, or salts.
- Catch decoding or cryptographic exceptions at trust boundaries when ciphertext or stored password data can be malformed.
- None of the helpers perform key rotation, version payloads, expire tokens, rate-limit code attempts, or erase managed strings from memory. Those responsibilities belong to the calling application.
