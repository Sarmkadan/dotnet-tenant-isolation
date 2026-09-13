# Tenant resolution middleware

`TenantResolutionMiddleware` establishes the tenant context for an HTTP request before passing the request to the rest of the ASP.NET Core pipeline. The middleware handles excluded paths, a cross-request tenant cache, response metadata, and resolution errors. It delegates the actual header, subdomain, query-string, route, claims, and default-tenant lookups to `ITenantResolutionService` (implemented by `TenantResolutionService`).

## Registration and pipeline placement

Register tenant isolation through one of the `AddTenantIsolation...` methods and add the middleware before components that require a tenant:

```csharp
builder.Services.AddTenantIsolationSqlServer(
    connectionString,
    options =>
    {
        options.ExcludedPaths = ["/health", "/api/health", "/.well-known"];
        options.EnableCaching = true;
        options.CacheExpirationMinutes = 10;
    });

builder.Services.Configure<TenantResolutionOptions>(options =>
{
    options.ResolutionStrategies =
    [
        TenantResolutionStrategy.Header,
        TenantResolutionStrategy.Subdomain,
        TenantResolutionStrategy.QueryString,
        TenantResolutionStrategy.Route,
        TenantResolutionStrategy.Claims,
        TenantResolutionStrategy.Default
    ];
    options.ThrowOnResolutionFailure = true;
});

var app = builder.Build();

app.UseAuthentication(); // Required before resolution when using Claims.
app.UseTenantResolution();
app.UseAuthorization();
```

Place routing before tenant resolution when using route values, and authentication before it when using claims. Place tenant-dependent authorization, data access, and endpoint code after it.

## Request sequence

For each request, the middleware follows this sequence:

1. **Check excluded paths.** Each configured entry is compared to `Request.Path` with case-insensitive `StartsWithSegments`. A match bypasses all tenant work and immediately invokes the next middleware. No tenant items or tenant response headers are added.
2. **Build the cross-request cache key.** When caching is enabled, a valid GUID in `X-Tenant-Id` produces a tenant-ID-based key. Otherwise, the request host produces a host-based key. A missing host, or disabled caching, skips this cache.
3. **Read the tenant cache.** A cache hit skips the resolution service. The middleware stores the cached tenant in `HttpContext.Items`, adds the tenant response headers, and invokes the next middleware.
4. **Run the configured resolution strategies.** On a cache miss, `ResolveTenantAsync()` tries each `TenantResolutionOptions.ResolutionStrategies` entry in order. The first lookup that returns a tenant wins.
5. **Validate activity.** The resolution service accepts only a tenant whose `Status` is `Active`. Its failure behavior depends on `ThrowOnResolutionFailure`.
6. **Cache a successful result.** When the middleware has a cache key, it stores the resolved tenant for `CacheExpirationMinutes`; a value of zero or less is treated as five minutes.
7. **Publish request context and response metadata.** The tenant is stored in `HttpContext.Items["tenant:current"]` and `HttpContext.Items["TenantId"]`. The response receives `X-Tenant-Id` and `X-Tenant-Slug`, after which the next middleware runs.

The resolution service also records the successful strategy in `HttpContext.Items["tenant:resolved_strategy"]`. A middleware-level cache hit does not populate that strategy item, because no strategy runs on that request.

## Resolution strategies

Strategies are ordered: the first successful lookup stops the chain. Within every ID/slug pair, the ID is checked first.

| Strategy | Request value | Lookup behavior |
| --- | --- | --- |
| `Header` | `X-Tenant-Id: <guid>` or `X-Tenant-Slug: <slug>` | Looks up a valid GUID directly; otherwise compares the slug to active tenants, case-insensitively. An invalid or unknown ID does not prevent the slug header from being tried. |
| `Subdomain` | Host such as `acme.example.com` | Uses the first host label (`acme`) as the tenant slug. A host without a usable dot-separated first label is ignored. Slug matching is case-insensitive. |
| `QueryString` | `?tenantId=<guid>` or `?tenantSlug=<slug>` | Looks up the ID directly or compares the slug to active tenants, case-insensitively. |
| `Route` | Route values named `tenantId` or `slug` | Reads values from ASP.NET Core route data, then performs the corresponding ID or case-insensitive slug lookup. |
| `Claims` | Authenticated-user claims `tenant_id` or `tenant_slug` | Skips unauthenticated users. Looks up the ID directly or compares the slug to active tenants, case-insensitively. |
| `Default` | `DefaultTenantId` or `DefaultTenantSlug` | Provides the final configured fallback. If included, `Default` must be last. |

