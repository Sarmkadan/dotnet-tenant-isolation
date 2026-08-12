#nullable enable

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

        _logger.LogInformation("Sending notification to user {RecipientUserId}", notification.RecipientUserId);

        if (!_notifications.TryAdd(notification.Id, notification))
        {
            _logger.LogError("Failed to add notification {NotificationId} to store", notification.Id);
            throw new InvalidOperationException("Failed to send notification");
        }

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

        _logger.LogInformation("Notification {NotificationId} sent successfully to user {RecipientUserId}", notification.Id, notification.RecipientUserId);

        return await Task.FromResult(notification);
    }

    public async Task SendTenantNotificationAsync(
        Guid tenantId,
        string title,
        string message,
        NotificationType type = NotificationType.Info)
    {
        _logger.LogInformation("Sending tenant notification to {TenantId}", tenantId);

        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = type,
            TenantId = tenantId
        };

        await SendNotificationAsync(notification);

        _logger.LogInformation("Tenant notification sent to {TenantId}", tenantId);
    }

    public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(string userId)
    {
        _logger.LogInformation("Getting unread notifications for user {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID is null or empty when fetching unread notifications");
            return new List<Notification>();
        }

        var unread = _notifications.Values
            .Where(n => n.RecipientUserId == userId && n.ReadAt == null)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        _logger.LogInformation("Found {Count} unread notifications for user {UserId}", unread.Count, userId);

        return await Task.FromResult(unread);
    }

    public async Task<bool> MarkAsReadAsync(string notificationId)
    {
        _logger.LogInformation("Marking notification {NotificationId} as read", notificationId);

        if (!_notifications.TryGetValue(notificationId, out var notification))
        {
            _logger.LogWarning("Notification {NotificationId} not found when attempting to mark as read", notificationId);
            return false;
        }

        notification.ReadAt = DateTime.UtcNow;
        _logger.LogInformation("Notification {NotificationId} marked as read", notificationId);

        return await Task.FromResult(true);
    }

    public async Task<bool> DeleteNotificationAsync(string notificationId)
    {
        _logger.LogInformation("Deleting notification {NotificationId}", notificationId);

        if (!_notifications.TryRemove(notificationId, out var notification))
        {
            _logger.LogWarning("Notification {NotificationId} not found when attempting to delete", notificationId);
            return false;
        }

        // Remove from user's notification list
        if (!string.IsNullOrEmpty(notification.RecipientUserId) &&
            _userNotifications.TryGetValue(notification.RecipientUserId, out var userNotifs))
        {
            userNotifs.Remove(notificationId);
        }

        _logger.LogInformation("Notification {NotificationId} deleted successfully", notificationId);
        return await Task.FromResult(true);
    }

    public async Task<IEnumerable<Notification>> GetNotificationHistoryAsync(string userId, int limit = 50)
    {
        _logger.LogInformation("Getting notification history for user {UserId} with limit {Limit}", userId, limit);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID is null or empty when fetching notification history");
            return new List<Notification>();
        }

        var history = _notifications.Values
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToList();

        _logger.LogInformation("Found {Count} notifications in history for user {UserId}", history.Count, userId);

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

public static class NotificationTemplates
{
    public const string NotificationSent = "Notification sent to user {0}: {1}";
    public const string NotificationDeleted = "Deleted notification {0}";
}
