# ApiCallResult

`ApiCallResult` is a generic result wrapper for external API calls in the `dotnet-tenant-isolation` library. It encapsulates the outcome of an HTTP operation, including success status, deserialized response data, error details, the HTTP status code, and the elapsed time of the call. The companion class `ExternalApiClient` provides typed methods (`GetAsync`, `PostAsync`, `PutAsync`, `DeleteAsync`) that return `ApiCallResult<T>`, while the static `AddExternalApiClient` extension registers the client in the dependency injection container.

## API

### `bool IsSuccess`

Indicates whether the API call completed successfully. This is `true` when the HTTP response has a success status code (typically 2xx) and deserialization succeeds; otherwise `false`.

### `T? Data`

The deserialized response payload when `IsSuccess` is `true`. If the call fails or the response body is empty, this property is `null`. The type `T` is determined by the generic argument passed to the calling method.

### `string? ErrorMessage`

A human-readable error description when `IsSuccess` is `false`. This may contain the server’s error response body, a deserialization failure message, or the exception message if a network-level error occurred. Returns `null` on success.

### `int? HttpStatusCode`

The HTTP status code returned by the external service, if a response was received. This is `null` when the request fails before receiving a response (e.g., timeout, DNS resolution failure, or cancellation).

### `TimeSpan Duration`

The total elapsed time between sending the request and receiving (or failing to receive) the final response. Includes time spent on redirects, retries, and deserialization.

### `ExternalApiClient`

The client class that houses the typed HTTP methods. It is configured via `AddExternalApiClient` and injected into consuming services. This class is not generic itself; the generic methods on it produce `ApiCallResult<T>` instances.

### `async Task<ApiCallResult<T>> GetAsync<T>(string endpoint, CancellationToken ct = default)`

Sends an HTTP GET request to the specified relative `endpoint`. The response body is deserialized to `T`. Returns `ApiCallResult<T>` where `IsSuccess` is `true` only if the status code indicates success and deserialization completes without error. Throws `ArgumentNullException` when `endpoint` is null or whitespace. Cancellation via `ct` throws `OperationCanceledException`.

### `async Task<ApiCallResult<T>> PostAsync<T>(string endpoint, object? body, CancellationToken ct = default)`

Sends an HTTP POST request with a JSON-serialized `body` to the relative `endpoint`. Deserializes the response to `T`. Returns `ApiCallResult<T>` with the same success criteria as `GetAsync`. Throws `ArgumentNullException` when `endpoint` is null or whitespace. Cancellation via `ct` throws `OperationCanceledException`.

### `async Task<ApiCallResult<T>> PutAsync<T>(string endpoint, object? body, CancellationToken ct = default)`

Sends an HTTP PUT request with a JSON-serialized `body` to the relative `endpoint`. Deserializes the response to `T`. Returns `ApiCallResult<T>` with the same success criteria as `GetAsync`. Throws `ArgumentNullException` when `endpoint` is null or whitespace. Cancellation via `ct` throws `OperationCanceledException`.

### `async Task<ApiCallResult<bool>> DeleteAsync(string endpoint, CancellationToken ct = default)`

Sends an HTTP DELETE request to the relative `endpoint`. Returns `ApiCallResult<bool>` where `Data` is `true` when the response status code indicates success, and `false` otherwise. This method does not deserialize a response body; it relies solely on the status code. Throws `ArgumentNullException` when `endpoint` is null or whitespace. Cancellation via `ct` throws `OperationCanceledException`.

### `static IServiceCollection AddExternalApiClient(this IServiceCollection services, Action<ExternalApiClientOptions> configure)`

Registers `ExternalApiClient` and its dependencies in the DI container. The `configure` delegate receives `ExternalApiClientOptions` to set properties such as base address, default headers, and timeout. Returns the `IServiceCollection` for chaining. Throws `ArgumentNullException` if `services` or `configure` is null.

## Usage

### Example 1: GET request with success and failure handling

```csharp
public class ProductService
{
    private readonly ExternalApiClient _apiClient;

    public ProductService(ExternalApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<Product?> GetProductAsync(string productId, CancellationToken ct)
    {
        ApiCallResult<Product> result = await _apiClient.GetAsync<Product>($"products/{productId}", ct);

        if (result.IsSuccess)
        {
            Console.WriteLine($"Fetched product in {result.Duration.TotalMilliseconds} ms");
            return result.Data;
        }

        Console.WriteLine($"Failed to fetch product: {result.ErrorMessage} (HTTP {result.HttpStatusCode})");
        return null;
    }
}
```

### Example 2: POST request with DI registration

```csharp
// In Startup.cs or Program.cs
services.AddExternalApiClient(options =>
{
    options.BaseAddress = new Uri("https://api.example.com");
    options.Timeout = TimeSpan.FromSeconds(30);
    options.DefaultHeaders.Add("X-Tenant-Id", TenantContext.Current.Id);
});

// In a consuming service
public class OrderService
{
    private readonly ExternalApiClient _apiClient;

    public OrderService(ExternalApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<bool> CreateOrderAsync(OrderRequest order, CancellationToken ct)
    {
        ApiCallResult<OrderResponse> result = await _apiClient.PostAsync<OrderResponse>("orders", order, ct);

        if (!result.IsSuccess)
        {
            // Log error and optionally rethrow or return false
            _logger.LogError("Order creation failed: {Error} (Status: {Status})",
                result.ErrorMessage, result.HttpStatusCode);
            return false;
        }

        _logger.LogInformation("Order {OrderId} created in {Duration} ms",
            result.Data.Id, result.Duration.TotalMilliseconds);
        return true;
    }
}
```

## Notes

- **Null `Data` on failure**: When `IsSuccess` is `false`, `Data` is always `null`. Do not attempt to access `Data` without first checking `IsSuccess`.
- **Null `HttpStatusCode`**: A null status code indicates the request never reached the server. This can happen due to DNS failures, connection timeouts, or cancellation before the response headers are received.
- **`DeleteAsync` returns `bool`**: Unlike the other methods, `DeleteAsync` does not deserialize a response body. `Data` is `true` for any 2xx status code and `false` otherwise, even if the server returns a body.
- **Duration measurement**: `Duration` includes the full round-trip time, including any internal retries or redirects performed by the underlying HTTP handler. It is always populated, even on failures.
- **Thread safety**: `ApiCallResult<T>` is an immutable snapshot produced at the end of a call. It is safe to read from multiple threads. `ExternalApiClient` itself is designed to be registered as a singleton or scoped service and relies on `HttpClient`’s documented thread safety for concurrent requests.
- **Cancellation**: All async methods accept a `CancellationToken`. When cancellation is requested, an `OperationCanceledException` is thrown rather than returning an `ApiCallResult<T>` with `IsSuccess` set to `false`. Callers should handle this exception separately from API-level failures.
- **Error message content**: `ErrorMessage` may contain raw server responses, exception stack traces, or deserialization error details. Avoid exposing it directly to end users without sanitization.
