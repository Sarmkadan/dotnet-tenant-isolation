# TenantIsolationExceptionValidation

`TenantIsolationExceptionValidation` is a static utility class designed to validate tenant isolation constraints within the `dotnet-tenant-isolation` framework. It provides methods and properties to assess whether a given context or configuration adheres to tenant isolation rules, returning validation errors or enforcing validity through exceptions. This class is typically used during application startup or request processing to ensure multi-tenancy boundaries are respected.

## API

### Validate

```csharp
public static IReadOnlyList<string> Validate()
```

**Purpose**: Returns a list of validation error messages describing why tenant isolation constraints are violated.  
**Parameters**: None.  
**Return Value**: An `IReadOnlyList<string>` containing error messages. Returns an empty list if validation passes.  
**Exceptions**: Does not throw; always returns a result.

---

### IsValid

```csharp
public static bool IsValid { get; }
```

**Purpose**: Indicates whether the current tenant isolation configuration is valid.  
**Parameters**: None.  
**Return Value**: `true` if no validation errors exist; `false` otherwise.  
**Exceptions**: Does not throw.

---

### EnsureValid

```csharp
public static void EnsureValid()
```

**Purpose**: Throws an exception if tenant isolation validation fails.  
**Parameters**: None.  
**Return Value**: `void`.  
**Exceptions**: Throws `TenantIsolationException` if `Validate()` returns any errors.

---

### Validate (Overload)

```csharp
public static IReadOnlyList<string> Validate(object context)
```

**Purpose**: Validates tenant isolation rules against a provided context object.  
**Parameters**:  
- `context` (object): The object to validate for tenant isolation compliance.  
**Return Value**: An `IReadOnlyList<string>` of error messages.  
**Exceptions**: Does not throw.

---

### IsValid (Overload)

```csharp
public static bool IsValid(object context)
```

**Purpose**: Checks if the provided context satisfies tenant isolation rules.  
**Parameters**:  
- `context` (object): The object to validate.  
**Return Value**: `true` if valid; `false` otherwise.  
**Exceptions**: Does not throw.

---

### EnsureValid (Overload)

```csharp
public static void EnsureValid(object context)
```

**Purpose**: Throws an exception if the provided context fails tenant isolation validation.  
**Parameters**:  
- `context` (object): The object to validate.  
**Return Value**: `void`.  
**Exceptions**: Throws `TenantIsolationException` if validation fails.

---

### Validate (Generic Overload)

```csharp
public static IReadOnlyList<string> Validate<T>(T context)
```

**Purpose**: Validates tenant isolation rules for a strongly-typed context.  
**Parameters**:  
- `context` (T): The typed object to validate.  
**Return Value**: An `IReadOnlyList<string>` of error messages.  
**Exceptions**: Does not throw.

---

### IsValid (Generic Overload)

```csharp
public static bool IsValid<T>(T context)
```

**Purpose**: Checks if a strongly-typed context meets tenant isolation requirements.  
**Parameters**:  
- `context` (T): The typed object to validate.  
**Return Value**: `true` if valid; `false` otherwise.  
**Exceptions**: Does not throw.

---

### EnsureValid (Generic Overload)

```csharp
public static void EnsureValid<T>(T context)
```

**Purpose**: Throws an exception if the strongly-typed context fails validation.  
**Parameters**:  
- `context` (T): The typed object to validate.  
**Return Value**: `void`.  
**Exceptions**: Throws `TenantIsolationException` if validation fails.

---

## Usage

### Example 1: Basic Validation Check

```csharp
// Validate tenant isolation configuration at startup
var errors = TenantIsolationExceptionValidation.Validate();
if (!TenantIsolationExceptionValidation.IsValid)
{
    Console.WriteLine("Tenant isolation configuration is invalid:");
    foreach (var error in errors)
    {
        Console.WriteLine($"- {error}");
    }
}
```

### Example 2: Context-Specific Validation

```csharp
// Validate a specific request context
var requestContext = new { TenantId = "tenant1", ResourceId = "resource42" };
TenantIsolationExceptionValidation.EnsureValid(requestContext);
// Proceed with request handling if no exception is thrown
```

## Notes

- All members are static and do not maintain instance state. Thread safety depends on the implementation of underlying validation logic.
- `Validate()` overloads may exhibit different behavior based on the type of `context` provided. Ensure appropriate overloads are used for specific scenarios.
- `EnsureValid()` methods will throw `TenantIsolationException` immediately upon validation failure, making them suitable for guard clauses.
- Empty error lists from `Validate()` indicate successful validation and should not be interpreted as exceptions.
- Generic overloads (`Validate<T>`, `IsValid<T>`, `EnsureValid<T>`) provide compile-time type safety and may include specialized validation rules for known types.
