using PushNotification.App.Models;

namespace PushNotification.App.Services.Notifications;

/// <summary>
/// Static fan-out point between the Android managed <c>FirebaseMessagingService</c>
/// (created by the OS, not by DI) and the singleton
/// <see cref="AndroidPushNotificationService"/> registered in the container.
/// </summary>
internal static class FirebasePushBridge
{
    public static event EventHandler<string>? TokenRefreshed;

    public static event EventHandler<PushMessage>? MessageReceived;

    public static event EventHandler<PushMessage>? NotificationTapped;

    public static void RaiseTokenRefreshed(string token) => TokenRefreshed?.Invoke(null, token);

    public static void RaiseMessageReceived(PushMessage message) => MessageReceived?.Invoke(null, message);

    public static void RaiseNotificationTapped(PushMessage message) => NotificationTapped?.Invoke(null, message);
}
