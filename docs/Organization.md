# Organization

Represents a tenant-specific organization entity in a multi-tenant application, storing core identity and operational details for a company or unit within a tenant boundary.

## API

### `public Guid Id`
Unique identifier for the organization. Assigned at creation and immutable for the lifetime of the record.

### `public Guid TenantId`
Identifier of the tenant to which this organization belongs. Used for data isolation and routing within multi-tenant systems.

### `public string Name`
Display name of the organization. Required and non-null; must be unique within the tenant scope.

### `public string? Slug`
URL-friendly identifier derived from `Name`. Optional; used for vanity URLs and routing. Nullable to support legacy systems or programmatic creation.

### `public string? Description`
Brief textual description of the organization. Optional; may be null for minimal setups.

### `public string? Website`
Canonical web address of the organization. Optional; typically a fully-qualified URL (e.g., `https://example.com`).

### `public string? LogoUrl`
Publicly accessible URL for the organization’s logo image. Optional; may point to a CDN or internal asset endpoint.

### `public string ContactEmail`
Primary contact email address for the organization. Required and non-null; used for notifications and support.

### `public string? ContactPhone`
Primary telephone number for the organization. Optional; may include country code and formatting (e.g., `+1 (555) 123-4567`).

### `public string? OrganizationType`
Categorical label for the organization (e.g., "Corporation", "Non-Profit", "Government"). Optional; free-form but typically constrained by application policy.

### `public int? EmployeeCount`
Approximate number of employees. Optional; may be null for privacy or early-stage organizations.

### `public string? Industry`
Industry classification (e.g., "Technology", "Healthcare"). Optional; free-form but often mapped to standard taxonomies.

### `public string? CountryCode`
ISO 3166-1 alpha-2 country code (e.g., "US", "DE"). Optional; used for regional compliance and localization.

### `public string? RegistrationNumber`
Unique identifier assigned by a government or registry (e.g., "1234567890"). Optional; relevant for regulated industries.

### `public string? TaxId`
Tax identification number (e.g., EIN, VAT ID). Optional; used for invoicing and compliance.

### `public bool IsActive`
Flag indicating whether the organization is currently active and eligible for services. Defaults to `true` on creation.

### `public DateTime CreatedAt`
Timestamp of when the organization record was created. Set automatically by the persistence layer; immutable.

### `public DateTime UpdatedAt`
Timestamp of the most recent update to the organization record. Updated automatically by the persistence layer on any write.

### `public string? Metadata`
Serialized dictionary or JSON blob for extensible key-value pairs. Optional; used to store tenant-specific or application-specific attributes without schema changes.

### `public bool IsDeleted`
Soft-delete flag. When `true`, the organization is considered logically deleted but retained for audit and recovery. Defaults to `false`.

## Usage
