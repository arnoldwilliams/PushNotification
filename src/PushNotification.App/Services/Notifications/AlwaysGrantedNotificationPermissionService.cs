using PushNotification.App.Models;

namespace PushNotification.App.Services.Notifications;

/// <summary>
/// Fallback permission service for desktop targets where notifications are always allowed.
/// </summary>
public sealed class AlwaysGrantedNotificationPermissionService : INotificationPermissionService
{
    public Task<NotificationPermissionStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(NotificationPermissionStatus.Granted);

    public Task<NotificationPermissionStatus> RequestAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(NotificationPermissionStatus.Granted);
}
