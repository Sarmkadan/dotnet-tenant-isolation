# WebhookPayload

The `WebhookPayload` type serves as a composite data structure within the `dotnet-tenant-isolation` project, encapsulating both the metadata required to identify a specific webhook event and the configuration state of the target webhook endpoint. It aggregates event-specific details, such as the payload data and signature, with operational metrics like failure counts and activation status, facilitating the serialization, transmission, and auditing of multi-tenant webhook interactions.

## API

The following members are exposed by the `WebhookPayload` type:

### `EventId`
*   **Type**: `string`
*   **Purpose**: Uniquely identifies the specific occurrence of an event instance. This value is distinct from the event type and is used for idempotency checks and audit tracing.

### `EventType`
*   **Type**: `string`
*   **Purpose**: Defines the category or classification of the event being transmitted (e.g., `tenant.created`, `user.updated`). This determines how the receiving system should interpret the `Data` payload.

### `TenantId`
*   **Type**: `Guid`
*   **Purpose**: Identifies the specific tenant associated with this event, ensuring logical isolation in multi-tenant environments.
*   **Note**: This member appears multiple times in the definition, indicating potential redundancy between event context and webhook configuration scopes; in practice, it represents the tenant owning the event.

### `Timestamp`
*   **Type**: `DateTime`
*   **Purpose**: Records the exact UTC time when the event occurred or was generated.

### `Data`
*   **Type**: `object?`
*   **Purpose**: Contains the actual payload content relevant to the event. This property is nullable, allowing for events that signal an occurrence without transmitting additional data.

### `Signature`
*   **Type**: `string`
*   **Purpose**: Holds the cryptographic signature (typically HMAC) used by the receiver to verify the authenticity and integrity of the payload against the shared secret.

### `Id`
*   **Type**: `Guid`
*   **Purpose**: Represents the unique identifier for the payload record itself or the primary entity context, depending on the serialization scope.
*   **Note**: Duplicate definitions exist in the source; this generally refers to the unique ID of the event record.

### `Url`
*   **Type**: `string`
*   **Purpose**: Specifies the target endpoint URI where the webhook payload should be delivered.

### `Secret`
*   **Type**: `string?`
*   **Purpose**: The shared secret key used to generate the `Signature`. This property is nullable and should be handled securely, typically omitted from logs or external serialization unless explicitly required for signing operations.

### `IsActive`
*   **Type**: `bool`
*   **Purpose**: Indicates whether the webhook configuration is currently enabled. If `false`, the system should suppress delivery attempts.

### `CreatedAt`
*   **Type**: `DateTime`
*   **Purpose**: Records the timestamp when the webhook configuration or event record was initially created.

### `LastTriggeredAt`
*   **Type**: `DateTime?`
*   **Purpose**: Stores the timestamp of the most recent successful or attempted delivery. Null if the webhook has never been triggered.

### `FailureCount`
*   **Type**: `int`
*   **Purpose**: Tracks the number of consecutive failed delivery attempts. This metric is often used to implement backoff strategies or automatic disabling of unstable webhooks.

### `DisabledAt`
*   **Type**: `DateTime?`
*   **Purpose**: Records the timestamp when the webhook was automatically or manually disabled due to excessive failures or administrative action. Null if currently active.

### `WebhookId`
*   **Type**: `Guid`
*   **Purpose**: References the specific configuration entity of the webhook subscription, distinguishing it from the event instance ID.

### `HttpStatusCode`
*   **Type**: `int`
*   **Purpose**: Captures the HTTP response status code returned by the target server during the last delivery attempt. Used to determine success (2xx) or specific failure modes (4xx, 5xx).

## Usage

### Example 1: Constructing a Payload for Dispatch
This example demonstrates populating a `WebhookPayload` instance with event data and signing information prior to transmission.

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

public class WebhookDispatcher
{
    public WebhookPayload PreparePayload(Guid tenantId, string eventType, object eventData, string secret, string targetUrl)
    {
        var eventId = Guid.NewGuid().ToString();
        var timestamp = DateTime.UtcNow;
        
        // Generate a simple HMAC-SHA256 signature for demonstration
        var payloadBody = System.Text.Json.JsonSerializer.Serialize(eventData);
        var signature = GenerateSignature(payloadBody, secret);

        return new WebhookPayload
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = eventType,
            TenantId = tenantId,
            Timestamp = timestamp,
            Data = eventData,
            Signature = signature,
            Url = targetUrl,
            Secret = secret, // Usually kept in memory only
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            WebhookId = Guid.NewGuid(), // Assumed known from config
            FailureCount = 0,
            HttpStatusCode = 0
        };
    }

    private string GenerateSignature(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
```

### Example 2: Processing Delivery Results
This example illustrates updating the `WebhookPayload` state based on the outcome of an HTTP request, specifically handling failure counting and status recording.

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class WebhookExecutor
{
    public async Task<WebhookPayload> ExecuteAndRecord(WebhookPayload payload, HttpClient client)
    {
        if (!payload.IsActive)
        {
            return payload;
        }

        try
        {
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload.Data));
            var response = await client.PostAsync(payload.Url, content);
            
            payload.HttpStatusCode = (int)response.StatusCode;
            payload.LastTriggeredAt = DateTime.UtcNow;

            if (response.IsSuccessStatusCode)
            {
                payload.FailureCount = 0;
            }
            else
            {
                payload.FailureCount += 1;
                // Logic to disable if threshold exceeded could follow here
                if (payload.FailureCount >= 5)
                {
                    payload.DisabledAt = DateTime.UtcNow;
                    payload.IsActive = false;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            payload.HttpStatusCode = 0; // Or specific mapping
            payload.FailureCount += 1;
            payload.LastTriggeredAt = DateTime.UtcNow;
        }

        return payload;
    }
}
```

## Notes

*   **Member Redundancy**: The type definition contains duplicate property names (`TenantId`, `EventType`, `Id`, `EventId`). In a compiled assembly, this typically indicates either a copy-paste error in documentation extraction or a complex inheritance/extension scenario where properties are re-declared. Consumers should verify which specific memory slot corresponds to the event context versus the webhook configuration context to avoid data overwrites during serialization.
*   **Thread Safety**: As `WebhookPayload` is a reference type containing mutable value types (`DateTime`, `int`, `bool`) and reference types (`string`, `object`), it is not thread-safe by default. Concurrent modification of properties such as `FailureCount` or `LastTriggeredAt` without external locking mechanisms may result in race conditions and inaccurate metric tracking.
*   **Nullable Handling**: The `Data`, `Secret`, `LastTriggeredAt`, and `DisabledAt` properties are nullable. Callers must perform null checks before accessing members of `Data` or performing cryptographic operations with `Secret`.
*   **Security**: The `Secret` property is exposed as a public member. Care must be taken to ensure this property is not inadvertently logged, serialized to client-side outputs, or included in telemetry streams, as exposure compromises the integrity of the webhook signature verification.
*   **Time Zones**: All `DateTime` members (`Timestamp`, `CreatedAt`, etc.) should be treated as UTC. No timezone offset information is stored within the type, so local time conversion must be handled explicitly by the consumer.
