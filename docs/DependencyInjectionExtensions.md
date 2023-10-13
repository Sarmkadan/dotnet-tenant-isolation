# DependencyInjectionExtensions

Provides extension methods for configuring tenant isolation in ASP.NET Core applications, including tenant resolution, feature toggles, and storage provider registrations.

## API

### `AddTenantIsolation(IServiceCollection services, Action<TenantIsolationOptions> configureOptions = null)`
Registers core tenant isolation services with optional configuration. The method configures tenant resolution, feature toggles, and default isolation strategy.

- **Parameters**
  - `services`: The `IServiceCollection` to add services to.
  - `configureOptions`: Optional action to configure `TenantIsolationOptions`.
- **Return Value**: The configured `IServiceCollection`.
- **Throws**: `ArgumentNullException` if `services` is `null`.

### `UseTenantResolution(IApplicationBuilder app)`
Configures the HTTP pipeline to resolve tenants from incoming requests using registered tenant resolvers.

- **Parameters**
  - `app`: The `IApplicationBuilder` to configure.
- **Return Value**: The configured `IApplicationBuilder`.
- **Throws**: `ArgumentNullException` if `app` is `null`.

### `AddTenantIsolationInMemory(IServiceCollection services, Action<TenantIsolationOptions> configureOptions = null)`
Registers in-memory tenant storage and resolution services.

- **Parameters**
  - `services`: The `IServiceCollection` to add services to.
  - `configureOptions`: Optional action to configure `TenantIsolationOptions`.
- **Return Value**: The configured `IServiceCollection`.
- **Throws**: `ArgumentNullException` if `services` is `null`.

### `AddTenantIsolationSqlServer(IServiceCollection services, string connectionString, Action<TenantIsolationOptions> configureOptions = null)`
Registers SQL Server-backed tenant storage and resolution services.

- **Parameters**
  - `services`: The `IServiceCollection` to add services to.
  - `connectionString`: The SQL Server connection string.
  - `configureOptions`: Optional action to configure `TenantIsolationOptions`.
- **Return Value**: The configured `IServiceCollection`.
- **Throws**: `ArgumentNullException` if `services` or `connectionString` is `null`.

### `AddTenantIsolationPostgres(IServiceCollection services, string connectionString, Action<TenantIsolationOptions> configureOptions = null)`
Registers PostgreSQL-backed tenant storage and resolution services.

- **Parameters**
  - `services`: The `IServiceCollection` to add services to.
  - `connectionString`: The PostgreSQL connection string.
  - `configureOptions`: Optional action to configure `TenantIsolationOptions`.
- **Return Value**: The configured `IServiceCollection`.
- **Throws**: `ArgumentNullException` if `services` or `connectionString` is `null`.

### `AddTenantFeatureToggle(IServiceCollection services)`
Registers feature toggle services for tenant-specific feature control.

- **Parameters**
  - `services`: The `IServiceCollection` to add services to.
- **Return Value**: The configured `IServiceCollection`.
- **Throws**: `ArgumentNullException` if `services` is `null`.

### `AutoMigrate`
Gets or sets a value indicating whether automatic database migrations should be applied on startup.

- **Type**: `bool`
- **Default**: `false`

### `MaxConcurrentTenants`
Gets or sets the maximum number of concurrent tenants allowed.

- **Type**: `int`
- **Default**: `100`

### `EnableSoftDeleteFilter`
Gets or sets a value indicating whether soft-delete filters should be enabled for tenant data.

- **Type**: `bool`
- **Default**: `true`

### `EnableAuditLogging`
Gets or sets a value indicating whether audit logging for tenant operations should be enabled.

- **Type**: `bool`
- **Default**: `true`

### `DefaultIsolationStrategy`
Gets or sets the default tenant isolation strategy (e.g., "Header", "Host", "Path").

- **Type**: `string`
- **Default**: `"Header"`

### `ConfigurationCacheDurationMinutes`
Gets or sets the duration (in minutes) for caching tenant configuration.

- **Type**: `int`
- **Default**: `5`

### `ValidateTenantOnEveryRequest`
Gets or sets a value indicating whether tenant validation should occur on every request.

- **Type**: `bool`
- **Default**: `false`

### `ExcludedPaths`
Gets or sets a list of request paths excluded from tenant resolution.

- **Type**: `List<string>`
- **Default**: Empty list

### `EnableWebhooks`
Gets or sets a value indicating whether webhook notifications for tenant events should be enabled.

- **Type**: `bool`
- **Default**: `false`

### `EnableCaching`
Gets or sets a value indicating whether response caching should be enabled.

- **Type**: `bool`
- **Default**: `true`

### `EnableEventBus`
Gets or sets a value indicating whether event bus integration should be enabled.

- **Type**: `bool`
- **Default**: `true`

### `EnableBackgroundTasks`
Gets or sets a value indicating whether background task processing should be enabled.

- **Type**: `bool`
- **Default**: `true`

### `EnableNotifications`
Gets or sets a value indicating whether tenant-related notifications should be enabled.

- **Type**: `bool`
- **Default**: `true`

### `EnableDistributedTracing`
Gets or sets a value indicating whether distributed tracing for tenant operations should be enabled.

- **Type**: `bool`
- **Default**: `false`

## Usage

### Basic Setup with In-Memory Storage
