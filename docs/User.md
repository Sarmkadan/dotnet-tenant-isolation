# User

Represents a user within a multi-tenant system, encapsulating identity, authentication state, and tenant-specific context. The type models core user attributes required for authentication, authorization, and profile management in a tenant-isolated application.

## API

### `Id`
- **Purpose**: Uniquely identifies the user within the system.
- **Type**: `Guid`
- **Constraints**: Read-only; assigned at creation and immutable thereafter.

### `TenantId`
- **Purpose**: Identifies the tenant to which the user belongs, enabling tenant isolation.
- **Type**: `Guid`
- **Constraints**: Read-only; assigned at creation and immutable thereafter.

### `OrganizationId`
- **Purpose**: Identifies the organization within the tenant that the user belongs to.
- **Type**: `Guid`
- **Constraints**: Read-only; assigned at creation and immutable thereafter.

### `Email`
- **Purpose**: The user's primary email address, used for login and communication.
- **Type**: `string`
- **Constraints**: Non-null; must be a valid email format. Used as a unique identifier within a tenant.

### `FirstName`
- **Purpose**: The user's given name.
- **Type**: `string`
- **Constraints**: Optional; may be null or empty.

### `LastName`
- **Purpose**: The user's family name.
- **Type**: `string`
- **Constraints**: Optional; may be null or empty.

### `Role`
- **Purpose**: The user's assigned role within the system, used for authorization.
- **Type**: `string`
- **Constraints**: Non-null; typically one of a predefined set of role names (e.g., "Admin", "Member").

### `PasswordHash`
- **Purpose**: The hashed representation of the user's password, used for authentication.
- **Type**: `string?`
- **Constraints**: Optional; null only during initial creation before a password is set. Never exposed in plaintext.

### `IsActive`
- **Purpose**: Indicates whether the user account is active and usable.
- **Type**: `bool`
- **Default**: `true`
- **Constraints**: Read-only after creation unless explicitly updated.

### `IsEmailVerified`
- **Purpose**: Indicates whether the user has verified their email address.
- **Type**: `bool`
- **Default**: `false`
- **Constraints**: Read-only after creation unless explicitly updated.

### `IsTwoFactorEnabled`
- **Purpose**: Indicates whether two-factor authentication is enabled for the user.
- **Type**: `bool`
- **Default**: `false`
- **Constraints**: Read-only after creation unless explicitly updated.

### `LastLoginAt`
- **Purpose**: Timestamp of the user's most recent successful login.
- **Type**: `DateTime?`
- **Constraints**: Null until first successful login; updated automatically on successful authentication.

### `FailedLoginAttempts`
- **Purpose**: Counts consecutive failed login attempts, used for account lockout logic.
- **Type**: `int`
- **Default**: `0`
- **Constraints**: Incremented on failed login attempts; reset to `0` on successful login.

### `LockedUntil`
- **Purpose**: Timestamp until which the account is locked due to too many failed login attempts.
- **Type**: `DateTime?`
- **Constraints**: Null when account is not locked; otherwise, a future timestamp. Automatically cleared on successful login or when lockout period expires.

### `PhoneNumber`
- **Purpose**: The user's phone number, used for two-factor authentication or communication.
- **Type**: `string?`
- **Constraints**: Optional; may be null or empty.

### `AvatarUrl`
- **Purpose**: URL to the user's profile image or avatar.
- **Type**: `string?`
- **Constraints**: Optional; may be null or empty.

### `Preferences`
- **Purpose**: Serialized user-specific preferences or settings.
- **Type**: `string?`
- **Constraints**: Optional; may be null or empty. Format is application-defined.

### `LastPasswordChangeAt`
- **Purpose**: Timestamp of the user's most recent password change.
- **Type**: `DateTime?`
- **Constraints**: Null until first password change; updated automatically when password is changed.

### `CreatedAt`
- **Purpose**: Timestamp when the user record was created.
- **Type**: `DateTime`
- **Constraints**: Read-only; assigned at creation and immutable thereafter.

### `UpdatedAt`
- **Purpose**: Timestamp of the last modification to the user record.
- **Type**: `DateTime`
- **Constraints**: Read-only; updated automatically on any modification.

## Usage

### Creating a new user
