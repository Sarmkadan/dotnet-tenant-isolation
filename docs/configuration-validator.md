# ConfigurationValidator

`ConfigurationValidator` checks a small set of application configuration values and reports configuration errors and warnings before the application begins serving requests. It reads directly from `IConfiguration`; it does not validate a bound `TenantIsolationOptions` instance.

Validation runs at startup only when the application calls `ValidateConfigurationOnStartup()`. Registering the validator with `AddConfigurationValidator()` makes it available through dependency injection but does not run it by itself.

## Startup setup

Register the validator while configuring services, then invoke startup validation after building the application:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfigurationValidator();

var app = builder.Build();
app.ValidateConfigurationOnStartup();
```

`AddConfigurationValidator()` registers `IConfigurationValidator` as a scoped service. `ValidateConfigurationOnStartup()` creates a scope, resolves that service, calls `ValidateAndThrow()`, disposes the scope, and returns the same `IApplicationBuilder`.

## Settings checked by `Validate()`

The full validation pass checks the following keys:

| Configuration key or section | Check | Result when the check fails |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | Must contain a non-whitespace value. | Error: `Connection string 'DefaultConnection' is not configured` |
| `ConnectionStrings:DefaultConnection` | A present value must contain at least 10 characters. | Error: `Connection string appears to be invalid or incomplete` |
| `TenantIsolation` | Section existence. | Warning: `TenantIsolation configuration section not found. Using defaults.`; its child checks are skipped. |
| `TenantIsolation:AutoMigrate` | Must be present and non-empty when the section exists. | Warning: `AutoMigrate setting not configured` |
| `TenantIsolation:EnableAuditLogging` | Must be present and non-empty when the section exists. | Warning: `EnableAuditLogging setting not configured` |
| `TenantIsolation:EnableSoftDeleteFilter` | Must be present and non-empty when the section exists. | Warning: `EnableSoftDeleteFilter setting not configured` |
| `Features` | Section existence. | Warning: `Features configuration section not found`; its child checks are skipped. |
| `Features:EnableWebhooks` | Must be present and non-empty when the section exists. | Warning: `Feature flag 'EnableWebhooks' not configured` |
| `Features:EnableCaching` | Must be present and non-empty when the section exists. | Warning: `Feature flag 'EnableCaching' not configured` |
| `Features:EnableEventBus` | Must be present and non-empty when the section exists. | Warning: `Feature flag 'EnableEventBus' not configured` |
| `Integration` | Section existence. | Warning: `Integration configuration section not found`; URL checks are skipped. |
| `Integration:WebhookUrl` | If non-empty, must be a well-formed absolute URI. | Error: `Invalid webhook URL: {value}` |
| `Integration:ExternalApiUrl` | If non-empty, must be a well-formed absolute URI. | Error: `Invalid external API URL: {value}` |

The validator checks only the conditions shown above. In particular:

- `AutoMigrate`, `EnableAuditLogging`, `EnableSoftDeleteFilter`, and the three feature flags are not parsed as Boolean values. Any non-empty string passes this validator.
- Integration URLs are optional. A missing or empty URL does not produce a message when the `Integration` section exists.
- URL validation accepts any well-formed absolute URI; it does not restrict the scheme to HTTP or HTTPS or test endpoint reachability.
- The connection string receives only the presence and minimum-length checks. The validator does not parse it or attempt a database connection.

After all checks, `IsValid` is set to `true` exactly when `Errors` is empty. Warnings do not make the result invalid.

## Validation APIs and failure modes

### `Validate()`

`Validate()` returns a `ValidationResult` containing all errors and warnings from the full set of checks. It does not throw for an invalid setting. It logs the start of validation and then logs either success or the number of errors. A warning-only result is therefore valid and receives the success log.

### `ValidateAndThrow()`

`ValidateAndThrow()` performs the full validation pass. If any errors were collected, it throws `InvalidOperationException`; the exception message starts with `Configuration validation failed:` and contains every error separated by the platform newline. Warnings are not included in that exception.

When there are no errors, the method returns normally. If warnings were collected, it logs their count at warning level.

Because `ValidateConfigurationOnStartup()` calls this method synchronously, an error prevents normal application startup at the point where the extension is invoked. Missing optional sections or settings produce warnings and do not stop startup.

### `ValidateSection(string sectionName)`

This method performs only an existence check for the named configuration section; it does not run the setting-specific rules described above.

- A null, empty, or whitespace-only `sectionName` throws `ArgumentException` with parameter name `sectionName`.
- A missing section returns an invalid result with `Configuration section '{sectionName}' not found` in `Errors`.
- An existing section returns a valid result with no errors or warnings.

### Construction, registration, and resolution failures

Constructing `ConfigurationValidator` with a null `IConfiguration` or logger throws `ArgumentNullException`. Passing null to either `AddConfigurationValidator()` or `ValidateConfigurationOnStartup()` also throws `ArgumentNullException`.

Startup validation resolves `IConfigurationValidator` with `GetRequiredService`. If it has not been registered, dependency injection throws `InvalidOperationException` before configuration checks run. Other exceptions raised by the configuration provider, logger, scope creation, or service resolution are not caught by the validator and propagate to the caller.

## Example configuration

The following configuration satisfies every check without warnings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TenantDb;"
  },
  "TenantIsolation": {
    "AutoMigrate": true,
    "EnableAuditLogging": true,
    "EnableSoftDeleteFilter": true
  },
  "Features": {
    "EnableWebhooks": true,
    "EnableCaching": true,
    "EnableEventBus": true
  },
  "Integration": {
    "WebhookUrl": "https://example.com/webhooks",
    "ExternalApiUrl": "https://api.example.com"
  }
}
```

The validator does not guarantee that these services are usable; it catches only the missing, short, and malformed values documented above.
