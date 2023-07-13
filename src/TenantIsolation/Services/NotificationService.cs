// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TenantIsolation.Services;

/// <summary>
/// Notification types
/// </summary>
public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success
}

/// <summary>
/// In-app notification
/// </summary>
public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public string? RecipientUserId { get; set; }
    public Guid? TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Notification service interface
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send notification to user
    /// </summary>
    Task<Notification> SendNotificationAsync(Notification notification);

    /// <summary>
    /// Send notification to tenant users
    /// </summary>
    Task SendTenantNotificationAsync(Guid tenantId, string title, string message, NotificationType type = NotificationType.Info);

    /// <summary>
    /// Get unread notifications for user
    /// </summary>
    Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(string userId);

    /// <summary>
    /// Mark notification as read
    /// </summary>
    Task<bool> MarkAsReadAsync(string notificationId);

    /// <summary>
    /// Delete notification
    /// </summary>
    Task<bool> DeleteNotificationAsync(string notificationId);

    /// <summary>
    /// Get notification history
    /// </summary>
    Task<IEnumerable<Notification>> GetNotificationHistoryAsync(string userId, int limit = 50);
}

/// <summary>
/// Notification service implementation
/// Stores notifications in memory (should use persistent storage in production)
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ConcurrentDictionary<string, Notification> _notifications;
    private readonly ConcurrentDictionary<string, List<string>> _userNotifications; // userId -> list of notification IDs
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _notifications = new ConcurrentDictionary<string, Notification>();
        _userNotifications = new ConcurrentDictionary<string, List<string>>();
        _logger = logger;
    }

    public async Task<Notification> SendNotificationAsync(Notification notification)
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        if (!_notifications.TryAdd(notification.Id, notification))
            throw new InvalidOperationException("Failed to send notification");

        // Track user notification
        if (!string.IsNullOrEmpty(notification.RecipientUserId))
        {
            _userNotifications.AddOrUpdate(
                notification.RecipientUserId,
                new List<string> { notification.Id },
                (_, list) =>
                {
                    list.Add(notification.Id);
                    return list;
                });
        }

        _logger.LogInformation(
            "Notification sent to user {UserId}: {Title}",
            notification.RecipientUserId, notification.Title);

        return await Task.FromResult(notification);
    }

    public async Task SendTenantNotificationAsync(
        Guid tenantId,
        string title,
        string message,
        NotificationType type = NotificationType.Info)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = type,
            TenantId = tenantId
        };

        await SendNotificationAsync(notification);
    }

    public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return new List<Notification>();

        var unread = _notifications.Values
            .Where(n => n.RecipientUserId == userId && n.ReadAt == null)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        return await Task.FromResult(unread);
    }

    public async Task<bool> MarkAsReadAsync(string notificationId)
    {
        if (!_notifications.TryGetValue(notificationId, out var notification))
            return false;

        notification.ReadAt = DateTime.UtcNow;
        return await Task.FromResult(true);
    }

    public async Task<bool> DeleteNotificationAsync(string notificationId)
    {
        if (!_notifications.TryRemove(notificationId, out var notification))
            return false;

        // Remove from user's notification list
        if (!string.IsNullOrEmpty(notification.RecipientUserId) &&
            _userNotifications.TryGetValue(notification.RecipientUserId, out var userNotifs))
        {
            userNotifs.Remove(notificationId);
        }

        _logger.LogInformation("Deleted notification {NotificationId}", notificationId);
        return await Task.FromResult(true);
    }

    public async Task<IEnumerable<Notification>> GetNotificationHistoryAsync(string userId, int limit = 50)
    {
        if (string.IsNullOrEmpty(userId))
            return new List<Notification>();

        var history = _notifications.Values
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToList();

        return await Task.FromResult(history);
    }
}

/// <summary>
/// Extension method to register notification service
/// </summary>
public static class NotificationServiceExtensions
{
    public static IServiceCollection AddNotificationService(this IServiceCollection services)
    {
        services.AddSingleton<INotificationService, NotificationService>();
        return services;
    }
}
