using Foundation;
using MyTestApp.Dto.Notifications.Service;
using System.Text;
using UIKit;

namespace TestApp.My.Mobile.Platforms.iOS
{
    /// <summary>
    /// Helpers for the APNs device token (<see cref="NSData"/>) and the APNs payload
    /// (<see cref="NSDictionary"/>).
    /// </summary>
    internal static class NSDataExtensions
    {
        /// <summary>
        /// Converts the raw APNs device token into the hex string the backend expects.
        /// </summary>
        public static string ToHexString(this NSData data)
        {
            var bytes = data?.ToArray();

            if (bytes == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
            {
                sb.AppendFormat("{0:x2}", b);
            }

            return sb.ToString().ToUpperInvariant();
        }

        /// <summary>
        /// Reads the alert/data fields from an APNs payload. When the app is in the foreground the
        /// payload is surfaced as a local notification; otherwise the tap is handled by
        /// <see cref="PushNotificationDelegate.DidReceiveNotificationResponse"/>.
        /// </summary>
        public static void ShowRemoteNotification(this NSDictionary userInfo)
        {
            if (userInfo == null)
            {
                return;
            }

            string title = string.Empty;

            string text = string.Empty;

            if (userInfo[PushNotificationFieldTypeDto.APS] is NSDictionary aps
                && aps[PushNotificationFieldTypeDto.Alert] is NSDictionary alert)
            {
                bool HasAlert(string key) => alert.ContainsKey(new NSString(key));

                title = HasAlert(PushNotificationFieldTypeDto.Title)
                    ? alert[new NSString(PushNotificationFieldTypeDto.Title)] as NSString ?? NSString.Empty
                    : NSString.Empty;

                text = HasAlert(PushNotificationFieldTypeDto.Body)
                    ? alert[new NSString(PushNotificationFieldTypeDto.Body)] as NSString ?? NSString.Empty
                    : NSString.Empty;
            }

            if (userInfo[PushNotificationFieldTypeDto.Data] is NSDictionary notificationData)
            {
                bool HasData(string key) => notificationData.ContainsKey(new NSString(key));

                if (HasData(PushNotificationFieldTypeDto.Title))
                {
                    title = notificationData[new NSString(PushNotificationFieldTypeDto.Title)] as NSString ?? title;
                }

                if (HasData(PushNotificationFieldTypeDto.Body))
                {
                    text = notificationData[new NSString(PushNotificationFieldTypeDto.Body)] as NSString ?? text;
                }
            }

            if (UIApplication.SharedApplication.ApplicationState == UIApplicationState.Active)
            {
                // The app is already running, so post a local notification to mirror what the
                // system would have shown for a background push.
#pragma warning disable CS0612, CS0618 // UILocalNotification is deprecated in favour of UNUserNotificationCenter.
                UIApplication.SharedApplication.ScheduleLocalNotification(new UILocalNotification
                {
                    FireDate = NSDate.FromTimeIntervalSinceNow(1),
                    AlertAction = title,
                    AlertBody = text,
                    ApplicationIconBadgeNumber = 1,
                    SoundName = "default"
                });
#pragma warning restore CS0612, CS0618
            }
        }
    }
}
