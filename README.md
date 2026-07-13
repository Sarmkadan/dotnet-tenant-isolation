// existing content ...

## TenantIsolationException

The `TenantIsolationException` class represents a custom exception that can be thrown when a tenant isolation-related error occurs. It provides additional information about the error, including an error code and error details.

### Example Usage

```csharp
try
{
    // Code that may throw a TenantIsolationException
}
catch (TenantIsolationException ex)
{
    Console.WriteLine($"Error Code: {ex.ErrorCode}");
    Console.WriteLine($"Error Details: {JsonUtility.Serialize(ex.ErrorDetails)}");
    Console.WriteLine(ex.ToString());
}
```

// existing content ...
