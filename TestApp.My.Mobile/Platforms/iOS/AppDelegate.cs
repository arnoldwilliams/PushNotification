using Foundation;
using MyTestApp.Dto.Notifications;
using TestApp.My.Mobile.Helpers;
using TestApp.My.Mobile.Interfaces;
using UIKit;
using UserNotifications;

namespace TestApp.My.Mobile
{
    /// <summary>
    /// iOS app delegate.
    /// <para>
    /// It forwards the APNs callbacks (token registration, remote notification) to
    /// <see cref="IPushNotificationEventSink"/>, which the view model is subscribed to. No
    /// registration work happens here, which mirrors the Android change (nothing in OnCreate).
    /// </para>
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            UNUserNotificationCenter.Current.Delegate = new NotificationCenterDelegate();

            return base.FinishedLaunching(application, launchOptions);
        }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            ServiceHelper.GetService<Platforms.iOS.iOSPushNotificationService>()
                ?.RegisterDeviceToken(deviceToken);
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            ServiceHelper.GetService<Platforms.iOS.iOSPushNotificationService>()
                ?.RegisterFailed(error);
        }

        public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            var message = PushMessageFactory.Build(userInfo);

            ServiceHelper.GetService<IPushNotificationEventSink>()?
                .RaiseNotificationReceived(message);
        }
    }

    /// <summary>
    /// Shows notifications while the app is in the foreground and forwards taps (deep links).
    /// </summary>
    public sealed class NotificationCenterDelegate : UNUserNotificationCenterDelegate
    {
        public override void WillPresentNotification(
            UNUserNotificationCenter center,
            UNNotification notification,
            Action<UNNotificationPresentationOptions> completionHandler)
        {
            var message = PushMessageFactory.Build(notification.Request.Content.UserInfo);

            ServiceHelper.GetService<IPushNotificationEventSink>()?
                .RaiseNotificationReceived(message);

            completionHandler(UNNotificationPresentationOptions.Alert
                              | UNNotificationPresentationOptions.Badge
                              | UNNotificationPresentationOptions.Sound);
        }

        public override void DidReceiveNotificationResponse(
            UNUserNotificationCenter center,
            UNNotificationResponse response,
            Action completionHandler)
        {
            var userInfo = response.Notification.Request.Content.UserInfo;

            var url = userInfo?[new NSString("externalDetailsUrl")]?.ToString();

            if (!string.IsNullOrWhiteSpace(url))
            {
                ServiceHelper.GetService<IPushNotificationEventSink>()?
                    .RaiseNotificationOpened(url);
            }

            completionHandler();
        }
    }

    internal static class PushMessageFactory
    {
        public static PushMessageDto Build(NSDictionary userInfo)
        {
            string Get(string key) => userInfo?[new NSString(key)]?.ToString() ?? string.Empty;

            return new PushMessageDto
            {
                Title = Get("title"),
                Body = Get("body"),
                Category = Get("category"),
                JsonPayload = Get("jsonPayload"),
                ExternalDetailsUrl = Get("externalDetailsUrl"),
                CreatedAtUtc = DateTime.UtcNow
            };
        }
    }
}
