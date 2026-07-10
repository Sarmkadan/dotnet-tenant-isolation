# CryptographyUtility

Utility class providing cryptographic helpers such as hashing, HMAC, random generation, symmetric encryption, and password handling. All members are static and stateless, making them safe to call from any thread without external synchronization.

## API

### GenerateSha256Hash
**Purpose:** Computes the SHA‑256 hash of the supplied input string.  
**Parameters:**  
- `input` (string): The text to hash. Must not be `null`.  
**Return value:** A hex‑encoded string representing the 32‑byte hash.  
**Exceptions:**  
- `ArgumentNullException` if `input` is `null`.  

### GenerateSha512Hash
**Purpose:** Computes the SHA‑512 hash of the supplied input string.  
**Parameters:**  
- `input` (string): The text to hash. Must not be `null`.  
**Return value:** A hex‑encoded string representing the 64‑byte hash.  
**Exceptions:**  
- `ArgumentNullException` if `input` is `null`.  

### GenerateSecureToken
**Purpose:** Generates a cryptographically strong random token suitable for use as a password reset token, API key, etc.  
**Parameters:**  
- `length` (int, optional): Number of bytes to generate; defaults to 32. Must be greater than zero.  
**Return value:** A URL‑safe Base64 string of the requested length.  
**Exceptions:**  
- `ArgumentOutOfRangeException` if `length` is less than or equal to zero.  

### GenerateHmacSha256
**Purpose:** Creates an HMAC‑SHA256 signature for the supplied data using the provided key.  
**Parameters:**  
- `data` (string): The data to sign. Must not be `null`.  
- `key` (string): The secret key used for the HMAC. Must not be `null` or empty.  
**Return value:** A Base64‑encoded string of the HMAC.  
**Exceptions:**  
- `ArgumentNullException` if `data` or `key` is `null`.  
- `ArgumentException` if `key` is empty.  

### VerifyHmacSha256
**Purpose:** Verifies that a supplied HMAC matches the recomputed HMAC for the given data and key.  
**Parameters:**  
- `data` (string): The original data. Must not be `null`.  
- `key` (string): The secret key. Must not be `null` or empty.  
- `hmac` (string): The Base64‑encoded HMAC to verify. Must not be `null`.  
**Return value:** `true` if the HMAC is valid; otherwise `false`.  
**Exceptions:**  
- `ArgumentNullException` if any parameter is `null`.  
- `ArgumentException` if `key` is empty.  

### GenerateRandomString
**Purpose:** Produces a random string of the specified length using a configurable character set.  
**Parameters:**  
- `length` (int): Desired length of the output string; must be greater than zero.  
- `includeSpecial` (bool, optional): If `true`, the string may include punctuation characters; defaults to `false`.  
**Return value:** A random string of the requested length.  
**Exceptions:**  
- `ArgumentOutOfRangeException` if `length` is less than or equal to zero.  

### GenerateRandomNumericCode
**Purpose:** Generates a random numeric code (digits only) of the specified length.  
**Parameters:**  
- `length` (int): Number of digits to generate; must be greater than zero.  
**Return value:** A string consisting solely of digits `0‑9`.  
**Exceptions:**  
- `ArgumentOutOfRangeException` if `length` is less than or equal to zero.  

### EncryptAes256
**Purpose:** Encrypts a plain‑text string using AES‑256 in CBC mode with a random IV.  
**Parameters:**  
- `plainText` (string): The text to encrypt. Must not be `null`.  
- `key` (string): The Base64‑encoded 32‑byte AES key. Must not be `null` or empty.  
- `iv` (string, optional): The Base64‑encoded 16‑byte initialization vector. If omitted, a random IV is generated and prepended to the ciphertext.  
**Return value:** A Base64‑encoded string containing the IV (if provided) followed by the ciphertext.  
**Exceptions:**  
- `ArgumentNullException` if `plainText` or `key` is `null`.  
- `ArgumentException` if `key` is empty or not a valid Base64 32‑byte key.  
- `CryptographicException` if the underlying AES operation fails.  

