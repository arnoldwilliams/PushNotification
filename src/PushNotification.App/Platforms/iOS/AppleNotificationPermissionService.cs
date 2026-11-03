using PushNotification.App.Models;
using UserNotifications;

namespace PushNotification.App.Services.Notifications;

/// <summary>
/// iOS notification permission handling via <see cref="UNUserNotificationCenter"/>.
/// </summary>
public sealed class AppleNotificationPermissionService : INotificationPermissionService
{
    public async Task<NotificationPermissionStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
        return Map(settings.AuthorizationStatus);
    }

    public async Task<NotificationPermissionStatus> RequestAsync(CancellationToken cancellationToken = default)
    {
        var options = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;

        var (approved, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(options);
        if (!approved)
        {
            return NotificationPermissionStatus.Denied;
        }

        return await GetStatusAsync(cancellationToken).ConfigureAwait(false);
    }

    private static NotificationPermissionStatus Map(UNAuthorizationStatus status) =>
        status switch
        {
            UNAuthorizationStatus.Authorized => NotificationPermissionStatus.Granted,
            UNAuthorizationStatus.Provisional => NotificationPermissionStatus.Granted,
            UNAuthorizationStatus.Ephemeral => NotificationPermissionStatus.Granted,
            UNAuthorizationStatus.Denied => NotificationPermissionStatus.Denied,
            UNAuthorizationStatus.NotDetermined => NotificationPermissionStatus.NotRequested,
            _ => NotificationPermissionStatus.Unknown,
        };
}
