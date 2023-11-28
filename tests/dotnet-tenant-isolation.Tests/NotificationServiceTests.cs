using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Services;
using Xunit;

/// <summary>
/// Tests for the NotificationService class.
/// </summary>
public class NotificationServiceTests
{
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    internal readonly NotificationService _notificationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationServiceTests"/> class.
    /// </summary>
    public NotificationServiceTests()
    {
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _notificationService = new NotificationService(_loggerMock.Object);
    }

    /// <summary>
    /// Tests that a valid notification is added to the database when SendNotificationAsync is called.
    /// </summary>
    [Fact]
    public async Task SendNotificationAsync_ValidNotification_AddsNotification()
    {
        // Arrange
        var notification = new Notification { Title = "Test", Message = "Msg", RecipientUserId = "user1" };

        // Act
        var result = await _notificationService.SendNotificationAsync(notification);

        // Assert
        result.Should().Be(notification);
        var unread = await _notificationService.GetUnreadNotificationsAsync("user1");
        unread.Should().ContainSingle().Which.Id.Should().Be(notification.Id);
    }

    /// <summary>
    /// Tests that an ArgumentNullException is thrown when SendNotificationAsync is called with a null notification.
    /// </summary>
    [Fact]
    public async Task SendNotificationAsync_NullNotification_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _notificationService.SendNotificationAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that only unread notifications are returned when GetUnreadNotificationsAsync is called.
    /// </summary>
    [Fact]
    public async Task GetUnreadNotificationsAsync_WithMultipleNotifications_ReturnsOnlyUnread()
    {
        // Arrange
        var userId = "user1";
        var n1 = new Notification { RecipientUserId = userId, Title = "N1" };
        var n2 = new Notification { RecipientUserId = userId, Title = "N2" };
        await _notificationService.SendNotificationAsync(n1);
        await _notificationService.SendNotificationAsync(n2);
        await _notificationService.MarkAsReadAsync(n1.Id);

        // Act
        var unread = await _notificationService.GetUnreadNotificationsAsync(userId);

        // Assert
        unread.Should().ContainSingle().Which.Id.Should().Be(n2.Id);
    }

    /// <summary>
    /// Tests that a notification is marked as read when MarkAsReadAsync is called.
    /// </summary>
    [Fact]
    public async Task MarkAsReadAsync_ExistingNotification_MarksAsRead()
    {
        // Arrange
        var n = new Notification { RecipientUserId = "u1", Title = "N" };
        await _notificationService.SendNotificationAsync(n);

        // Act
        var result = await _notificationService.MarkAsReadAsync(n.Id);

        // Assert
        result.Should().BeTrue();
        var unread = await _notificationService.GetUnreadNotificationsAsync("u1");
        unread.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a notification is deleted when DeleteNotificationAsync is called.
    /// </summary>
    [Fact]
    public async Task DeleteNotificationAsync_ExistingNotification_RemovesIt()
    {
        // Arrange
        var n = new Notification { RecipientUserId = "u1", Title = "N" };
        await _notificationService.SendNotificationAsync(n);

        // Act
        var result = await _notificationService.DeleteNotificationAsync(n.Id);

        // Assert
        result.Should().BeTrue();
        var history = await _notificationService.GetNotificationHistoryAsync("u1");
        history.Should().BeEmpty();
    }
}
