using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TenantIsolation.Services;

namespace TenantIsolation.Tests;

public static class NotificationServiceTestsExtensions
{
    /// <summary>
    /// Creates a notification with the specified properties and sends it asynchronously.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    /// <param name="recipientUserId">The recipient user identifier.</param>
    /// <param name="isRead">Whether the notification should be marked as read initially.</param>
    /// <returns>The created and sent notification.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="title"/> or <paramref name="message"/> or <paramref name="recipientUserId"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the notification service fails to send the notification.</exception>
    public static async Task<Notification> CreateAndSendTestNotificationAsync(
        this NotificationServiceTests tests,
        string title,
        string message,
        string recipientUserId,
        bool isRead = false)
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(recipientUserId);

        var notification = new Notification
        {
            Title = title,
            Message = message,
            RecipientUserId = recipientUserId
        };

        var result = await tests._notificationService.SendNotificationAsync(notification);

        if (isRead)
        {
            await tests._notificationService.MarkAsReadAsync(result.Id);
        }

        return result;
    }

    /// <summary>
    /// Sends a notification asynchronously using the test's notification service.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="notification">The notification to send.</param>
    /// <returns>The sent notification.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="notification"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the notification service fails to send the notification.</exception>
    public static async Task<Notification> SendTestNotificationAsync(
        this NotificationServiceTests tests,
        Notification notification)
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentNullException.ThrowIfNull(notification);

        return await tests._notificationService.SendNotificationAsync(notification);
    }

    /// <summary>
    /// Marks a notification as read asynchronously.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="notificationId">The notification identifier.</param>
    /// <returns>True if the notification was found and marked as read; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="notificationId"/> is <see langword="null"/>.</exception>
    public static async Task<bool> MarkTestNotificationAsReadAsync(
        this NotificationServiceTests tests,
        string notificationId)
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentNullException.ThrowIfNull(notificationId);

        return await tests._notificationService.MarkAsReadAsync(notificationId);
    }

    /// <summary>
    /// Creates multiple notifications with sequential titles and sends them asynchronously.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="count">The number of notifications to create.</param>
    /// <param name="recipientUserId">The recipient user identifier.</param>
    /// <param name="prefix">The title prefix for generated notifications.</param>
    /// <returns>An enumerable of created notifications.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="recipientUserId"/> or <paramref name="prefix"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 1.</exception>
    public static async Task<IReadOnlyList<Notification>> CreateMultipleTestNotificationsAsync(
        this NotificationServiceTests tests,
        int count,
        string recipientUserId,
        string prefix = "Notification")
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentNullException.ThrowIfNull(recipientUserId);
        ArgumentException.ThrowIfNullOrEmpty(prefix);

        var notifications = new List<Notification>(count);

        for (var i = 0; i < count; i++)
        {
            var notification = await tests.CreateAndSendTestNotificationAsync(
                $"{prefix} {i + 1}",
                $"Message for notification {i + 1}",
                recipientUserId);
            notifications.Add(notification);
        }

        return notifications.AsReadOnly();
    }

    /// <summary>
    /// Creates a notification with a specific ID for testing edge cases.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="notificationId">The specific notification ID to use.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    /// <param name="recipientUserId">The recipient user identifier.</param>
    /// <returns>The created notification.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="notificationId"/> or <paramref name="title"/> or <paramref name="message"/> or <paramref name="recipientUserId"/> is <see langword="null"/>.</exception>
    public static Notification CreateTestNotificationWithId(
        this NotificationServiceTests tests,
        string notificationId,
        string title,
        string message,
        string recipientUserId)
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentException.ThrowIfNullOrEmpty(notificationId);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(recipientUserId);

        return new Notification
        {
            Id = notificationId,
            Title = title,
            Message = message,
            RecipientUserId = recipientUserId
        };
    }
}