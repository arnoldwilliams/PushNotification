using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using TestApp.My.Mobile.Common;
using TestApp.My.Mobile.Helpers;
using TestApp.My.Mobile.Interfaces;
using Constants = TestApp.My.Mobile.Helpers.Constants;

namespace TestApp.My.Mobile
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                               ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        internal static readonly string ChannelId = Constants.NotificationChannel;

        internal static readonly string ChannelDescription = Constants.NotificationChannelDescription;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Only platform plumbing belongs here: channel creation, message bus wiring and
            // deep link handling. The push notification device registration now lives in
            // PushNotificationViewModel and is triggered from MainPage.OnAppearing.
            CreateNotificationChannel();

            MessageBus.Instance.Subscribe<string>(Constants.CloseApp, _ => CloseApp());

            HandleIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            // Required because the activity uses SingleTop; a tapped notification reuses this instance.
            HandleIntent(intent);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            var channel = new NotificationChannel(ChannelId, ChannelDescription, NotificationImportance.High)
            {
                LockscreenVisibility = NotificationVisibility.Public
            };

            channel.EnableVibration(true);
            channel.EnableLights(true);

            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager?.CreateNotificationChannel(channel);
        }

        private static void HandleIntent(Intent? intent)
        {
            var url = intent?.Extras?.GetString(nameof(MyTestApp.Dto.Notifications.PushMessageDto.ExternalDetailsUrl));

            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            // Hand the deep link to the view model through the platform service.
            ServiceHelper.GetService<IPushNotificationEventSink>()?.RaiseNotificationOpened(url);
        }

        private static async void CloseApp()
        {
            await Task.Delay(1000);
            Microsoft.Maui.Controls.Application.Current?.Quit();
        }
    }
}
