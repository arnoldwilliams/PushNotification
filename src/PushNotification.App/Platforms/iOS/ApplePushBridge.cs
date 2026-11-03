using PushNotification.App.Models;

namespace PushNotification.App.Services.Notifications;

/// <summary>
/// Fan-out point between the iOS <c>AppDelegate</c>/<c>UNUserNotificationCenter</c> callbacks
/// (invoked by the OS) and the singleton <see cref="ApplePushNotificationService"/>.
/// </summary>
internal static class ApplePushBridge
{
    private static readonly object Sync = new();
    private static string? _deviceToken;

    public static event EventHandler<string>? TokenRefreshed;

    public static event EventHandler<PushMessage>? MessageReceived;

    public static event EventHandler<PushMessage>? NotificationTapped;

    /// <summary>APNs device token (hex string) captured from the AppDelegate.</summary>
    public static string? DeviceToken
    {
        get
        {
            lock (Sync)
            {
                return _deviceToken;
            }
        }
    }

    public static void SetDeviceToken(string token)
    {
        lock (Sync)
        {
            _deviceToken = token;
        }

        TokenRefreshed?.Invoke(null, token);
    }

    public static void RaiseMessageReceived(PushMessage message) => MessageReceived?.Invoke(null, message);

    public static void RaiseNotificationTapped(PushMessage message) => NotificationTapped?.Invoke(null, message);
}
