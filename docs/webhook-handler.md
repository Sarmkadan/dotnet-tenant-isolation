# WebhookHandler

`WebhookHandler` manages webhook subscriptions and turns an incoming `TenantEvent` into outbound HTTP deliveries. It is not an HTTP endpoint for receiving third-party webhooks: the input to this flow is the in-process call to `SendWebhookAsync`, while `IWebhookDeliveryService` performs the outbound POST.

The implementation is in `src/TenantIsolation/Integration/WebhookHandler.cs` under the `TenantIsolation.Integration` namespace.

## Processing flow

For each call to `SendWebhookAsync`, the handler:

1. Rejects a null event with `ArgumentNullException`.
2. Takes a snapshot of active subscriptions whose `TenantId` equals the event's tenant and whose `EventType` exactly equals the runtime event type name returned by `event.GetType().Name`.
3. Returns without delivery when no subscription matches.
4. Starts one delivery task per matching subscription and awaits all of them with `Task.WhenAll`.
5. For each subscription, maps the event to a `WebhookPayload` and supplies a fixed resilience configuration to `IWebhookDeliveryService`.
6. Records the returned result in the subscription's in-memory delivery history.
7. Resets or increments the subscription's consecutive failure state.

Matching is case-sensitive and uses the unqualified CLR type name, not a base type, interface, namespace-qualified name, or value stored elsewhere in the event. A subscription for `TenantCreatedEvent` therefore does not receive a different event type, even when the types share a base class. The tenant comparison occurs before fan-out, which prevents a subscription registered for one tenant from being selected for another tenant's event.

Deliveries to matching subscriptions run concurrently. `SendWebhookAsync` completes after every selected delivery task completes; it does not return after merely scheduling the work.

## Payload and outbound request

The handler creates this logical payload for each selected subscription:

| Payload field | Source |
| --- | --- |
| `EventId` | `TenantEvent.EventId` |
| `EventType` | Runtime type name from `TenantEvent.GetType().Name` |
| `TenantId` | `TenantEvent.TenantId` |
| `Timestamp` | `TenantEvent.OccurredAt` |
| `Data` | The complete `TenantEvent` instance |
| `Signature` | Initially empty; the delivery service populates it when a secret is configured |

`WebhookHandler` delegates serialization, signing, HTTP transport, retry decisions, timeout handling, and circuit-breaker state to `IWebhookDeliveryService`. The endpoint passed to that service always has the following settings:

| Setting | Value |
| --- | --- |
| URL | Subscription `Url` |
| Secret | Subscription `Secret` |
| Timeout | 10 seconds per attempt |
| Maximum retries | 3 |
| Base retry delay | 1,000 milliseconds |
| Respect `Retry-After` | Enabled |
| Circuit breaker | Enabled |

The delivery service sends JSON with `Content-Type: application/json` and adds `X-Event-Id`, `X-Event-Type`, and `X-Tenant-Id` on the initial request. When a secret is present, it signs the payload with HMAC-SHA256 and also sends the signature in `X-Signature`. Retry details and circuit-breaker behavior belong to the delivery service rather than the handler.

## Result handling

Every result returned by the delivery service becomes a `WebhookDelivery` entry containing the subscription ID, event ID, status code, success flag, error message, retry count, and the UTC time at which the handler recorded the result. A missing HTTP status code is stored as `0`.

On success, the handler:

- sets `LastTriggeredAt` to the current UTC time;
- resets `FailureCount` to zero; and
- leaves the subscription active.

On failure, it increments `FailureCount`. The subscription remains active through five consecutive failed calls. The sixth consecutive failure sets `IsActive` to `false` and records `DisabledAt`; subsequent events no longer select that subscription. A later success resets the counter, so the threshold is based on consecutive handler-level failures rather than lifetime failures.

The handler does not throw merely because the delivery service returns an unsuccessful result. Unexpected exceptions escaping an individual delivery task do propagate from `Task.WhenAll`; other deliveries may already have run, and the handler does not roll back their history or subscription updates.

## Subscription lifecycle

`RegisterWebhookAsync` requires non-empty event type and URL values and validates the URL with `ValidationUtility.RequireValidUrl`. It creates an active subscription with a new ID and initializes an empty history list. The optional secret is stored with the subscription for signing by the delivery service.

`UnregisterWebhookAsync` removes both the subscription and its history. It returns `true` only when the subscription existed; history is removed even if the subscription lookup fails.

The query methods behave as follows:

- `GetWebhooksAsync(tenantId)` returns all subscriptions for that tenant, including inactive ones.
- Supplying `eventType` applies the same case-sensitive string equality used during delivery. A null or empty filter means all event types for the tenant.
- `GetWebhookByIdAsync` looks up an ID without applying a tenant filter. Callers that expose this method across a trust boundary must enforce tenant authorization separately.
- `GetDeliveryHistoryAsync` returns the newest entries first and applies `limit` with LINQ `Take`. An unknown webhook ID returns an empty sequence.

## Lifetime, storage, and concurrency

The default service registration adds `IWebhookHandler` as scoped when webhooks are enabled. Subscriptions and histories are held only in dictionaries owned by that handler instance. They are not persisted, shared across handler scopes or application instances, or restored after a restart. Registration and event dispatch must therefore resolve the same handler instance to observe the same subscriptions.

The subscription dictionaries support concurrent dictionary operations, but the contained `WebhookSubscription` objects are mutable and each history value is a mutable `List<WebhookDelivery>`. Concurrent calls involving the same subscription can race while updating failure state or appending/reading history. Consumers needing durable subscriptions, cross-instance delivery, or strong concurrency guarantees should place those concerns in a persistent, synchronized implementation rather than treating this handler as a durable queue.

## Example

```csharp
var subscription = await webhookHandler.RegisterWebhookAsync(
    tenantId,
    nameof(TenantCreatedEvent),
    "https://partner.example/webhooks/tenants",
    signingSecret);

await webhookHandler.SendWebhookAsync(tenantCreatedEvent);

var recentDeliveries = await webhookHandler.GetDeliveryHistoryAsync(
    subscription.Id,
    limit: 10);
```

The event type passed at registration must match the concrete event class name. Registering a friendly label such as `"tenant.created"` will not match `TenantCreatedEvent` unless that is also the runtime type name.

## Operational boundaries

- Tenant filtering in this class is routing logic, not authentication or authorization.
- Delivery history is diagnostic in-memory state, not an audit log.
- Disabling a subscription is automatic after six consecutive failed results, but re-enabling it is not exposed by `IWebhookHandler`.
- Unregistering while a delivery is already in progress does not cancel that outbound request.
- The handler accepts no cancellation token, so callers cannot directly cancel fan-out through this API.

## Related documentation

- [Webhook delivery service](../src/TenantIsolation/Integration/WebhookDeliveryService.cs)
- [Tenant events](./TenantEvent.md)
- [Architecture](./ARCHITECTURE.md)
