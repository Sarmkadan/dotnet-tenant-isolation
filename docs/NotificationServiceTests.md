# NotificationServiceTests

Unit test suite for the `NotificationService` class, verifying its core notification lifecycle operations—sending, retrieving unread items, marking as read, and deletion—under both valid and invalid input conditions. Each test method targets a specific behavior or failure mode to ensure the service adheres to its contract.

## API

### NotificationServiceTests

Constructor for the test class. Initializes the test harness, typically setting up mocked dependencies and the system under test before each test run.

### async Task SendNotificationAsync_ValidNotification_AddsNotification

Validates that calling `SendNotificationAsync` with a properly constructed notification object results in the notification being persisted. The test asserts that the added notification appears in the subsequent retrieval of unread notifications.

- **Parameters:** None (self-contained test method).
- **Returns:** A completed `Task` representing the asynchronous test operation.
- **Throws:** Test assertion failures if the notification is not found after sending.

### async Task SendNotificationAsync_NullNotification_ThrowsArgumentNullException

Ensures that `SendNotificationAsync` throws an `ArgumentNullException` when invoked with a `null` notification argument. Confirms the service’s input validation guard.

- **Parameters:** None.
- **Returns:** A completed `Task`.
- **Throws:** Test fails if the expected exception type is not thrown.

### async Task GetUnreadNotificationsAsync_WithMultipleNotifications_ReturnsOnlyUnread

Verifies that `GetUnreadNotificationsAsync` filters correctly when multiple notifications exist with mixed read states. Only notifications whose `IsRead` flag is `false` should be returned.

- **Parameters:** None.
- **Returns:** A completed `Task`.
- **Throws:** Test assertion failures if the returned collection includes read notifications or omits unread ones.

### async Task MarkAsReadAsync_ExistingNotification_MarksAsRead

Confirms that calling `MarkAsReadAsync` on an existing notification toggles its `IsRead` status to `true`. The test typically retrieves the notification afterward to assert the state change.

- **Parameters:** None.
- **Returns:** A completed `Task`.
- **Throws:** Test fails if the notification’s `IsRead` property remains `false` after the operation.

### async Task DeleteNotificationAsync_ExistingNotification_RemovesIt

Tests that `DeleteNotificationAsync` permanently removes an existing notification from the backing store. A subsequent query for that notification should yield no result.

- **Parameters:** None.
- **Returns:** A completed `Task`.
- **Throws:** Test assertion failures if the notification is still retrievable after deletion.

## Usage

```csharp
// Example 1: Typical test arrangement using a mock repository
[Fact]
public async Task SendNotificationAsync_ValidNotification_AddsNotification()
{
    // Arrange
    var repository = new Mock<INotificationRepository>();
    var service = new NotificationService(repository.Object);
    var notification = new Notification { Id = Guid.NewGuid(), Message = "Test" };

    repository.Setup(r => r.AddAsync(It.IsAny<Notification>()))
              .Returns(Task.CompletedTask)
              .Verifiable();

    // Act
    await service.SendNotificationAsync(notification);

    // Assert
    repository.Verify(r => r.AddAsync(notification), Times.Once);
}
```

```csharp
// Example 2: Testing exception behavior with null input
[Fact]
public async Task SendNotificationAsync_NullNotification_ThrowsArgumentNullException()
{
    // Arrange
    var service = new NotificationService(Mock.Of<INotificationRepository>());

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(
        () => service.SendNotificationAsync(null));
}
```

## Notes

- **Edge Cases:** Tests for `GetUnreadNotificationsAsync` assume at least one read and one unread notification exist; an empty store or all-read scenario should be covered by additional boundary tests. `MarkAsReadAsync` expects the target notification to exist—behavior for a non-existent ID is not covered here and may throw or no-op depending on implementation.
- **Thread Safety:** These test methods are designed for sequential execution within a single test runner context. They do not validate concurrent access to the service. If `NotificationService` is intended for multi-threaded use, separate concurrency tests should be authored to verify atomicity of operations like `MarkAsReadAsync` and `DeleteNotificationAsync`.
