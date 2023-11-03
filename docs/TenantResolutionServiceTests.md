# TenantResolutionServiceTests

Unit tests for the `TenantResolutionService` class, verifying tenant resolution behavior across multiple strategies including headers, route values, claims, and subdomains. The tests validate correct tenant identification, caching behavior, error handling for inactive tenants, and edge cases like reserved subdomains and case mismatches.

## API

### `TenantResolutionServiceTests`

The test class containing unit tests for tenant resolution functionality in `TenantResolutionService`. This class uses xUnit and Moq to mock dependencies and verify tenant resolution behavior under various scenarios.

### `ResolveTenantAsync_WithValidTenantIdHeader_ResolvesFromHeader`

Verifies that when a valid `X-Tenant-Id` header is present, the service resolves the tenant from the header value. The test mocks the HTTP context to include the header and asserts that the resolved tenant matches the expected tenant ID.

### `ResolveTenantAsync_WithValidTenantSlugHeader_ResolvesFromHeader`

Ensures that a valid `X-Tenant-Slug` header is correctly processed to resolve the tenant. The test sets up the header in the HTTP context and validates that the resolved tenant corresponds to the slug value.

### `ResolveTenantAsync_WithInvalidTenantIdHeader_TriesNextStrategy`

Confirms that when an invalid tenant ID header is provided, the service proceeds to attempt resolution using subsequent strategies rather than failing immediately. The test mocks an invalid tenant ID and verifies that the next strategy is invoked.

### `ResolveTenantAsync_WithValidTenantIdClaim_ResolvesFromClaims`

Validates that a valid tenant ID claim in the user's identity is used to resolve the tenant. The test sets up a claims principal with the tenant ID claim and asserts that the tenant is resolved correctly from the claim.

### `ResolveTenantAsync_WithValidTenantSlugClaim_ResolvesFromClaims`

Ensures that a valid tenant slug claim is processed to resolve the tenant. The test configures the claims principal with the slug claim and verifies that the tenant is resolved from the claim value.

### `ResolveTenantAsync_WithoutAuthentication_SkipsClaimsResolution`

Confirms that when no user is authenticated (i.e., no claims principal), the service skips claims-based resolution entirely. The test ensures that claims resolution is not attempted and that resolution proceeds to other strategies if available.

### `ResolveTenantAsync_WithValidTenantIdRoute_ResolvesFromRoute`

Verifies that a tenant ID provided via route data (e.g., `{tenantId}` in the route template) is used to resolve the tenant. The test mocks the route data and asserts that the tenant is resolved from the route value.

### `ResolveTenantAsync_WithValidTenantSlugRoute_ResolvesFromRoute`

Ensures that a tenant slug provided via route data is correctly processed to resolve the tenant. The test sets up the route data with the slug and validates that the resolved tenant matches the slug value.

### `ResolveTenantAsync_WithValidTenantSubdomain_ResolvesFromSubdomain`

Validates that a valid tenant subdomain in the host (e.g., `tenant.example.com`) is used to resolve the tenant. The test mocks the host header with a subdomain and asserts that the tenant is resolved from the subdomain.

### `ResolveTenantAsync_WithReservedSubdomain_IgnoresSubdomainResolution`

Confirms that when the subdomain matches a reserved value (e.g., `www`, `admin`), the service ignores subdomain-based resolution. The test sets up a reserved subdomain and verifies that subdomain resolution is skipped.

### `ResolveTenantAsync_WithReservedSubdomains_AllIgnoredDuringResolution`

Extends the reserved subdomain behavior to ensure that all reserved subdomains are ignored during resolution. The test iterates over a list of reserved subdomains and verifies that none are used for tenant resolution.

### `ResolveTenantAsync_WithSubdomainCaseMismatch_StillResolves`

Ensures that subdomain resolution is case-insensitive, so a subdomain like `TENANT` still resolves the tenant correctly. The test mocks a case-mismatched subdomain and asserts that the tenant is resolved despite the mismatch.

### `ResolveTenantAsync_CachesTenantInHttpContext`

Verifies that once a tenant is resolved, it is cached in the `HttpContext.Items` collection to avoid repeated resolution on subsequent requests. The test asserts that the tenant is stored in the context after resolution.

### `ResolveTenantAsync_ReturnsCachedTenantOnSecondCall`

Confirms that a second call to `ResolveTenantAsync` returns the cached tenant from `HttpContext.Items` instead of re-resolving. The test calls the method twice and asserts that the same tenant is returned both times.

### `ResolveTenantAsync_WithInactiveTenant_ThrowsTenantNotActiveException`

Validates that attempting to resolve a tenant with an inactive status results in a `TenantNotActiveException` being thrown. The test mocks an inactive tenant and asserts that the exception is thrown.

### `ResolveTenantAsync_WithNonActiveStatus_ThrowsException`

Ensures that any non-active tenant status (e.g., `Pending`, `Suspended`) causes the resolution to fail with an exception. The test configures the tenant with a non-active status and verifies that an exception is thrown.

### `ResolveTenantAsync_WithoutHttpContext_ThrowsTenantNotResolvedException`

Confirms that when no `HttpContext` is available (e.g., in a background service), the method throws a `TenantNotResolvedException`. The test invokes the method without an `HttpContext` and asserts that the exception is thrown.

### `ResolveTenantAsync_WithNoResolutionStrategies_ThrowsTenantNotResolvedException`

Validates that when no tenant resolution strategies are configured, the method throws a `TenantNotResolvedException`. The test sets up the service with no strategies and asserts that the exception is thrown.

### `GetCurrentTenant_WithCachedTenant_ReturnsTenant`

Ensures that `GetCurrentTenant` retrieves the tenant from the cached value in `HttpContext.Items` when available. The test caches a tenant and asserts that `GetCurrentTenant` returns it.

## Usage

### Example 1: Testing Header-Based Tenant Resolution
