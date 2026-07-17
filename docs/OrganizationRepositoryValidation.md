# OrganizationRepositoryValidation

`OrganizationRepositoryValidation` provides a collection of static validation helpers used by the `OrganizationRepository` to verify input parameters and business rules before executing repository operations. Each helper returns a list of validation error messages, or a boolean indicating overall validity, and can optionally throw an exception when validation fails.

## API

### `public static IReadOnlyList<string> Validate`
Validates the generic state required for most repository operations.  
**Parameters:** None.  
**Returns:** A read‑only list of error messages; an empty list indicates no validation errors.  
**Throws:** Never.

### `public static bool IsValid`
Indicates whether the generic validation passes.  
**Parameters:** None.  
**Returns:** `true` if `Validate` returns an empty list; otherwise `false`.  
**Throws:** Never.

### `public static void EnsureValid`
Ensures that the generic validation succeeds, throwing an `ArgumentException` if any validation errors are present.  
**Parameters:** None.  
**Returns:** Nothing.  
**Throws:** `ArgumentException` when `Validate` yields one or more error messages.

### `public static IReadOnlyList<string> ValidateGetBySlugAsync`
Validates the input for `GetBySlugAsync(Guid tenantId, string slug)`.  
**Parameters:**  
- `slug` (implicitly required by the method signature).  
**Returns:** Errors related to a null, empty, or improperly formatted slug.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateGetActiveOrganizationsAsync`
Validates parameters for retrieving active organizations.  
**Parameters:** None.  
**Returns:** Errors if the calling context lacks required tenant information.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateGetWithUsersAsync`
Validates the request to fetch organizations together with their users.  
**Parameters:** None.  
**Returns:** Errors concerning missing tenant identifiers or insufficient permissions.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateGetByIndustryAsync`
Validates the industry filter used by `GetByIndustryAsync`.  
**Parameters:**  
- `industry` (implicitly required).  
**Returns:** Errors when `industry` is null, empty, or not recognized.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateGetByCountryAsync`
Validates the country filter for `GetByCountryAsync`.  
**Parameters:**  
- `countryCode` (implicitly required).  
**Returns:** Errors for null, empty, or invalid ISO country codes.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateSearchAsync`
Validates search criteria supplied to `SearchAsync`.  
**Parameters:**  
- `searchTerm` (implicitly required).  
**Returns:** Errors when the search term is null, empty, or exceeds allowed length.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateGetOrganizationCountAsync`
Validates parameters for counting organizations.  
**Parameters:** None.  
**Returns:** Errors if the tenant context is missing or if filters are contradictory.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateIsSlugUniqueAsync`
Validates the uniqueness check for an organization slug.  
**Parameters:**  
- `slug` (implicitly required).  
**Returns:** Errors when `slug` is null, empty, or contains prohibited characters.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateGetOrganizationsWithUserCountAsync`
Validates the request to retrieve organizations together with their user counts.  
**Parameters:** None.  
**Returns:** Errors related to missing tenant identifiers or invalid pagination parameters.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateGetByRegistrationNumberAsync`
Validates the registration number used by `GetByRegistrationNumberAsync`.  
**Parameters:**  
- `registrationNumber` (implicitly required).  
**Returns:** Errors when the registration number is null, empty, or fails format validation.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateDeactivateAsync`
Validates the deactivation request for an organization.  
**Parameters:**  
- `organizationId` (implicitly required).  
**Returns:** Errors if the identifier is empty or if the organization is already inactive.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateGetStatisticsAsync`
Validates parameters for retrieving organization statistics.  
**Parameters:** None.  
**Returns:** Errors when the tenant context is missing or when the organization does not exist.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateBulkActivateAsync`
Validates a bulk activation request.  
**Parameters:**  
- `organizationIds` (implicitly required).  
**Returns:** Errors for null collections, empty collections, or identifiers that do not correspond to existing organizations.  
**Throws:** Never.

### `public static IReadOnlyList<string> ValidateGetRecentAsync`
Validates the request to fetch recently created organizations.  
**Parameters:** None.  
**Returns:** Errors if the caller lacks permission to view recent entities or if the supplied date range is invalid.  
**Throws:** Never.

## Usage

