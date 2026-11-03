using Android.App;
using Android.Content;
using Firebase.Messaging;
using PushNotification.App.Models;
using PushNotification.App.Services.Notifications;

namespace PushNotification.App;

/// <summary>
/// Receives FCM messages. The Android framework instantiates this service, so it cannot
/// use constructor injection; it forwards everything through <see cref="FirebasePushBridge"/>.
/// </summary>
[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class AppFirebaseMessagingService : FirebaseMessagingService
{
    // FirebaseMessagingService.OnNewToken is marked obsolete in the current binding, but it
    // remains the supported FCM callback for token rotation.
#pragma warning disable CS0618, CS0672
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        FirebasePushBridge.RaiseTokenRefreshed(token);
    }
#pragma warning restore CS0618, CS0672

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        FirebasePushBridge.RaiseMessageReceived(FirebaseMessageMapper.Map(message, IsAppInForeground()));
    }

    private static bool IsAppInForeground()
    {
        var process = Android.App.Application.Context
            .GetSystemService(Context.ActivityService) is ActivityManager manager
            ? manager.RunningAppProcesses?.FirstOrDefault(p => p.ProcessName == Android.App.Application.Context.PackageName)
            : null;

        return process?.Importance == Importance.Foreground;
    }
}
