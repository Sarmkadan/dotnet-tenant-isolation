using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Services;
using Xunit;

namespace TenantIsolation.Tests;

public class NotificationServiceTests
{
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _notificationService = new NotificationService(_loggerMock.Object);
    }

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

    [Fact]
    public async Task SendNotificationAsync_NullNotification_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _notificationService.SendNotificationAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

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
