# ExternalApiClient

`ExternalApiClient` is the library's typed wrapper around an injected `HttpClient`. It creates outbound GET, POST, PUT, and DELETE requests, adds caller-supplied headers, serializes request bodies as JSON, deserializes successful response bodies, and returns an `ApiCallResult<T>` instead of throwing for most send and response-processing failures.

The client does not resolve, validate, or authorize a tenant. Tenant context propagation is explicit: the caller must copy the appropriate tenant identifier into the request headers for every outbound call that needs tenant scope.

## Registration and construction

Register the typed client with the provided extension:

```csharp
services.AddExternalApiClient();
```

This registers `IExternalApiClient` through `AddHttpClient<IExternalApiClient, ExternalApiClient>()`. The configured `HttpClient` has:

- a 30-second timeout;
- a default `User-Agent` header of `TenantIsolation/1.0`.

`AddTenantIsolation(...)` also calls this registration when `TenantIsolationOptions.EnableExternalApiClient` is `true`, which is its default value.

The class can also be constructed directly from an `HttpClient` and an `ILogger<ExternalApiClient>`. The constructor itself does not perform null checks.

## Outbound operations

| Method | Request body | Successful result |
| --- | --- | --- |
| `GetAsync<T>(url, headers)` | None | Deserializes the response JSON as `T`. |
| `PostAsync<T>(url, payload, headers)` | Serializes `payload` as JSON with UTF-8 and `application/json`. | Deserializes the response JSON as `T`. |
| `PutAsync<T>(url, payload, headers)` | Serializes `payload` as JSON with UTF-8 and `application/json`. | Deserializes the response JSON as `T`. |
| `DeleteAsync(url, headers)` | None | Returns `Data = true`; the response body is not exposed. |

URLs may be absolute, or relative when the injected `HttpClient` has a `BaseAddress`. `GetAsync` and `DeleteAsync` reject a null or empty URL with `ArgumentException`. `PostAsync` and `PutAsync` reject a null payload with `ArgumentNullException`; payload serialization occurs before the client's internal exception handling, so serialization failures propagate to the caller.

The optional header dictionary is applied to `HttpRequestMessage.Headers` with `Add`. It is intended for request headers such as tenant identifiers, authorization, or correlation values. Content headers such as `Content-Type` belong to the request content and cannot necessarily be added through this dictionary. Invalid or restricted request headers are reported as a failed result because header application occurs inside the request-processing exception handler.

## Tenant context propagation

`ExternalApiClient` has no dependency on `IHttpContextAccessor`, `ITenantResolutionService`, or another ambient tenant-context provider. It does not automatically forward the tenant from the inbound request, claims, route, or `HttpContext.Items`. Omitting tenant headers sends an unscoped request from this client's perspective.

Pass the resolved tenant explicitly using the shared header constants:

```csharp
using TenantIsolation.Constants;
using TenantIsolation.Integration;

public sealed class CatalogGateway
{
    private readonly IExternalApiClient _client;

    public CatalogGateway(IExternalApiClient client)
    {
        _client = client;
    }

    public Task<ApiCallResult<Product>> GetProductAsync(
        Guid tenantId,
        Guid productId)
    {
        var headers = new Dictionary<string, string>
        {
            [TenantConstants.TenantIdHeader] = tenantId.ToString()
        };

        return _client.GetAsync<Product>(
            $"https://catalog.example/products/{productId}",
            headers);
    }
}
```

Use `TenantConstants.TenantIdHeader` (`X-Tenant-Id`) when propagating an ID or `TenantConstants.TenantSlugHeader` (`X-Tenant-Slug`) when the downstream service resolves by slug. Which header is trusted is a contract of the receiving service. Forward only tenant context that the application has already authenticated and authorized; do not copy an arbitrary inbound tenant header before tenant resolution has established that it belongs to the caller.

Background jobs have no inbound HTTP context, so they must carry the tenant ID or slug in their work item and construct the same header dictionary explicitly. Retries reuse the supplied headers, preserving that explicit tenant value across retry attempts.

## Result and failure behavior

For an HTTP success status, the client reads the entire response body and deserializes it with the default `System.Text.Json.JsonSerializer` options. The returned result contains `IsSuccess = true`, the deserialized `Data`, the numeric `HttpStatusCode`, and the elapsed `Duration`. A successful response may still have `Data = null`, for example when its body is the JSON literal `null` and the target type permits it. An empty body generally causes deserialization to fail and produces a failed result through the general exception handler.

For a non-success status, the client reads the response body for logging and returns:

- `IsSuccess = false`;
- `ErrorMessage = "API returned {status}"`;
- the numeric `HttpStatusCode`;
- the elapsed `Duration`.

The response body is not placed in `ErrorMessage` or returned to the caller. It is written to the warning log, so downstream error bodies may contain data that should be considered when configuring log access and retention.

`HttpRequestException` failures may be retried when their inner exception is a `TimeoutException`, `IOException`, or another `HttpRequestException`. The client makes up to three retries after the initial attempt, with delays of 1, 2, and 4 seconds. Other `HttpRequestException` instances return a failed result immediately. The `Duration` in a final result measures only the final attempt, not earlier attempts or backoff delays.

Other exceptions raised while constructing or sending the request, reading the response, or deserializing successful JSON are caught and returned with `ErrorMessage = "Unexpected error: ..."`. The public methods do not accept a `CancellationToken`. In particular, the configured `HttpClient` timeout is not documented as part of the transient retry classification and can follow the general-exception path.

## Logging and operational considerations

The client logs the HTTP method and URL before each attempt, success duration at information level, non-success response content at warning level, retry attempts at warning level, and terminal exceptions at error level. Avoid putting secrets or sensitive tenant data in URLs because the complete URL is logged.

The wrapper standardizes transport results, but it does not provide tenant authorization, circuit breaking, rate limiting, response streaming, custom JSON options, cancellation, or automatic authentication. Configure those concerns on the underlying `HttpClient` or in application-specific handlers, and keep tenant authorization at the boundary where the tenant is resolved.

## Related documentation

- [ApiCallResult](./ApiCallResult.md)
- [Tenant resolution middleware](./tenant-resolution-middleware.md)
- [API reference](./api-reference.md)