### DecryptAes256
**Purpose:** Decrypts a string produced by `EncryptAes256`.  
**Parameters:**  
- `cipherText` (string): The Base64‑encoded output from `EncryptAes256`. Must not be `null`.  
- `key` (string): The Base64‑encoded 32‑byte AES key used for encryption. Must not be `null` or empty.  
**Return value:** The original plain‑text string.  
**Exceptions:**  
- `ArgumentNullException` if `cipherText` or `key` is `null`.  
- `ArgumentException` if `key` is empty or not a valid Base64 32‑byte key.  
- `CryptographicException` if decryption fails (e.g., incorrect key or corrupted data).  

### GenerateSecureGuid
**Purpose:** Creates a GUID using a cryptographically strong random number generator.  
**Parameters:** None.  
**Return value:** A `Guid` value with random bits sourced from a secure RNG.  
**Exceptions:** None.  

### ComputeFingerprint
**Purpose:** Computes a compact fingerprint (SHA‑256 hash) of the input string, useful for identifying data without revealing its content.  
**Parameters:**  
- `input` (string): The data to fingerprint. Must not be `null`.  
**Return value:** A hex‑encoded SHA‑256 hash string.  
**Exceptions:**  
- `ArgumentNullException` if `input` is `null`.  

### HashPassword
**Purpose:** Securely hashes a password with a random salt using PBKDF2‑HMAC‑SHA256 (100 000 iterations).  
**Parameters:**  
- `password` (string): The password to hash. Must not be `null`.  
**Return value:** A value tuple `(string Hash, string Salt)` where both components are Base64‑encoded.  
**Exceptions:**  
- `ArgumentNullException` if `password` is `null`.  

### VerifyPassword
**Purpose:** Verifies a supplied password against a previously stored hash and salt.  
**Parameters:**  
- `password` (string): The password to test. Must not be `null`.  
- `hash` (string): The Base64‑encoded hash produced by `HashPassword`. Must not be `null`.  
- `salt` (string): The Base64‑encoded salt produced by `HashPassword`. Must not be `null`.  
**Return value:** `true` if the password matches the hash; otherwise `false`.  
**Exceptions:**  
- `ArgumentNullException` if any parameter is `null`.  

## Usage

```csharp
// Example 1: Hash a password and later verify it
var (hash, salt) = CryptographyUtility.HashPassword("CorrectHorseBatteryStaple");
bool isValid = CryptographyUtility.VerifyPassword("CorrectHorseBatteryStaple", hash, salt);
// isValid == true

// Example 2: Generate a secure token and encrypt a message with AES‑256
string token = CryptographyUtility.GenerateSecureToken(); // e.g., "X7z9Q2..."
string plainText = "Confidential data";
string key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // 32‑byte key
string iv = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)); // 16‑byte IV
string cipherText = CryptographyUtility.EncryptAes256(plainText, key, iv);
string recovered = CryptographyUtility.DecryptAes256(cipherText, key, iv);
// recovered == plainText
```

## Notes

- All methods validate their arguments and throw `ArgumentNullException` for any `null` reference where a value is required.  
- Length‑based methods (`GenerateSecureToken`, `GenerateRandomString`, `GenerateRandomNumericCode`) throw `ArgumentOutOfRangeException` when the requested length is less than or equal to zero.  
- Cryptographic operations that rely on external keys (`EncryptAes256`, `DecryptAes256`, `GenerateHmacSha256`, `VerifyHmacSha256`) will throw `CryptographicException` if the key material is malformed or the underlying algorithm fails.  
- The class contains no static fields; each method operates solely on its inputs and uses thread‑safe random number generators (`RandomNumberGenerator`). Consequently, all members are thread‑safe and can be invoked concurrently without additional synchronization.  
- When using `EncryptAes256`/`DecryptAes256`, the caller is responsible for securely managing the AES key and IV; re‑using an IV with the same key compromises confidentiality.  
- The password hashing performed by `HashPassword`/`VerifyPassword` uses a fixed iteration count (100 000). If a different work factor is required, the caller must implement a custom PBKDF2 routine.  
- Outputs from hashing and HMAC functions are hex‑ or Base64‑encoded strings; consumers should treat them as opaque values and avoid attempting to interpret the raw bytes.
