using PushNotification.App.Models;

namespace PushNotification.App.Services.Notifications;

/// <summary>
/// Requests and reports the OS level notification permission for the app.
/// </summary>
public interface INotificationPermissionService
{
    /// <summary>Reads the current permission without prompting the user.</summary>
    Task<NotificationPermissionStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prompts the user for notification permission when the platform allows it.
    /// Returns the resulting status.
    /// </summary>
    Task<NotificationPermissionStatus> RequestAsync(CancellationToken cancellationToken = default);
}
