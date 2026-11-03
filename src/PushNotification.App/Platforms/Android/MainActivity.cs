using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using PushNotification.App.Services.Notifications;

namespace PushNotification.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleNotificationIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleNotificationIntent(intent);
    }

    public override void OnRequestPermissionsResult(
        int requestCode,
        string[] permissions,
        Permission[] grantResults)
    {
        AndroidNotificationPermissionService.HandlePermissionResult(permissions, grantResults);
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }

    private static void HandleNotificationIntent(Intent? intent)
    {
        // FCM adds "google.message_id" to the launch intent when the user taps a notification.
        if (intent?.Extras is null || !intent.Extras.ContainsKey("google.message_id"))
        {
            return;
        }

        FirebasePushBridge.RaiseNotificationTapped(FirebaseMessageMapper.MapIntent(intent));
    }
}
