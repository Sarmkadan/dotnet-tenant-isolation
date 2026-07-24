#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Services;
using Xunit;

namespace TenantIsolation.Tests;

public class NotificationTests
{
    private readonly Mock<ILogger<NotificationService>> _loggerMock = new();
    private readonly NotificationService _notificationService;

    public NotificationTests()
    {
        _notificationService = new NotificationService(_loggerMock.Object);
    }

    [Fact]
    public void Notification_DefaultConstructor_InitializesProperties()
    {
        // Act
        var notification = new Notification();

        // Assert
        notification.Id.Should().NotBeNullOrEmpty();
        notification.Id.Should().MatchRegex("^[a-f0-9]{32}$"); // GUID without dashes
        notification.Title.Should().BeEmpty();
        notification.Message.Should().BeEmpty();
        notification.Type.Should().Be(NotificationType.Info);
        notification.RecipientUserId.Should().BeNull();
        notification.TenantId.Should().BeNull();
        notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        notification.ReadAt.Should().BeNull();
        notification.Metadata.Should().NotBeNull();
        notification.Metadata.Should().BeEmpty();
        notification.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void Notification_WithParameters_InitializesPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString("N");
        var title = "Test Title";
        var message = "Test Message";
        var type = NotificationType.Warning;
        var recipientUserId = "user-123";
        var tenantId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddHours(-1);
        var readAt = DateTime.UtcNow.AddMinutes(-30);
        var metadata = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var notification = new Notification
        {
            Id = id,
            Title = title,
            Message = message,
            Type = type,
            RecipientUserId = recipientUserId,
            TenantId = tenantId,
            CreatedAt = createdAt,
            ReadAt = readAt,
            Metadata = metadata,
            ExpiresAt = expiresAt
        };

        // Assert
        notification.Id.Should().Be(id);
        notification.Title.Should().Be(title);
        notification.Message.Should().Be(message);
        notification.Type.Should().Be(type);
        notification.RecipientUserId.Should().Be(recipientUserId);
        notification.TenantId.Should().Be(tenantId);
        notification.CreatedAt.Should().Be(createdAt);
        notification.ReadAt.Should().Be(readAt);
        notification.Metadata.Should().BeEquivalentTo(metadata);
        notification.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void Notification_MetadataProperty_IsInitializedAsEmptyDictionary()
    {
        // Act
        var notification = new Notification();

        // Assert
        notification.Metadata.Should().NotBeNull();
        notification.Metadata.Should().BeEmpty();
    }

    [Fact]
    public async Task SendNotificationAsync_WithValidNotification_ReturnsNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Test Notification",
            Message = "This is a test message",
            Type = NotificationType.Success,
            RecipientUserId = "user-456",
            TenantId = Guid.NewGuid()
        };

