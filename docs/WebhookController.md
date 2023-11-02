# WebhookController

The `WebhookController` is an ASP.NET Core API controller responsible for managing webhook subscriptions and deliveries within a multi‑tenant environment. It exposes endpoints that allow tenants to register, retrieve, test, and delete webhooks, as well as to query delivery logs. The controller relies on request‑scoped data such as the tenant identifier and webhook details to isolate operations per tenant.

## API

### RegisterWebhook
- **Purpose:** Handles the creation of a new webhook subscription for the current tenant.  
- **Parameters:** Receives a `WebhookSubscription` object (typically from the request body) that defines the event type, target URL, and optional secret.  
- **Return Value:** `Task<ActionResult<ApiResponse<WebhookSubscription>>>` – a wrapped response containing the created subscription or error information.  
- **Throws:** May throw exceptions that result in a `BadRequest` or `Conflict` response when the supplied data fails validation, the URL is malformed, or a subscription with the same event type and URL already exists.

### GetWebhook
- **Purpose:** Retrieves a specific webhook subscription belonging to the tenant.  
- **Parameters:** Expects an identifier (e.g., GUID) for the subscription, usually supplied via a route parameter.  
- **Return Value:** `Task<ActionResult<ApiResponse<WebhookSubscription>>>` – returns the matching subscription or a not‑found response.  
- **Throws:** May produce a `NotFound` response if no subscription matches the identifier, or a `BadRequest` if the identifier is invalid.

### GetTenantWebhooks
- **Purpose:** Returns all webhook subscriptions registered for the current tenant.  
- **Parameters:** No additional parameters beyond the tenant context derived from the request.  
- **Return Value:** `Task<ActionResult<ApiResponse<List<WebhookSubscription>>>>` – a list of subscriptions wrapped in an API response.  
- **Throws:** Generally does not throw; returns an empty list if the tenant has no subscriptions.

### DeleteWebhook
- **Purpose:** Deletes a webhook subscription for the tenant.  
- **Parameters:** Requires the subscription identifier (route or query parameter).  
- **Return Value:** `Task<ActionResult<ApiResponse<object>>>` – indicates success or failure of the deletion operation.  
- **Throws:** May yield a `NotFound` response if the subscription does not exist, or a `BadRequest` for an invalid identifier.

### GetWebhookDeliveries
- **Purpose:** Provides delivery attempt logs for a specific webhook subscription.  
- **Parameters:** Expects the webhook subscription identifier to filter deliveries.  
- **Return Value:** `Task<ActionResult<ApiResponse<List<WebhookDelivery>>>>` – a collection of delivery records.  
- **Throws:** Returns `NotFound` if the webhook does not exist; otherwise succeeds with an empty list when no deliveries have been recorded.

### TestWebhook
- **Purpose:** Sends a test payload to the configured webhook URL to verify connectivity and secret handling.  
- **Parameters:** Takes the webhook subscription identifier (and optionally a test payload).  
- **Return Value:** `Task<ActionResult<ApiResponse<object>>>` – outcome of the test request.  
- **Throws:** May produce a `BadRequest` if the webhook is missing, or propagate HTTP communication errors as an internal server error response.

### TenantId
- **Purpose:** Holds the identifier of the tenant associated with the current request. Populated by middleware or route constraints.  
- **Type:** `string`

### EventType
- **Purpose:** Represents the type of event the webhook is subscribed to (e.g., `"order.created"`).  
- **Type:** `string`

### Url
- **Purpose:** The target URL where webhook payloads are delivered.  
- **Type:** `string`

### Secret
- **Purpose:** Optional secret used to sign webhook payloads for verification by the receiver.  
- **Type:** `string?` (nullable)

## Usage

```csharp
// Example 1: Register a new webhook using HttpClient
var client = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
var payload = new
{
    eventType = "order.created",
    url = "https://tenant.example.com/webhooks/order",
    secret = "s3cr3t"
};
var response = await client.PostAsJsonAsync("webhook/register", payload);
response.EnsureSuccessStatusCode();
var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<WebhookSubscription>>();
Console.WriteLine($"Webhook registered with id: {apiResponse?.Result?.Id}");
```

```csharp
// Example 2: Retrieve all webhook subscriptions for the current tenant
var client = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
// Assume tenant context is set via a header or subdomain handled by the server
var response = await client.GetAsync("webhook/tenant");
response.EnsureSuccessStatusCode();
var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<WebhookSubscription>>>();
foreach (var hook in apiResponse?.Result ?? new List<WebhookSubscription>())
{
    Console.WriteLine($"{hook.Id}: {hook.EventType} -> {hook.Url}");
}
```

## Notes

- The controller is designed to be **stateless**; all tenant‑specific data (`TenantId`, `EventType`, `Url`, `Secret`) is supplied per request, typically through ASP.NET Core routing, headers, or middleware. Consequently, a single controller instance can safely handle concurrent requests without additional synchronization.
- If the `Secret` property is `null`, the webhook payloads are delivered unsigned; consumers should treat such webhooks as unauthenticated.
- Duplicate registration attempts (same `EventType` and `Url` for a tenant) result in a conflict response (`409`) rather than creating a second entry.
- Invalid URLs (failing `Uri.IsWellFormedUriString`) trigger a `400 Bad Request` before any persistence attempt.
- The `TestWebhook` endpoint performs an outbound HTTP request; network failures or non‑2xx responses from the target URL are captured and returned as part of the `ApiResponse` payload, not as exceptions thrown by the controller.
- Because the controller relies on request‑scoped services (e.g., a webhook repository), it should be registered as **transient** or **scoped** in the DI container to avoid unintended state sharing across requests.
