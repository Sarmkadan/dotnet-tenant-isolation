# Changelog

All notable changes to the dotnet-tenant-isolation project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2025-08-24

### Added
- Add tenant migration toolkit with zero-downtime data transfer
- Docker support with multi-stage builds
- Health check endpoints (/health, /health/ready)
- Integration test suite with xUnit
- Migration guide from v1.x

### Changed
- Upgraded to .NET 10.0
- Modern C# features (records, primary constructors)
- Improved API consistency

### Fixed
- Various edge cases found through testing

## [1.1.0] - 2025-06-24

### Added
- Add tenant usage metering and quota enforcement
- Performance benchmarks with BenchmarkDotNet
- Improved input validation and error messages

### Fixed
- Edge case handling for null/empty inputs
- Resource cleanup in disposal paths
- Thread safety improvements

### Changed
- Optimized hot paths with Span<T> and object pooling
- Better exception messages with parameter details

## [1.0.0] - 2025-05-27

### Added
- NuGet packaging with full metadata (PackageId, Authors, Tags, Readme)
- Complete unit test suite across three test files
- XML documentation for all public APIs
- In-memory database support for fast, isolated unit tests
- `AddTenantIsolationInMemory` extension method for test bootstrapping
- Makefile with `build`, `test`, `pack`, `docker-build` targets
- Docker multi-stage build and docker-compose for local development
- GitHub Actions CI pipeline (build, test, publish)
- CodeQL security scanning workflow
- Dependabot configuration for NuGet and GitHub Actions updates

### Changed
- Promoted all APIs to stable — no further breaking changes in the 1.x line
- Hardened configuration validation to run at startup, not on first use
- Moved `TenantIsolationException` hierarchy into dedicated `Exceptions/` namespace

### Fixed
- Subscription expiration check had an off-by-one on boundary day
- `GetConfigurationAsync` did not respect `CacheDurationMinutes` when value was zero

## [0.9.0] - 2025-05-13

### Added
- `ConfigurationValidator` runs at `IHostedService` startup and fails fast on bad config
- `DependencyInjectionExtensions` with fluent `AddTenantIsolation*` overloads for SQL Server, PostgreSQL, and In-Memory
- `ServiceRegistrationExtensions` with `UseTenantResolution` middleware helper
- `appsettings.Development.json` with sane local defaults
- Dockerfile and docker-compose configuration
- GitHub Actions build workflow
- `.editorconfig` and `Directory.Build.props` for consistent code style

### Changed
- All service registrations moved to extension methods — `Program.cs` is now three lines
- Background workers (`SubscriptionExpirationWorker`, `TenantCleanupWorker`) registered via `IHostedService`

### Fixed
- `RequestContextMiddleware` now correctly sets `HttpContext.Items["TenantId"]` before downstream middleware runs

## [0.8.0] - 2025-04-29

### Added
- `TenantApiController` — CRUD endpoints for tenant management
- `FeaturesController` — per-tenant feature toggle read/write endpoints
- `AdminController` — bulk activate/suspend/delete operations
- `AnalyticsController` — usage statistics and billing summary endpoints
- `WebhookController` — inbound lifecycle event handler with HMAC signature verification
- `ResponseFormatter` for consistent JSON envelope shape across all controllers
- `AuditLogger` service for change-trail logging
- `ExportService` for tenant data backup

### Changed
- Controllers now resolve tenant from `HttpContext.Items` set by middleware (no redundant resolution)
- `NotificationService` uses `IHttpClientFactory` for outbound webhook calls

## [0.7.0] - 2025-04-15

### Added
- `BackgroundTaskQueue` — bounded `Channel<T>`-backed work queue
- `SubscriptionExpirationWorker` — daily scan to suspend tenants with lapsed subscriptions
- `TenantCleanupWorker` — periodic hard-delete of soft-deleted tenants past retention window
- `EventBus` and `EventPublisher` for in-process tenant lifecycle events
- `TenantEvent` base class with `TenantCreated`, `TenantSuspended`, `TenantDeleted` subtypes
- `ExternalApiClient` and `HttpClientFactory` wrappers with per-tenant auth headers
- `WebhookHandler` for dispatching outbound lifecycle notifications

### Fixed
- `TenantCleanupWorker` could double-delete a tenant if two instances ran concurrently — now uses optimistic concurrency check

## [0.6.0] - 2025-04-01

### Added
- `TenantResolutionMiddleware` — resolves tenant on every request via cascading strategy (header → claims → route → subdomain)
- `ErrorHandlingMiddleware` — catches `TenantIsolationException` subtypes and maps them to correct HTTP status codes
- `RateLimitingMiddleware` — per-tenant sliding-window rate limiting using `IMemoryCache`
- `RequestLoggingMiddleware` — structured request/response logging with correlation IDs
- `RequestContextMiddleware` — propagates resolved tenant ID into `AsyncLocal` context for non-HTTP code paths
- `DistributedTracingExtensions` — adds tenant ID to `Activity` tags for OpenTelemetry

