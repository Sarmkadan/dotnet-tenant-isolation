# Notification

Represents a tenant-scoped notification entity used to deliver messages to users within a multi-tenant application. The type encapsulates notification content, delivery metadata, and lifecycle tracking such as read status and expiration. It integrates with the `NotificationService` for sending, querying, and managing notifications across tenants.

## API

### Properties

- **`Id`** (string)
  Unique identifier for the notification. Assigned during creation.

- **`Title`** (string)
  Human-readable title or subject of the notification.

- **`Message`** (string)
  Main content or body of the notification.

- **`Type`** (NotificationType)
  Categorizes the notification (e.g., Info, Warning, Error). Defines the semantic purpose of the message.

- **`RecipientUserId`** (string?)
  Identifier of the user intended to receive the notification. Optional if targeting all users in a tenant.

- **`TenantId`** (Guid?)
  Identifier of the tenant associated with the notification. Optional if global or system-wide.

- **`CreatedAt`** (DateTime)
  Timestamp when the notification was created. Immutable after creation.

- **`ReadAt`** (DateTime?)
  Timestamp when the notification was marked as read. `null` if unread.

- **`Metadata`** (Dictionary<string, string>)
  Additional key-value pairs for extensibility (e.g., routing, correlation IDs).

- **`ExpiresAt`** (DateTime?)
  Optional expiration timestamp. Notifications may be automatically cleaned up after this time.

- **`NotificationService`** (NotificationService)
  Reference to the service used to manage this notification (e.g., sending, querying). Not settable externally.

### Methods

- **`SendNotificationAsync()`** → `Task<Notification>`
  Sends the notification to the specified recipient and tenant. Returns the persisted notification with updated metadata (e.g., `Id`, `CreatedAt`). Throws if `RecipientUserId` or `TenantId` is invalid or if persistence fails.

- **`SendTenantNotificationAsync()`** → `Task`
  Broadcasts the notification to all users within the specified tenant. Throws if `TenantId` is invalid or if persistence or broadcast fails.

- **`GetUnreadNotificationsAsync()`** → `Task<IEnumerable<Notification>>`
  Retrieves all unread notifications for the current tenant and recipient. Returns an empty collection if none exist. Throws on query failure.

- **`MarkAsReadAsync()`** → `Task<bool>`
  Marks the notification as read by setting `ReadAt` to the current UTC time. Returns `true` if updated; `false` if already read or not found. Throws on persistence failure.

- **`DeleteNotificationAsync()`** → `Task<bool>`
  Removes the notification from the system. Returns `true` if deleted; `false` if not found. Throws on deletion failure.

- **`GetNotificationHistoryAsync()`** → `Task<IEnumerable<Notification>>`
  Retrieves all notifications (read and unread) for the current tenant and recipient, ordered by `CreatedAt` descending. Returns an empty collection if none exist. Throws on query failure.

- **`AddNotificationService(IServiceCollection)`** → `IServiceCollection`
  Configures the notification service and its dependencies in the dependency injection container. Returns the collection for chaining. Must be called during application startup.

## Usage

### Example 1: Sending a Tenant-Specific Notification
