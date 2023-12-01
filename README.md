// existing content ...

## ValidationResultExtensions

The `ValidationResultExtensions` class provides a set of extension methods for working with `ValidationResult` objects, making it easier to combine, validate, and handle validation results in a more fluent and expressive way.

### Example Usage

```csharp
var result1 = new ValidationResult
{
    Errors = new List<string> { "Error 1", "Error 2" },
    Warnings = new List<string> { "Warning 1" }
};

var result2 = new ValidationResult
{
    Errors = new List<string> { "Error 3" },
    Warnings = new List<string> { "Warning 2", "Warning 3" }
};

var combinedResult = ValidationResultExtensions.Combine(result1, result2);

if (combinedResult.HasErrors)
{
    Console.WriteLine("Errors:");
    foreach (var error in combinedResult.Errors)
    {
        Console.WriteLine(error);
    }
}

if (combinedResult.HasWarnings)
{
    Console.WriteLine("Warnings:");
    foreach (var warning in combinedResult.Warnings)
    {
        Console.WriteLine(warning);
    }
}

// Output:
// Errors:
// Error 1
// Error 2
// Error 3
// Warnings:
// Warning 1
// Warning 2
// Warning 3
```

// existing content ...
