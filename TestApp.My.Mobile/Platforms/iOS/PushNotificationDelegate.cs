using UserNotifications;
using TestApp.My.Mobile.Interfaces;
using TestApp.My.Mobile.Helpers;
using TestApp.My.Mobile.Services;
using MyTestApp.Dto.Notifications;

namespace TestApp.My.Mobile.Platforms.iOS
{
    /// <summary>
    /// <see cref="UNUserNotificationCenterDelegate"/> for the iOS app.
    /// <para>
    /// It only forwards the notification events to <see cref="IPushNotificationEventSink"/>; the
    /// <c>PushNotificationViewModel</c> decides what to do with them. This mirrors the Android
    /// <c>TestAppFirebaseMessagingService</c>, which also only raises events.
    /// </para>
    /// </summary>
    public class PushNotificationDelegate : UNUserNotificationCenterDelegate
    {
        /// <summary>
        /// Shows the alert, sound and badge even while the app is in the foreground.
        /// </summary>
        public override void WillPresentNotification(
            UNUserNotificationCenter center,
            UNNotification notification,
            Action<UNNotificationPresentationOptions> completionHandler)
        {
            var message = PushMessageFactory.Build(notification.Request.Content.UserInfo);

            ServiceHelper.GetService<IPushNotificationEventSink>()?
                .RaiseNotificationReceived(message);

            completionHandler(UNNotificationPresentationOptions.Banner
                              | UNNotificationPresentationOptions.List
                              | UNNotificationPresentationOptions.Badge
                              | UNNotificationPresentationOptions.Sound);
        }

        /// <summary>
        /// Handles the user tapping a notification (including deep links).
        /// </summary>
        public override void DidReceiveNotificationResponse(
            UNUserNotificationCenter center,
            UNNotificationResponse response,
            Action completionHandler)
        {
            try
            {
                var userInfo = response.Notification.Request.Content.UserInfo;

                if (userInfo != null)
                {
                    userInfo.ShowRemoteNotification();

                    var message = PushMessageFactory.Build(userInfo);

                    if (!string.IsNullOrWhiteSpace(message.ExternalDetailsUrl))
                    {
                        // Hand the deep link to the view model through the platform service.
                        ServiceHelper.GetService<IPushNotificationEventSink>()?
                            .RaiseNotificationOpened(message.ExternalDetailsUrl);
                    }
                }

                completionHandler();
            }
            catch (Exception ex)
            {
                LoggingService<PushNotificationDelegate>.LogError(ex);

                completionHandler();
            }
        }
    }
}
