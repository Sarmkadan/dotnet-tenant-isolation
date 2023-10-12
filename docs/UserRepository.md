# UserRepository

Provides data access methods for querying and managing user entities within a multi-tenant system. Designed to support tenant isolation by scoping all operations to the current tenant context.

## API

### `UserRepository`

Initializes a new instance of the `UserRepository` with required services and tenant context.

### `async Task<User?> GetByEmailAsync(string email)`

Retrieves a single user by email address within the current tenant. The comparison is case-insensitive.

- **Parameters**
  - `email`: The email address to search for.
- **Returns**
  - A `User` instance if found; otherwise `null`.
- **Exceptions**
  - Throws `ArgumentException` if `email` is null or whitespace.

### `async Task<List<User>> GetActiveUsersInOrganizationAsync(string organizationId)`

Returns all active users belonging to the specified organization within the current tenant.

- **Parameters**
  - `organizationId`: The unique identifier of the organization.
- **Returns**
  - A list of active `User` instances. Empty list if none found.
- **Exceptions**
  - Throws `ArgumentException` if `organizationId` is null or whitespace.

### `async Task<List<User>> GetByRoleAsync(string role)`

Fetches all users assigned the specified role within the current tenant.

- **Parameters**
  - `role`: The role name to filter by.
- **Returns**
  - A list of `User` instances with the given role. Empty list if none found.
- **Exceptions**
  - Throws `ArgumentException` if `role` is null or whitespace.

### `async Task<List<User>> GetUnverifiedUsersAsync()`

Returns all users within the current tenant who have not completed email verification.

- **Returns**
  - A list of unverified `User` instances. Empty list if none found.

### `async Task<List<User>> GetNeverLoggedInAsync()`

Returns users within the current tenant who have never successfully authenticated.

- **Returns**
  - A list of `User` instances with zero or no login events. Empty list if none found.

### `async Task<List<User>> GetLockedAccountsAsync()`

Retrieves all user accounts within the current tenant that are currently locked due to failed login attempts or administrative action.

- **Returns**
  - A list of locked `User` instances. Empty list if none found.

### `async Task<int> GetUserCountAsync()`

Returns the total number of users within the current tenant.

- **Returns**
  - The count of `User` records.

### `async Task<List<User>> GetRecentlyActiveAsync(int days = 30)`

Fetches users within the current tenant who have been active within the specified number of days.

- **Parameters**
  - `days`: Number of days to look back. Defaults to 30.
- **Returns**
  - A list of `User` instances active within the time window. Empty list if none found.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `days` is less than 1.

### `async Task<List<User>> SearchAsync(string query, int maxResults = 100)`

Performs a free-text search across user names and emails within the current tenant.

- **Parameters**
  - `query`: The search term to match against user names and emails.
  - `maxResults`: Maximum number of results to return. Defaults to 100.
- **Returns**
  - A list of matching `User` instances, limited by `maxResults`.
- **Exceptions**
  - Throws `ArgumentException` if `query` is null or whitespace.
  - Throws `ArgumentOutOfRangeException` if `maxResults` is less than 1.

### `async Task<bool> IsEmailUniqueAsync(string email)`

Checks whether the specified email address is unique within the current tenant.

- **Parameters**
  - `email`: The email address to validate.
- **Returns**
  - `true` if the email is not in use; otherwise `false`.
- **Exceptions**
  - Throws `ArgumentException` if `email` is null or whitespace.

### `async Task<List<User>> GetUsersRequiringPasswordChangeAsync()`

Returns users within the current tenant who are required to change their password at next login.

- **Returns**
  - A list of `User` instances requiring a password change. Empty list if none found.

### `async Task<int> DeactivateOrganizationUsersAsync(string organizationId)`

Deactivates all users belonging to the specified organization within the current tenant.

- **Parameters**
  - `organizationId`: The unique identifier of the organization.
- **Returns**
  - The number of users deactivated.
- **Exceptions**
  - Throws `ArgumentException` if `organizationId` is null or whitespace.

### `async Task<object> GetUserStatisticsAsync()`

Generates aggregate statistics about users within the current tenant.

- **Returns**
  - An anonymous object containing counts of active, inactive, locked, unverified, and total users.
- **Exceptions**
  - Never throws.

## Usage
