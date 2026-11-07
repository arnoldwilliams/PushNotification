using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Firebase.Messaging;
using MyTestApp.Dto.Notifications;
using MyTestApp.Dto.Notifications.Service;
using TestApp.My.Mobile.Helpers;
using TestApp.My.Mobile.Interfaces;
using Constants = TestApp.My.Mobile.Helpers.Constants;

namespace TestApp.My.Mobile.Platforms.Android
{
    /// <summary>
    /// Receives FCM messages and new tokens.
    /// <para>
    /// The original sample did the device registration here as well. In this version the service
    /// only raises events; the <c>PushNotificationViewModel</c> owns registration.
    /// </para>
    /// </summary>
    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class TestAppFirebaseMessagingService : FirebaseMessagingService
    {
        internal static readonly string ChannelId = Constants.NotificationChannel;

        internal static readonly string ChannelDescription = Constants.NotificationChannelDescription;

        internal static readonly int NotificationID = Constants.NotificationID;

        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            if (message is null)
            {
                return;
            }

            var pushMessage = BuildPushMessage(message);

            ServiceHelper.GetService<IPushNotificationEventSink>()?
                .RaiseNotificationReceived(pushMessage);

            SendNotification(pushMessage);
        }

#pragma warning disable CS0672, CS0618 // OnNewToken is deprecated but is still the required FCM override
        public override void OnNewToken(string token)
        {
            base.OnNewToken(token);

            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            // Let the view model (via the platform service) take care of re-registering the device.
            ServiceHelper.GetService<IPushNotificationEventSink>()?
                .RaiseTokenRefreshed(token);
        }
#pragma warning restore CS0672, CS0618

        private static PushMessageDto BuildPushMessage(RemoteMessage message)
        {
            var data = message.Data ?? new Dictionary<string, string>();

            string Get(string key) => data.TryGetValue(key, out var value) ? value : string.Empty;

            return new PushMessageDto
            {
                Title = message.GetNotification()?.Title ?? Get(PushNotificationFieldTypeDto.Title),
                Body = message.GetNotification()?.Body ?? Get(PushNotificationFieldTypeDto.Body),
                Category = Get(PushNotificationFieldTypeDto.Category),
                JsonPayload = Get(PushNotificationFieldTypeDto.JsonPayload),
                ExternalDetailsUrl = Get(PushNotificationFieldTypeDto.ExternalDetailsUrl),
                CreatedAtUtc = DateTime.TryParse(Get(PushNotificationFieldTypeDto.CreatedAt), out var createdAt)
                    ? createdAt.ToUniversalTime()
                    : DateTime.UtcNow
            };
        }

        private void SendNotification(PushMessageDto pushMessage)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            intent.AddFlags(ActivityFlags.SingleTop);

            if (!string.IsNullOrEmpty(pushMessage.ExternalDetailsUrl))
            {
                intent.PutExtra(nameof(PushMessageDto.ExternalDetailsUrl), pushMessage.ExternalDetailsUrl);
            }

            var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable
                : PendingIntentFlags.UpdateCurrent;

            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, pendingIntentFlags)!;

            // The AndroidX fluent builder is annotated as nullable in the binding, so the chain
            // and the final Build() trip CS8602. The values are never null at runtime here.
#pragma warning disable CS8602
            var notificationBuilder = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle(pushMessage.Title ?? string.Empty)
                .SetSmallIcon(Resource.Drawable.maui_splash)
                .SetContentText(pushMessage.Body ?? string.Empty)
                .SetChannelId(ChannelId)
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true)
                .SetPriority((int)NotificationCompat.PriorityMax);

            NotificationManagerCompat.From(this)?.Notify(NotificationID, notificationBuilder.Build());
#pragma warning restore CS8602
        }
    }
}
