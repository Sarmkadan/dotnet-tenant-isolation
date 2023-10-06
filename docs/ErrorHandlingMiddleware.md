# ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` is a reusable HTTP middleware component designed to centralize and standardize error handling in ASP.NET Core applications. It intercepts exceptions thrown during request processing, converts them into consistent JSON error responses, and enriches them with diagnostic information such as trace identifiers, timestamps, and contextual details. This middleware is particularly useful in multi-tenant isolation scenarios where consistent error formatting across tenants improves maintainability and debugging.

## API

### `public ErrorHandlingMiddleware(RequestDelegate next)`

Initializes a new instance of the `ErrorHandlingMiddleware` with the specified request delegate.

- **Parameters**
  - `next` – The `RequestDelegate` representing the next middleware in the pipeline.
- **Remarks**
  - Throws `ArgumentNullException` if `next` is `null`.

---

### `public async Task InvokeAsync(HttpContext context)`

Invokes the middleware to process an HTTP request.

- **Parameters**
  - `context` – The `HttpContext` for the current HTTP request.
- **Return Value**
  - A `Task` representing the asynchronous operation.
- **Remarks**
  - Catches and handles exceptions thrown during request processing.
  - Sets the HTTP response status code, content type, and body based on the caught exception.
  - Throws `ArgumentNullException` if `context` is `null`.

---

### `public string Code`

Gets the standardized error code associated with the current error response.

- **Return Value**
  - A non-null `string` representing the error code (e.g., `"INVALID_INPUT"`, `"RESOURCE_NOT_FOUND"`).
- **Remarks**
  - This property is populated based on the type of exception caught or explicitly set during error construction.

---

### `public string Message`

Gets the human-readable error message associated with the current error response.

- **Return Value**
  - A non-null `string` containing a descriptive error message.
- **Remarks**
  - This property is typically derived from the exception message or a predefined message template.

---
### `public int StatusCode`

Gets the HTTP status code to be returned in the error response.

- **Return Value**
  - An `int` representing the HTTP status code (e.g., `400`, `404`, `500`).
- **Remarks**
  - This value is mapped from the exception type or explicitly configured during error construction.

---
### `public string? TraceId`

Gets the trace identifier associated with the current HTTP request.

- **Return Value**
  - An optional `string` containing the trace identifier, or `null` if not available.
- **Remarks**
  - This value is typically derived from `HttpContext.TraceIdentifier`.

---
### `public string? Details`

Gets additional diagnostic details about the error.

- **Return Value**
  - An optional `string` containing extended error details, or `null` if not applicable.
- **Remarks**
  - This property may include stack traces, inner exception messages, or other contextual information in development environments.

---
### `public DateTime Timestamp`

Gets the UTC timestamp when the error was captured.

- **Return Value**
  - A `DateTime` representing the moment the error was recorded.
- **Remarks**
  - This value is set at the time of exception interception and is always in UTC.

## Usage

### Example 1: Basic Integration in ASP.NET Core Pipeline

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseMiddleware<ErrorHandlingMiddleware>();

    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

### Example 2: Custom Error Response with Additional Context

```csharp
public class CustomErrorController : ControllerBase
{
    [HttpGet("resource/{id}")]
    public IActionResult GetResource(int id)
    {
        try
        {
            var resource = _repository.GetById(id);
            if (resource == null)
            {
                throw new ResourceNotFoundException($"Resource with ID {id} not found.");
            }
            return Ok(resource);
        }
        catch (ResourceNotFoundException ex)
        {
            var error = new ErrorHandlingMiddleware(null)
            {
                Code = "RESOURCE_NOT_FOUND",
                Message = ex.Message,
                StatusCode = StatusCodes.Status404NotFound,
                Details = $"Requested ID: {id}"
            };
            throw new Exception("Custom error wrapped", ex);
        }
    }
}
```

## Notes

- **Thread Safety**: The `ErrorHandlingMiddleware` is stateless and thread-safe. All public members are read-only or immutable after construction, and the `InvokeAsync` method does not modify shared state.
- **Exception Handling**: The middleware catches exceptions but does not suppress them unless explicitly rethrown. It ensures that the HTTP response reflects the error while allowing global exception handlers or other middleware to further process the exception.
- **TraceId Availability**: The `TraceId` depends on the `HttpContext.TraceIdentifier`, which may be `null` in some hosting environments or early pipeline stages. Always check for `null` when using this property.
- **Details Field**: The `Details` field may contain sensitive information in development environments. Ensure proper sanitization or disable detailed output in production.
- **Timestamp Precision**: The `Timestamp` is recorded at the moment the exception is intercepted, which may slightly precede the actual response being sent.
