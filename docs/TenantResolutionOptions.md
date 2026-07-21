# TenantResolutionOptions

Configuration options for tenant resolution strategies in multi-tenant applications. This class defines how tenant identifiers are discovered and validated during request processing, including fallback behavior and validation rules.

## API

### `List<TenantResolutionStrategy> ResolutionStrategies`
Gets or sets the ordered list of tenant resolution strategies to attempt during tenant resolution. Strategies are executed in sequence until a valid tenant is identified or all strategies are exhausted. Must contain at least one strategy if `ThrowOnResolutionFailure` is `true`.

### `Guid? DefaultTenantId`
Gets or sets the default tenant identifier to use when no tenant can be resolved and `ThrowOnResolutionFailure` is `false`. When set, this value takes precedence over `DefaultTenantSlug`. Must be a valid GUID if specified.

### `string? DefaultTenantSlug`
Gets or sets the default tenant slug to use when no tenant can be resolved and `ThrowOnResolutionFailure` is `false`. When set, this value is used only if `DefaultTenantId` is not specified. Must be a non-empty string if specified.

### `bool ThrowOnResolutionFailure`
Gets or sets a value indicating whether to throw an exception when tenant resolution fails. If `true`, resolution failures result in an exception; if `false`, the system falls back to `DefaultTenantId` or `DefaultTenantSlug` if provided. Defaults to `true`.

### `void Validate()`
Validates the current configuration. Throws an exception if:
- `ResolutionStrategies` is empty or contains invalid strategies.
- Both `DefaultTenantId` and `DefaultTenantSlug` are specified.
- `DefaultTenantSlug` is empty or whitespace.
- Any strategy in `ResolutionStrategies` is `null`.

### `static TenantResolutionOptions CreateDefault()`
Creates a new instance of `TenantResolutionOptions` with default values:
- `ResolutionStrategies` initialized to an empty list.
- `DefaultTenantId` set to `null`.
- `DefaultTenantSlug` set to `null`.
- `ThrowOnResolutionFailure` set to `true`.

## Usage
