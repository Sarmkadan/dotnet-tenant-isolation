# ApiResponse

The `ApiResponse<T>` type is a generic wrapper used throughout the `dotnet-tenant-isolation` project to standardize the shape of HTTP API responses. It encapsulates success state, payload data, optional error details, metadata such as timestamps and tracing identifiers, and pagination information when applicable. By providing factory methods and a service registration helper, it encourages consistent response formatting across controllers and middleware.

## API

### Success (property)
- **Purpose**: Indicates whether the operation represented by the response was successful.
- **Type**: `bool`
- **Remarks**: Set to `true` for successful outcomes; `false` otherwise. Consumers should inspect this property before accessing `Data` or `Items`.

### Data (property)
- **Purpose**: Holds the primary payload of the response when the operation succeeds.
- **Type**: `T?` (nullable to allow value types to represent absence of data)
- **Remarks**: May be `null` even when `Success` is `true` if the operation intentionally returns no content.

### Message (property)
- **Purpose**: Provides a human‑readable description of the outcome.
- **Type**: `string?`
- **Remarks**: Typically set for both success and error responses; may be `null` when no message is needed.

### Errors (property)
- **Purpose**: Contains validation or business‑rule errors keyed by field or identifier.
- **Type**: `Dictionary<string, string[]>?`
- **Remarks**: Each key maps to an array of error messages. Populated only when `Success` is `false` and the failure stems from invalid input.

### Timestamp (property)
- **Purpose**: Records the UTC date and time when the response instance was created.
- **Type**: `DateTime`
- **Remarks**: Automatically initialized to `DateTime.UtcNow` in the constructor; should not be modified after creation.

### Path (property)
- **Purpose**: Stores the request path associated with the response.
- **Type**: `string?`
- **Remarks**: Useful for logging and debugging; may be `null` if the response is generated outside of an HTTP context.

### TraceId (property)
- **Purpose**: Holds a correlation identifier for tracing the request across services.
- **Type**: `string?`
- **Remarks**: Often populated by middleware; may be `null` when tracing is disabled.

### Items (property)
- **Purpose**: Contains a collection of payload items for list‑style endpoints.
- **Type**: `List<T>`
- **Remarks**: Used alongside `Total`, `Page`, and `PageSize` for paginated results. Empty list when no items are present.

### Total (property)
- **Purpose**: Indicates the total number of items available across all pages.
- **Type**: `int`
- **Remarks**: Meaningful only when `Items` is populated for a pagated query; otherwise defaults to `0`.

### Page (property)
- **Purpose**: Specifies the current page number in a pagated result set.
- **Type**: `int`
- **Remarks**: One‑based indexing; defaults to `1`.

### PageSize (property)
- **Purpose**: Defines the maximum number of items per page in a pagated result set.
- **Type**: `int`
- **Remarks**: Defaults to a sensible server‑defined value (e.g., `20`) if not explicitly set.

### ResponseFormatter (property)
- **Purpose**: Provides a delegate that can transform the response into a specific output format (e.g., JSON, XML).
- **Type**: `ResponseFormatter`
- **Remarks**: Set via dependency injection; if `null`, the framework uses a default formatter.

### Success<T> (method)
```csharp
public static ApiResponse<T> Success<T>(T data, string? message = null)
```
- **Purpose**: Creates a successful response containing a single data item.
- **Parameters**:
  - `data`: The payload to include in the `Data` property.
  - `message`: Optional descriptive message.
- **Return Value**: An `ApiResponse<T>` instance with `Success` set to `true`.
- **Exceptions**: None.

### Success (method)
```csharp
public static ApiResponse<object?> Success(object? data, string? message = null)
```
- **Purpose**: Creates a successful response with an untyped payload.
- **Parameters**:
  - `data`: The payload (can be `null`).
  - `message`: Optional descriptive message.
- **Return Value**: An `ApiResponse<object?>` instance with `Success` set to `true`.
- **Exceptions**: None.

### Error (method)
```csharp
public static ApiResponse<object?> Error(string message, Dictionary<string, string[]>? errors = null)
```
- **Purpose**: Creates an error response.
- **Parameters**:
  - `message`: A summary of the error.
  - `errors`: Optional dictionary of field‑specific validation errors.
- **Return Value**: An `ApiResponse<object?>` instance with `Success` set to `false`.
- **Exceptions**: Throws `ArgumentNullException` if `message` is `null`.

### Paginated<T> (method)
```csharp
public static ApiResponse<PaginatedResponse<T>> Paginated<T>(
    IEnumerable<T> items,
    int total,
    int page,
    int pageSize)
```
- **Purpose**: Creates a successful paginated response.
- **Parameters**:
  - `items`: The collection of items for the current page.
  - `total`: Total number of items available across all pages.
  - `page`: The current page number (one‑based).
  - `pageSize`: Number of items per page.
- **Return Value**: An `ApiResponse<PaginatedResponse<T>>` where the `Data` property holds a `PaginatedResponse<T>` containing `Items`, `Total`, `Page`, and `PageSize`.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if `page` or `pageSize` is less than `1`, or if `total` is negative.

### AddResponseFormatter (extension method)
```csharp
public static IServiceCollection AddResponseFormatter(
    this IServiceCollection services,
    ResponseFormatter formatter)
```
- **Purpose**: Registers a custom `ResponseFormatter` delegate with the DI container for use by `ApiResponse` instances.
- **Parameters**:
  - `services`: The service collection to modify.
  - `formatter`: The delegate that formats an `ApiResponse` into the desired output.
- **Return Value**: The same `IServiceCollection` instance to allow chaining.
- **Exceptions**: Throws `ArgumentNullException` if either `services` or `formatter` is `null`.

## Usage

### Example 1: Simple success response
```csharp
using DotnetTenantIsolation.Api;

public IActionResult GetProduct(int id)
{
    var product = _productRepository.GetById(id);
    if (product == null)
        return NotFound();

    var response = ApiResponse<Product>.Success(product, "Product retrieved.");
    return Ok(response);
}
```

### Example 2: Error response with validation details
```csharp
using DotnetTenantIsolation.Api;

public IActionResult CreateOrder([FromBody] OrderDto dto)
{
    var validationErrors = new Dictionary<string, string[]>
    {
        { "Quantity", new[] { "Quantity must be greater than zero." } },
        { "CustomerId", new[] { "CustomerId is required." } }
    };

    var response = ApiResponse<object?>.Error(
        "Order creation failed due to validation errors.",
        validationErrors);

    return BadRequest(response);
}
```

## Notes

- All instance properties are mutable; concurrent modification of the same `ApiResponse` instance from multiple threads is not thread‑safe. Typically each instance is confined to a single request scope, eliminating concurrency concerns.
- Static factory methods (`Success`, `Error`, `Paginated<T>`, `AddResponseFormatter`) are pure and thread‑safe; they rely only on their input parameters and do not access shared state.
- The `Timestamp` property is set once during object construction and should not be altered afterward; modifying it after creation may lead to inconsistent audit trails.
- When using the paginated factory, ensure that the `items` enumerable contains exactly the number of elements indicated by `pageSize` for the current page, unless it is the final page where fewer items are permissible.
- The `ResponseFormatter` delegate, if supplied, is invoked by the framework’s response middleware; exceptions thrown within the formatter will propagate according to the middleware’s error handling policy. Implementations should be stateless or otherwise thread‑safe if shared across requests.
