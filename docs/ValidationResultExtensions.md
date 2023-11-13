# ValidationResultExtensions
The `ValidationResultExtensions` class provides a set of extension methods for working with `ValidationResult` objects, allowing for the combination, analysis, and manipulation of validation results. These methods enable developers to easily merge validation results, check for errors and warnings, and log or throw exceptions based on the validation outcome.

## API
* `Combine`: Combines two `ValidationResult` objects into a single result. This method has two overloads: one that takes two `ValidationResult` parameters and another that takes a `ValidationResult` and a `params ValidationResult[]` parameter. Both return a `ValidationResult` object.
* `AddError`: Adds an error message to a `ValidationResult` object. This method takes a `ValidationResult` and a `string` parameter.
* `AddWarning`: Adds a warning message to a `ValidationResult` object. This method takes a `ValidationResult` and a `string` parameter.
* `HasErrors`: Checks if a `ValidationResult` object contains any error messages. This method takes a `ValidationResult` parameter and returns a `bool` value.
* `HasWarnings`: Checks if a `ValidationResult` object contains any warning messages. This method takes a `ValidationResult` parameter and returns a `bool` value.
* `GetFirstError`: Retrieves the first error message from a `ValidationResult` object. This method takes a `ValidationResult` parameter and returns a `string?` value.
* `GetFirstWarning`: Retrieves the first warning message from a `ValidationResult` object. This method takes a `ValidationResult` parameter and returns a `string?` value.
* `Log`: Logs a `ValidationResult` object. This method takes a `ValidationResult` parameter.
* `ThrowIfInvalid`: Throws an exception if a `ValidationResult` object is invalid. This method takes a `ValidationResult` parameter.
* `Merge`: Merges a `ValidationResult` object with another `ValidationResult` object. This method takes two `ValidationResult` parameters.

## Usage
The following examples demonstrate how to use the `ValidationResultExtensions` class:
```csharp
// Example 1: Combining validation results
ValidationResult result1 = new ValidationResult();
result1.AddError("Error message 1");
ValidationResult result2 = new ValidationResult();
result2.AddWarning("Warning message 1");
ValidationResult combinedResult = ValidationResultExtensions.Combine(result1, result2);
Console.WriteLine(ValidationResultExtensions.HasErrors(combinedResult));  // Output: True
Console.WriteLine(ValidationResultExtensions.HasWarnings(combinedResult));  // Output: True

// Example 2: Logging and throwing exceptions
ValidationResult result3 = new ValidationResult();
result3.AddError("Error message 2");
ValidationResultExtensions.Log(result3);
try
{
    ValidationResultExtensions.ThrowIfInvalid(result3);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);  // Output: Error message 2
}
```

## Notes
When using the `ValidationResultExtensions` class, note that the `Combine` method will merge all error and warning messages from the input `ValidationResult` objects. The `HasErrors` and `HasWarnings` methods will return `true` if at least one error or warning message is present in the `ValidationResult` object, respectively. The `GetFirstError` and `GetFirstWarning` methods will return `null` if no error or warning messages are present. The `Log` method will log the entire `ValidationResult` object, including all error and warning messages. The `ThrowIfInvalid` method will throw an exception if the `ValidationResult` object contains any error messages. The `Merge` method will merge the two input `ValidationResult` objects, preserving all error and warning messages. The `ValidationResultExtensions` class is thread-safe, as all methods only access and manipulate the input `ValidationResult` objects without modifying any shared state.
