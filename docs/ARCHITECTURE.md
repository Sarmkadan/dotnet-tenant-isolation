# Architecture

This document describes the actual structure of the codebase as it is today. If the code and this doc disagree, the code wins - and this doc should be fixed.

## Overview

A multi-tenancy isolation framework for ASP.NET Core built on EF Core. The core idea: resolve the tenant once per HTTP request, cache it in `HttpContext.Items`, and hand every downstream component (DbContext factory, caching, feature toggles, metering) a tenant-scoped view of the world.

Solution layout:

```
src/TenantIsolation/          the framework + a runnable host (Program.cs)
tests/dotnet-tenant-isolation.Tests/
benchmarks/dotnet-tenant-isolation.Benchmarks/   BenchmarkDotNet suites
examples/                     standalone usage snippets (not compiled into the solution flow)
docs/                         per-type reference docs + this file
```

The `src/TenantIsolation` project is both a library and a demo host: `Program.cs` wires `AddTenantIsolationSqlServer(...)` + `AddTenantFeatureToggle()` + `UseTenantResolution()` and runs migrations on startup.

## Request data flow

```
HTTP request
  -> RequestContextMiddleware / RequestLoggingMiddleware / RateLimitingMiddleware / ErrorHandlingMiddleware
  -> TenantResolutionMiddleware
       - skips excluded paths (TenantIsolationOptions.ExcludedPaths)
       - calls TenantResolutionService.ResolveTenantAsync()
       - stores tenant in HttpContext.Items, adds X-Tenant-Id / X-Tenant-Slug response headers
  -> controllers (TenantApiController, AdminController, FeaturesController, AnalyticsController, WebhookController)
  -> services (TenantService, ConfigurationService, TenantFeatureService, DataIsolationService, ...)
  -> repositories (Repository<T>, TenantRepository, UserRepository, OrganizationRepository)
  -> TenantDbContext (created per-request by TenantDbContextFactory)
```

### Tenant resolution

`Services/TenantResolutionService` tries strategies in a fixed priority order:

1. **Header** - `X-Tenant-Id` (GUID) or tenant slug header
2. **Claims** - tenant id / slug claims on the authenticated principal
3. **Route** - tenant id / slug route parameters
4. **Subdomain** - first host label, with a `FrozenSet` of reserved subdomains (`www`, `api`, `admin`, ...) filtered out; subdomain extraction uses `IndexOf` instead of `Split` to avoid per-request array allocation

The resolved tenant must be `TenantStatus.Active`, otherwise `TenantNotActiveException`. Result is cached in `HttpContext.Items` so repeat resolutions within a request are free.

**Rationale**: header first because API clients are the primary consumers and headers are explicit; subdomain last because it is the most ambiguous (shared hosts, reserved names). Trade-off: slug lookups currently scan `GetAllActiveTenantsAsync()` with `FirstOrDefault` - fine for hundreds of tenants held in the in-memory store, would need a slug index in the store for tens of thousands.

### Tenant store

`Services/DynamicTenantStore` (`IDynamicTenantStore`) is the singleton source of truth for tenant lookups at request time. It caches all tenants in a `ConcurrentDictionary<Guid, Tenant>` backed by `TenantRepository`, and raises `OnTenantRegistered` / `OnTenantRemoved` events. `TenantStoreBackgroundReloadService` (an `IHostedService`) starts a periodic reload timer so tenant changes propagate without restart.

**Rationale**: resolution happens on every request; hitting the database each time would put the tenants table on the hot path. Trade-off: eventual consistency - a newly created tenant is invisible until the next reload tick unless registered through the store. Known wart: the constructor does `LoadTenantsAsync().Wait()`, a sync-over-async block during first resolution of the singleton.

### Data access

- `Data/TenantDbContext` - EF Core context with global query filters (tenant isolation + soft delete), `SetCurrentTenant()` / `ClearCurrentTenant()`, automatic timestamps.
- `Data/TenantDbContextFactory` (`ITenantDbContextFactory<TenantDbContext>`) - builds a context per request. Base `DbContextOptions<TenantDbContext>` are registered as a singleton (configured once in `AddTenantIsolation`); the factory layers the resolved tenant on top. This is where per-tenant connection strings (`TenantConnectionString` model) plug in for the `DatabasePerTenant` strategy.
- `Data/Repository<T>` - generic base (paging, bulk update/delete, exists/count); `TenantRepository`, `UserRepository`, `OrganizationRepository` add domain queries (slug lookup, expiring subscriptions, RBAC queries, statistics).

**Rationale for factory instead of plain `AddDbContext`**: the context cannot be constructed until the tenant is known, and the tenant is only known mid-request. The DI registration `services.AddScoped(sp => sp.GetRequiredService<ITenantDbContextFactory<TenantDbContext>>().Create())` keeps consumer code able to inject `TenantDbContext` directly.

