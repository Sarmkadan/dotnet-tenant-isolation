# ValidationResult

`ValidationResult` is a utility class used for collecting and reporting validation outcomes during application configuration validation. It aggregates errors and warnings, provides methods to add validation messages, and offers integration points with the .NET dependency injection and middleware systems to enforce configuration correctness at startup.

## API

### `public bool IsValid`
Gets a value indicating whether there are no errors in the validation result.
**Returns:** `true` if `Errors` is empty; otherwise, `false`.

### `public List<string> Errors`
Gets the collection of error messages collected during validation.
**Remarks:** This list is mutable; modifications affect the validation state.

### `public List<string> Warnings`
Gets the collection of warning messages collected during validation.
**Remarks:** This list is mutable; modifications affect the validation state.

### `public void AddError(string error)`
Adds an error message to the validation result.
**Parameters:**
- `error`: The error message to add. If `null`, the message is ignored.

### `public void AddWarning(string warning)`
Adds a warning message to the validation result.
**Parameters:**
- `warning`: The warning message to add. If `null`, the message is ignored.

### `public ConfigurationValidator ConfigurationValidator`
Gets the associated `ConfigurationValidator` instance used to perform validation.
**Remarks:** This property is set during validation and may be `null` if not initialized.

### `public ValidationResult Validate(Action<ValidationResult> action)`
Validates a configuration section using a custom validation action.
**Parameters:**
- `action`: A delegate that receives the current `ValidationResult` and performs validation logic.
**Returns:** The same `ValidationResult` instance for method chaining.
**Throws:** `ArgumentNullException` if `action` is `null`.

### `public ValidationResult ValidateSection(IConfigurationSection section)`
Validates a configuration section and aggregates results.
**Parameters:**
- `section`: The configuration section to validate. If `null`, no validation is performed.
**Returns:** The same `ValidationResult` instance for method chaining.

### `public void ValidateAndThrow()`
Throws an `InvalidOperationException` if any errors are present.
**Throws:** `InvalidOperationException` with a message aggregating all errors if `IsValid` is `false`.

### `public static IServiceCollection AddConfigurationValidator(IServiceCollection services)`
Registers a `ConfigurationValidator` in the dependency injection container.
**Parameters:**
- `services`: The `IServiceCollection` to extend.
**Returns:** The same `IServiceCollection` for method chaining.
**Throws:** `ArgumentNullException` if `services` is `null`.

### `public static IApplicationBuilder ValidateConfigurationOnStartup(IApplicationBuilder app)`
Ensures configuration validation runs when the application starts.
**Parameters:**
- `app`: The `IApplicationBuilder` instance.
**Returns:** The same `IApplicationBuilder` for method chaining.
**Throws:** `ArgumentNullException` if `app` is `null`.

## Usage

### Example 1: Basic Validation with Custom Rules
```csharp
var validationResult = new ValidationResult();
validationResult.Validate(result =>
{
    if (string.IsNullOrEmpty(config["ApiKey"]))
    {
        result.AddError("API key is required.");
    }
});

if (!validationResult.IsValid)
{
    validationResult.ValidateAndThrow();
}
```

### Example 2: Integration with ASP.NET Core
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register validator
builder.Services.AddConfigurationValidator();

// Validate configuration on startup
var app = builder.Build();
app.ValidateConfigurationOnStartup();
```

## Notes

- **Thread Safety:** `ValidationResult` is not thread-safe. Concurrent modifications to `Errors` or `Warnings` may lead to inconsistent states. External synchronization is required if used across threads.
- **Null Handling:** Methods like `AddError` and `AddWarning` silently ignore `null` inputs; no exceptions are thrown for null messages.
- **Validation Chaining:** Methods such as `Validate` and `ValidateSection` return the instance for fluent chaining, but do not alter thread safety guarantees.
- **Startup Validation:** `ValidateConfigurationOnStartup` integrates with the ASP.NET Core pipeline and should be called early in the application lifecycle to fail fast if configuration is invalid.