### Changed
- Middleware order documented and enforced: `ErrorHandling → RequestLogging → TenantResolution → RateLimiting → RequestContext`

### Fixed
- Header-based resolution ignored `X-Tenant-Slug` when `X-Tenant-Id` was absent

## [0.5.0] - 2025-03-18

### Added
- `TenantFeature` model with `FeatureKey`, `IsEnabled`, `RolloutPercentage`, `UsageCount`, `UsageLimit`, `LastUsedAt`
- `TenantFeatureService` — enable, disable, set rollout percentage, record usage, get statistics
- `IsFeatureEnabledAsync` respects rollout percentage via `Random.Shared` seeded by tenant ID for determinism within a request
- Default feature seed for new tenants configurable via `TenantIsolationOptions.DefaultFeatures`
- `FeaturesController` scaffolded (no-op pending v0.8.0)

### Fixed
- `UsageLimit` of zero was incorrectly treated as unlimited instead of disabled

## [0.4.0] - 2025-03-04

### Added
- `TenantConfiguration` model with `Key`, `Value`, `ValueType`, `IsEncrypted`, `ExpiresAt`
- `ConfigurationService` — typed `GetConfigurationAsync<T>` with automatic string-to-T conversion
- `SetConfigurationAsync` with optional encryption flag
- `ImportConfigurationAsync` / `ExportConfigurationAsync` for JSON bulk migration
- `CachingService` and `CacheProvider` wrapping `IMemoryCache` with 60-minute default TTL
- `CryptographyUtility` for AES-256-GCM encrypt/decrypt of sensitive configuration values

### Changed
- Configuration reads now go through cache-aside; writes invalidate the relevant cache entry

### Fixed
- Type converter did not handle `bool` values expressed as `"True"` (capital T) from JSON import

## [0.3.0] - 2025-02-18

### Added
- `DataIsolationPolicy` model with `PolicyType` (Strict/Relaxed/Custom), `EntityType`, `RestrictedFields`, `AllowedCrossTenantAccess`, `FilterRule`
- `DataIsolationService` — create policy, check field access, evaluate cross-tenant permission, detect policy violations
- `CheckPolicyViolationsAsync` throws `DataIsolationViolationException` on breach
- `ValidationUtility` for slug, email, and policy-rule validation
- `TenantIsolationException` hierarchy: `TenantNotResolvedException`, `TenantNotActiveException`, `DataIsolationViolationException`, `ConfigurationNotFoundException`

### Changed
- `TenantDbContext` now applies global query filters for soft-delete on all `ITenantEntity` types

## [0.2.0] - 2025-02-04

### Added
- `TenantResolutionService` — cascading resolution: HTTP header, user claims, route values, subdomain
- `GetCurrentTenant()` / `GetCurrentTenantId()` / `HasTenant()` helpers
- `TenantConnectionString` model and `OrganizationRepository` with `GetActiveOrganizationsAsync`
- `UserRepository` with `GetByEmailAsync`, `GetActiveUsersForTenantAsync`, `GetUsersWithRoleAsync`
- `TenantRepository` with `GetStatusCountsAsync`, `GetExpiringSubscriptionsAsync`, `GetBillingSummaryAsync`
- `StringExtensions`, `CollectionExtensions`, `DateTimeExtensions`, `JsonUtility`
- `TenantConstants` centralising header names, claim types, cache key prefixes

### Changed
- `TenantService.CreateTenantAsync` now validates slug uniqueness before insert

### Fixed
- Subdomain extractor split on the wrong delimiter when host contained a port number

## [0.1.0] - 2025-01-17

### Added
- Initial project structure: solution file, src and tests directories
- `Tenant` model with `Id`, `Name`, `Slug`, `Status` (Active/Suspended/Trial/Inactive/Archived/Provisioning), `SubscriptionPlan`, `SubscriptionExpiresAt`, `IsDeleted`
- `User` model with tenant scoping, roles, and soft-delete
- `Organization` model linked to `Tenant`
- `TenantDbContext` (Entity Framework Core 10) with migration support
- Generic `Repository<T>` base with `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
- `TenantService` — `CreateTenantAsync`, `ActivateTenantAsync`, `SuspendTenantAsync`, `DeleteTenantAsync`, `IsSubscriptionValidAsync`, `GetTenantStatisticsAsync`
- `HealthCheckService` with database connectivity probe
- `TimeProvider` abstraction for testable date/time logic
- `TenantIsolationOptions` configuration class

---

## Support

- **Issues**: Report bugs on [GitHub Issues](https://github.com/Sarmkadan/dotnet-tenant-isolation/issues)
- **Discussions**: Ask questions on [GitHub Discussions](https://github.com/Sarmkadan/dotnet-tenant-isolation/discussions)
- **Documentation**: Read comprehensive [docs](/docs)
- **Examples**: Check [examples](/examples) directory

---

## License

MIT License - See [LICENSE](LICENSE) file for details

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com)**
