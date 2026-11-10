using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;  //Needed for Android.Views.View and ViewGroup
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Firebase.Messaging;
using Google.Android.Material.BottomNavigation;
using Microsoft.Identity.Client;
using TestApp.My.Mobile.Common;
using TestApp.My.Mobile.Interfaces;
using TestApp.My.Mobile.LocalServices.Interfaces;
using TestApp.My.Mobile.Platforms.Android;
using TestApp.My.Mobile.Services;
using Constants = TestApp.My.Mobile.Helpers.Constants;

namespace TestApp.My.Mobile
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | 
        ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | 
        ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private ICommonPlatformService _commonPlatformService;

        internal static readonly string ChannelId = Helpers.Constants.NotificationChannel;

        internal static readonly string ChannelDescription = Helpers.Constants.NotificationChannelDescription;

        IDeviceInstallationService DeviceInstallationService =>
    _deviceInstallationService ?? (_deviceInstallationService = IPlatformApplication.Current.Services.GetService<IDeviceInstallationService>());

        INotificationRegistrationService _notificationRegistrationService;
        IDeviceInstallationService _deviceInstallationService;

        INotificationRegistrationService NotificationRegistrationService =>
        _notificationRegistrationService ?? (_notificationRegistrationService = IPlatformApplication.Current.Services.GetService<INotificationRegistrationService>());

        public void OnSuccess(Java.Lang.Object result)
        {
            DeviceInstallationService.Token = result.ToString();
        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.CreateNotificationChannel();

            MessageBus.Instance.Subscribe(Constants.CloseApp, CloseApp);

            _commonPlatformService = ServiceHelper.GetService<ICommonPlatformService>();

            if ( _commonPlatformService != null)
            {
                _commonPlatformService.UIWindow = this;
                CommonPlatformService.InitialPageLoad = true;
            }

            string platform = Microsoft.Maui.Devices.DeviceInfo.Current.Platform.ToString();
            string deviceModel = Microsoft.Maui.Devices.DeviceInfo.Current.Model;
            string appVersion = Microsoft.Maui.ApplicationModel.AppInfo.Current.VersionString;


            if (DeviceInstallationService.NotificationsSupported)
            {
                FirebaseMessaging.Instance.GetToken()
                .AddOnSuccessListener(new PNSTokenSuccessListener(token =>
                {
                    DeviceInstallationService.Token = token;

                    NotificationRegistrationService.RefreshRegistrationAsync(token)
                   .ContinueWith((task) =>
                   {
                       if (task.IsFaulted)
                       {
                           throw task.Exception;
                       }
                   });
                }));
            }

            HandleIntent(Intent);
        }

        private BottomNavigationView? FindBottomNavigationView(Android.Views.View view)
        {
            if (view is BottomNavigationView bnv)
                return bnv;

            if (view is ViewGroup group)
            {
                for (int i = 0; i < group.ChildCount; i++)
                {
                    var result = FindBottomNavigationView(group.GetChildAt(i));
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            var importance = NotificationImportance.High;

            var channel = new NotificationChannel(ChannelId, ChannelDescription, importance);

            channel.EnableVibration(true);
            channel.LockscreenVisibility = NotificationVisibility.Public;
            channel.EnableLights(true);

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        private async Task CloseApp(object obj)
        {
            await Task.Delay(1000);
           App.Current.Quit();
        }

        private static void HandleIntent(Intent intent)
        {
            if (intent == null)
            {
                return;
            }

            var url = intent.Extras?.GetString("url");
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                Microsoft.Maui.Controls.Application.Current?.SendOnAppLinkRequestReceived(uri);
            }
        }
    }
}