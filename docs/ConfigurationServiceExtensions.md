# ConfigurationServiceExtensions

Static class that provides asynchronous extension methods for reading and writing tenant‑specific configuration values through an `IConfigurationService` instance.

## API

### GetBooleanAsync
**Purpose:** Asynchronously retrieves a boolean configuration value.  
**Parameters:**  
- `this IConfigurationService service` – the configuration service to query.  
- `string key` – the key whose value is to be read.  
- `bool defaultValue` (optional) – value returned when the key is missing or has a null value.  
- `CancellationToken cancellationToken` (optional) – token to observe for cancellation.  
**Return Value:** `Task<bool>` yielding the configured boolean or the supplied default.  
**Throws:**  
- `ArgumentNullException` if `service` or `key` is `null`.  
- `OperationCanceledException` if the cancellation token is triggered.  
- `InvalidCastException` if the stored value cannot be converted to `bool`.

### GetIntAsync
**Purpose:** Asynchronously retrieves an integer configuration value.  
**Parameters:** same pattern as `GetBooleanAsync` with an `int defaultValue`.  
**Return Value:** `Task<int>`.  
**Throws:**  
- `ArgumentNullException` for null `service` or `key`.  
- `OperationCanceledException` on cancellation.  
- `InvalidCastException` if the value cannot be converted to `int`.  
- `FormatException` if the stored string is not a valid integer.

### GetDoubleAsync
**Purpose:** Asynchronously retrieves a double‑precision floating‑point configuration value.  
**Parameters:** same pattern as `GetBooleanAsync` with a `double defaultValue`.  
**Return Value:** `Task<double>`.  
**Throws:**  
- `ArgumentNullException` for null `service` or `key`.  
- `OperationCanceledException` on cancellation.  
- `InvalidCastException` if the value cannot be converted to `double`.  
- `FormatException` if the stored string is not a valid floating‑point number.

### SetConfigurationAutoAsync
**Purpose:** Asynchronously writes a configuration entry, creating it if it does not exist, and returns the resulting `TenantConfiguration` object.  
**Parameters:**  
- `this IConfigurationService service` – the service to write to.  
- `string key` – the configuration key.  
- `object value` – the value to store (must be serializable by the underlying store).  
- `CancellationToken cancellationToken` (optional) – token to observe for cancellation.  
**Return Value:** `Task<TenantConfiguration>` representing the saved configuration entry.  
**Throws:**  
- `ArgumentNullException` if `service`, `key`, or `value` is `null`.  
- `OperationCanceledException` on cancellation.  
- `InvalidOperationException` if the underlying store is read‑only or rejects the write.

### HasValueAsync
**Purpose:** Asynchronously checks whether a configuration key exists and holds a non‑null value.  
**Parameters:**  
- `this IConfigurationService service` – the service to query.  
- `string key` – the key to test.  
- `CancellationToken cancellationToken` (optional) – token to observe for cancellation.  
**Return Value:** `Task<bool>` – `true` if the key has a value, otherwise `false`.  
**Throws:**  
- `ArgumentNullException` if `service` or `key` is `null`.  
- `OperationCanceledException` on cancellation.

### GetConfigurationOrThrowAsync
**Purpose:** Asynchronously retrieves the full `TenantConfiguration` object for a key, throwing if the key is missing.  
**Parameters:**  
- `this IConfigurationService service` – the service to query.  
- `string key` – the key whose configuration is desired.  
- `CancellationToken cancellationToken` (optional) – token to observe for cancellation.  
**Return Value:** `Task<TenantConfiguration>` containing the configuration for the key.  
**Throws:**  
- `ArgumentNullException` if `service` or `key` is `null`.  
- `OperationCanceledException` on cancellation.  
- `KeyNotFoundException` (or a store‑specific equivalent) if no configuration exists for the key.

### GetAsync<T>
**Purpose:** Generic asynchronous retrieval of a configuration value converted to type `T`.  
**Parameters:**  
- `this IConfigurationService service` – the service to query.  
- `string key` – the key to read.  
- `T defaultValue` (optional) – value to return when the key is missing or null.  
- `CancellationToken cancellationToken` (optional) – token to observe for cancellation.  
**Return Value:** `Task<T>` yielding the converted value or the supplied default.  
**Throws:**  
- `ArgumentNullException` if `service` or `key` is `null`.  
- `OperationCanceledException` on cancellation.  
- `InvalidCastException` if the stored value cannot be cast to `T`.  
- `FormatException` if parsing the stored value into `T` fails.

## Usage

**Example 1 – Reading a boolean flag with a fallback**

```csharp
using Microsoft.Extensions.Configuration;
using DotnetTenantIsolation.Extensions; // namespace containing ConfigurationServiceExtensions

public async Task<bool> IsFeatureEnabledAsync(IConfigurationService configService, string featureKey)
{
    return await configService.GetBooleanAsync(
        featureKey,
        defaultValue: false);
}
```

**Example 2 – Storing a numeric threshold and retrieving the saved record**

```csharp
public async Task<TenantConfiguration> SaveAndGetThresholdAsync(
    IConfigurationService configService,
    string thresholdKey,
    double threshold)
{
    await configService.SetConfigurationAutoAsync(thresholdKey, threshold);
    return await configService.GetConfigurationOrThrowAsync(thresholdKey);
}
```

## Notes

- All extension methods are safe to invoke concurrently from multiple threads; they do not introduce mutable shared state beyond the underlying configuration store, which must provide its own thread‑safety guarantees.  
- Supplying a `CancellationToken` allows the caller to abort the operation; if triggered, the methods throw `OperationCanceledException`.  
- Default value parameters are applied only when the key is absent or the stored value is `null`; they are not used to mask type‑conversion failures.  
- The generic `GetAsync<T>` depends on the store’s ability to serialize/deserialize the value; unsupported or malformed types result in `InvalidCastException` or `FormatException`.  
- `SetConfigurationAutoAsync` overwrites any existing entry for the same key; callers should verify uniqueness with `HasValueAsync` if overwriting is undesirable.  
- The `TenantConfiguration` instances returned by `GetConfigurationOrThrowAsync` and `SetConfigurationAutoAsync` are snapshots; subsequent changes to the underlying store will not update the returned object without a new call.