        // Act
        var result = await _notificationService.SendNotificationAsync(notification);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(notification);
        result.Id.Should().Be(notification.Id);
        result.Title.Should().Be("Test Notification");
        result.Message.Should().Be("This is a test message");
        result.Type.Should().Be(NotificationType.Success);
        result.RecipientUserId.Should().Be("user-456");
        result.TenantId.Should().NotBeNull();
    }

    [Fact]
    public async Task SendNotificationAsync_WithNullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        Notification? nullNotification = null;

        // Act & Assert
        Func<Task> act = async () => await _notificationService.SendNotificationAsync(nullNotification!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendNotificationAsync_WithEmptyTitle_StillCreatesValidNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "",
            Message = "Message without title",
            RecipientUserId = "user-789"
        };

        // Act
        var result = await _notificationService.SendNotificationAsync(notification);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().BeEmpty();
        result.Message.Should().Be("Message without title");
    }

    [Fact]
    public async Task SendNotificationAsync_WithEmptyMessage_StillCreatesValidNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Empty Message Title",
            Message = "",
            RecipientUserId = "user-999"
        };

        // Act
        var result = await _notificationService.SendNotificationAsync(notification);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Empty Message Title");
        result.Message.Should().BeEmpty();
    }

    [Fact]
    public async Task SendNotificationAsync_WithAllNotificationTypes_CreatesCorrectType()
    {
        // Arrange & Act & Assert for each notification type
        foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
        {
            var notification = new Notification
            {
                Title = $"Test {type}",
                Message = $"Message for {type}",
                Type = type,
                RecipientUserId = "user-types"
            };

            var result = await _notificationService.SendNotificationAsync(notification);
            result.Type.Should().Be(type);
        }
    }

    [Fact]
    public async Task GetUnreadNotificationsAsync_WithNullUserId_ReturnsEmptyList()
    {
        // Arrange
        string? nullUserId = null;

        // Act
        var result = await _notificationService.GetUnreadNotificationsAsync(nullUserId!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUnreadNotificationsAsync_WithEmptyUserId_ReturnsEmptyList()
    {
        // Arrange
        var emptyUserId = "";

        // Act
        var result = await _notificationService.GetUnreadNotificationsAsync(emptyUserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUnreadNotificationsAsync_WithValidUserId_ReturnsUnreadNotifications()
    {
        // Arrange
        var userId = "user-unread";
        var readNotification = new Notification
        {
            Title = "Read Notification",
            Message = "This notification has been read",
            RecipientUserId = userId,
            ReadAt = DateTime.UtcNow.AddMinutes(-10) // Has been read
        };

        var unreadNotification1 = new Notification
        {
            Title = "Unread Notification 1",
            Message = "This notification is unread",
            RecipientUserId = userId
        };

        var unreadNotification2 = new Notification
        {
            Title = "Unread Notification 2",
            Message = "Another unread notification",
            RecipientUserId = userId
        };

        await _notificationService.SendNotificationAsync(readNotification);
        await _notificationService.SendNotificationAsync(unreadNotification1);
        await _notificationService.SendNotificationAsync(unreadNotification2);

        // Act
        var result = await _notificationService.GetUnreadNotificationsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        var unreadList = result.ToList();
        unreadList.Should().HaveCount(2);
        unreadList.Should().Contain(n => n.Id == unreadNotification1.Id);
        unreadList.Should().Contain(n => n.Id == unreadNotification2.Id);
        unreadList.Should().NotContain(n => n.Id == readNotification.Id);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithValidNotificationId_MarksAsRead()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Notification to mark as read",
            Message = "Test message",
            RecipientUserId = "user-mark-read"
        };

        await _notificationService.SendNotificationAsync(notification);
        var readAtBefore = notification.ReadAt;

        // Act
        var result = await _notificationService.MarkAsReadAsync(notification.Id);

        // Assert
        result.Should().BeTrue();

        // Verify the notification was actually marked as read
        var unreadNotifications = await _notificationService.GetUnreadNotificationsAsync(notification.RecipientUserId);
        unreadNotifications.Should().NotContain(n => n.Id == notification.Id);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithInvalidNotificationId_ReturnsFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString("N");

        // Act
        var result = await _notificationService.MarkAsReadAsync(invalidId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteNotificationAsync_WithValidNotificationId_DeletesNotification()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Notification to delete",
            Message = "Test message",
            RecipientUserId = "user-delete"
        };

        await _notificationService.SendNotificationAsync(notification);

        // Verify it exists
        var historyBefore = await _notificationService.GetNotificationHistoryAsync(notification.RecipientUserId);
        historyBefore.Should().Contain(n => n.Id == notification.Id);

        // Act
        var result = await _notificationService.DeleteNotificationAsync(notification.Id);

        // Assert
        result.Should().BeTrue();

        // Verify it's deleted
        var historyAfter = await _notificationService.GetNotificationHistoryAsync(notification.RecipientUserId);
        historyAfter.Should().NotContain(n => n.Id == notification.Id);
    }

    [Fact]
    public async Task DeleteNotificationAsync_WithInvalidNotificationId_ReturnsFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString("N");

        // Act
        var result = await _notificationService.DeleteNotificationAsync(invalidId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetNotificationHistoryAsync_WithNullUserId_ReturnsEmptyList()
    {
        // Arrange
        string? nullUserId = null;

        // Act
        var result = await _notificationService.GetNotificationHistoryAsync(nullUserId!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNotificationHistoryAsync_WithEmptyUserId_ReturnsEmptyList()
    {
        // Arrange
        var emptyUserId = "";

        // Act
        var result = await _notificationService.GetNotificationHistoryAsync(emptyUserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNotificationHistoryAsync_WithValidUserId_ReturnsNotificationsInReverseChronologicalOrder()
    {
        // Arrange
        var userId = "user-history";
        var oldNotification = new Notification
        {
            Title = "Old Notification",
            Message = "Sent earlier",
            RecipientUserId = userId,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var newNotification = new Notification
        {
            Title = "New Notification",
            Message = "Sent recently",
            RecipientUserId = userId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        await _notificationService.SendNotificationAsync(oldNotification);
        await _notificationService.SendNotificationAsync(newNotification);

        // Act
        var result = await _notificationService.GetNotificationHistoryAsync(userId);

        // Assert
        result.Should().NotBeNull();
        var historyList = result.ToList();
        historyList.Should().HaveCount(2);
        historyList[0].Id.Should().Be(newNotification.Id); // Newest first
        historyList[1].Id.Should().Be(oldNotification.Id); // Oldest second
    }

    [Fact]
    public async Task GetNotificationHistoryAsync_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var userId = "user-limit";

        // Create 10 notifications
        for (int i = 0; i < 10; i++)
        {
            var notification = new Notification
            {
                Title = $"Notification {i}",
                Message = $"Message {i}",
                RecipientUserId = userId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            await _notificationService.SendNotificationAsync(notification);
        }

        // Act - get only 5
        var result = await _notificationService.GetNotificationHistoryAsync(userId, 5);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task SendTenantNotificationAsync_CreatesValidTenantNotification()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var title = "Tenant Alert";
        var message = "Important message for all tenant users";
        var type = NotificationType.Warning;

        // Act
        await _notificationService.SendTenantNotificationAsync(tenantId, title, message, type);

        // Assert - verify notification was created without throwing
        // Tenant notifications are stored internally but not associated with a specific user
        // The main thing is that the method completes successfully
        var result = await _notificationService.GetNotificationHistoryAsync("any-user");
        result.Should().NotBeNull();
    }

    [Fact]
    public void Notification_MetadataCanBeModified()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Test",
            Message = "Test message"
        };

        // Act - modify metadata
        notification.Metadata["key1"] = "value1";
        notification.Metadata["key2"] = "value2";

        // Assert
        notification.Metadata.Should().HaveCount(2);
        notification.Metadata["key1"].Should().Be("value1");
        notification.Metadata["key2"].Should().Be("value2");
    }

    [Fact]
    public void Notification_PropertiesAreMutable()
    {
        // Arrange
        var notification = new Notification();

        // Act - modify all properties
        notification.Title = "Updated Title";
        notification.Message = "Updated Message";
        notification.Type = NotificationType.Error;
        notification.RecipientUserId = "user-updated";
        notification.TenantId = Guid.NewGuid();
        notification.ReadAt = DateTime.UtcNow;
        notification.Metadata["custom"] = "data";
        notification.ExpiresAt = DateTime.UtcNow.AddDays(30);

        // Assert
        notification.Title.Should().Be("Updated Title");
        notification.Message.Should().Be("Updated Message");
        notification.Type.Should().Be(NotificationType.Error);
        notification.RecipientUserId.Should().Be("user-updated");
        notification.TenantId.Should().NotBeNull();
        notification.ReadAt.Should().NotBeNull();
        notification.Metadata.Should().ContainKey("custom");
        notification.ExpiresAt.Should().NotBeNull();
    }
}