Subdomain resolution ignores these reserved first labels:

```text
www, api, mail, smtp, ftp, admin, app, static, cdn, assets,
dev, staging, prod, auth, login
```

The list is built into `TenantResolutionService`; it is not currently configurable.

Although a newly constructed `TenantResolutionOptions` lists `Subdomain` first, the standard `AddTenantIsolation...` registration configures the effective default order as `Header`, `Subdomain`, `QueryString`, `Route`, `Claims`, then `Default`. Explicitly configure `ResolutionStrategies` when application behavior should not depend on registration defaults.

## Configuration options

### `TenantIsolationOptions` used by the middleware

| Option | Default | Effect |
| --- | --- | --- |
| `ExcludedPaths` | `/health`, `/api/health`, `/.well-known` | Path prefixes that bypass tenant resolution. Matching is case-insensitive and segment-aware. |
| `EnableCaching` | `true` | Enables the middleware's cross-request cache lookup and write. It does not control the per-request value stored in `HttpContext.Items` by the resolution service. |
| `CacheExpirationMinutes` | `60` | Successful-resolution cache lifetime. The middleware substitutes five minutes when the configured value is zero or negative. |

The middleware cache key is based only on a valid `X-Tenant-Id` or, as a fallback, the host. It does not include `X-Tenant-Slug`, query-string values, route values, or claims. If different tenants can use those inputs on the same host, disable middleware caching or ensure requests have tenant-specific hosts or valid `X-Tenant-Id` headers so a host-level cache entry cannot be reused across those requests.

### `TenantResolutionOptions` used by the resolution service

| Option | Default/effective registration value | Effect |
| --- | --- | --- |
| `ResolutionStrategies` | `Header`, `Subdomain`, `QueryString`, `Route`, `Claims`, `Default` | Ordered strategy chain; the first successful strategy wins. The collection cannot be empty, and `Default` must be last when present. |
| `DefaultTenantId` | `null` | ID used by the `Default` strategy. It is checked before `DefaultTenantSlug`. |
| `DefaultTenantSlug` | `null` | Slug used by the `Default` strategy. Configuration validation restricts using it together with `DefaultTenantId` unless its text equals the ID's GUID representation. |
| `ThrowOnResolutionFailure` | `true` | Causes unresolved requests, and inactive tenants found during resolution, to raise tenant-resolution exceptions. With `false`, the resolver returns an unsuccessful result; `ResolveTenantAsync()` still converts an unsuccessful result into `TenantNotResolvedException`. |

## Outcomes and errors

- A successfully resolved or middleware-cached tenant is available downstream through `ITenantResolutionService.GetCurrentTenant()`, `HttpContext.Items["tenant:current"]`, or `HttpContext.Items["TenantId"]`.
- The middleware adds `X-Tenant-Id` and `X-Tenant-Slug` response headers after successful resolution. Excluded and unresolved requests do not receive them.
- `TenantNotResolvedException` produces HTTP `400` with a JSON body containing `error`, `message`, and `code`.
- `TenantNotActiveException` produces HTTP `403` with the same JSON shape.
- Other `TenantIsolationException` instances produce HTTP `500` with the same JSON shape.
- Unexpected exceptions are logged and rethrown for the application's outer error handler.

If an `ITenantResolutionService` implementation returns `null` instead of throwing, the middleware logs a warning and continues without a tenant context. The built-in `TenantResolutionService.ResolveTenantAsync()` normally throws when it cannot produce a tenant.