## Domain models

`Models/` - all persisted via `TenantDbContext`:

| Model | Role |
|---|---|
| `Tenant` | tenant lifecycle (6 statuses, 4 isolation strategies, subscription, soft delete) |
| `User` | tenant-scoped user: lockout, 2FA, email verification; unique (TenantId, Email) |
| `Organization` | company inside a tenant |
| `TenantConfiguration` | typed key-value config per tenant, encryption flag |
| `TenantConnectionString` | per-tenant DB connection with pooling/test tracking |
| `DataIsolationPolicy` | field-level ACLs, cross-tenant access rules, priority resolution |
| `TenantFeature` | feature toggles: rollout %, usage limits, Beta/GA/Deprecated |
| `TenantUsageRecord` | usage metering records |

Model behavior lives on the entities themselves (`Tenant.CanActivate()`, `User.CanLogin()`, `TenantFeature.IsAvailable()`), with serialization and validation split into `*JsonExtensions` / `*Validation` partner files to keep entity files readable.

## Services

- `TenantService` - tenant CRUD + lifecycle (create/suspend/delete/restore)
- `ConfigurationService` - per-tenant config with caching (`ConfigurationCacheDurationMinutes`)
- `TenantFeatureService` - feature toggle evaluation
- `DataIsolationService` - enforces `DataIsolationPolicy`
- `TenantUsageMeteringService` (`ITenantUsageMeteringService`) - usage counters
- `AuditLogger` (`IAuditLogger`), `NotificationService` (`INotificationService`), `ExportService` (`IExportService`), `HealthCheckService` (`IHealthCheckService`)

## Cross-cutting infrastructure

- **Caching** (`Caching/`): `ICacheProvider` / `CacheProvider`, `ICachingService` / `CachingService`, and `TenantAwareDistributedCacheProvider` which prefixes keys with the tenant id so tenants can never read each other's cache entries. Registered only when `TenantIsolationOptions.EnableCaching` is true.
- **Events** (`Events/`): in-process `EventBus` / `EventPublisher` publishing `TenantEvent`s (opt-in via `EnableEventBus`).
- **Background tasks** (`BackgroundTasks/`): `BackgroundTaskQueue` plus two hosted workers - `SubscriptionExpirationWorker` and `TenantCleanupWorker` (opt-in via `EnableBackgroundTasks`).
- **Integration** (`Integration/`): `ExternalApiClient`, `WebhookHandler` (opt-in via `EnableWebhooks`), custom `HttpClientFactory`.
- **Middleware** (`Middleware/`): error handling, rate limiting, request context, request logging, tenant resolution. Wired via extension methods (`UseTenantResolution()` etc.).
- **Utilities** (`Utilities/`): `ITimeProvider`/`TimeProvider` (testability seam for clock access), crypto, JSON, string/collection/date extensions, distributed tracing helpers.

## Composition root

`Configuration/DependencyInjectionExtensions.AddTenantIsolation(...)` registers the DbContext factory, repositories, core services and `TenantIsolationOptions`; `AddTenantIsolationSqlServer(connectionString, ...)` is the convenience overload used by `Program.cs`. `Configuration/ServiceRegistrationExtensions.AddTenantIsolationPhase2Services(...)` layers on the optional subsystems (caching, event bus, webhooks, background tasks, audit, notifications) driven by the same options object - each subsystem is behind an `Enable*` flag so the host pays only for what it turns on.

`ConfigurationValidator` (`AddConfigurationValidator()`) validates the options at startup rather than failing lazily at first use.

## Extension points

- `IDynamicTenantStore` - replace the SQL-backed store with e.g. a config-file or control-plane-API store
- `ITenantResolutionService` - swap the strategy chain for a custom resolution scheme
- `ITenantDbContextFactory<T>` - custom per-tenant context construction (schema-per-tenant, sharding)
- `ICacheProvider` / distributed cache - swap memory cache for Redis via the standard `IDistributedCache`
- `IEventBus` / `IEventPublisher` - bridge tenant events to an external broker
- `TenantIsolationOptions.ExcludedPaths` - carve out health checks, auth endpoints etc. from resolution

## Known limitations

- Slug-based lookups are O(n) scans over the in-memory tenant list (see above).
- `DynamicTenantStore` blocks on `.Wait()` during initial load.
- Tenant cache is per-process; multi-instance deployments see updates only after each node's reload tick (no cross-node invalidation).
- The middleware currently lets requests through with a warning when no tenant resolves and the path is not excluded; strict mode (throw) is commented out in `TenantResolutionMiddleware`.
- `Program.cs` is a demo host; the project is not yet split into a pure class library + sample host.
