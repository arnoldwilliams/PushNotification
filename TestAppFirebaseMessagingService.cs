using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Firebase.Messaging;
using MyTestApp.Dto.Notifications.Service;
using TestApp.My.Mobile.LocalServices.Interfaces;

namespace TestApp.My.Mobile.Platforms.Android
{
    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class TestAppFirebaseMessagingService : FirebaseMessagingService
    {
        public TestAppFirebaseMessagingService()
        {
        }

        internal static readonly string ChannelId = Helpers.Constants.NotificationChannel;

        internal static readonly string ChannelDescription = Helpers.Constants.NotificationChannelDescription;


        internal static readonly int NotificationID = Helpers.Constants.NotificationID;

        INotificationRegistrationService _notificationRegistrationService;

        IDeviceInstallationService _deviceInstallationService;

        public override void OnMessageReceived(RemoteMessage message)
        {
            if (message is null)
            {
                return;
            }

            if (message.Data is null || !message.Data.Any())
            {
                return;
            }

            base.OnMessageReceived(message);

            var title = message.GetNotification()?.Title ?? "New Alert";
            var body = message.GetNotification()?.Body ?? "Tap to view details.";

            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<(string, string)>((title, body)));

            this.SendNotification(message.Data);
        }

        public override void OnNewToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            _deviceInstallationService = ServiceHelper.GetService<IDeviceInstallationService>();
            if (_deviceInstallationService == null)
            {
                return;
            }

            _notificationRegistrationService = ServiceHelper.GetService<INotificationRegistrationService>();
            if (_notificationRegistrationService == null)
            {
                return;
            }

            base.OnNewToken(token);

            _deviceInstallationService.Token = token;

            _notificationRegistrationService.RefreshRegistrationAsync(token)
                .ContinueWith((task) =>
                {
                    if (task.IsFaulted)
                    {
                        throw task.Exception;
                    }
                });
        }

        private void SendNotification(IDictionary<string, string> messageKeyValuePairs)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            intent.AddFlags(ActivityFlags.SingleTop);

            Func<string, bool> checkKey = (key) => messageKeyValuePairs.ContainsKey(key);
            Func<DateTime> getUpdatedAtUtc = () => (DateTime.TryParse(messageKeyValuePairs[PushNotificationFieldTypeDto.CreatedAt], out DateTime statusUpdatedAt) ? statusUpdatedAt : DateTime.UtcNow).ToLocalTime();

            var title = checkKey(PushNotificationFieldTypeDto.Title) ? messageKeyValuePairs[PushNotificationFieldTypeDto.Title] : string.Empty;
            var text = checkKey(PushNotificationFieldTypeDto.Body) ? messageKeyValuePairs[PushNotificationFieldTypeDto.Body] : string.Empty;
            var message = checkKey(PushNotificationFieldTypeDto.JsonPayload) ? messageKeyValuePairs[PushNotificationFieldTypeDto.JsonPayload] : string.Empty;
            var category = checkKey(PushNotificationFieldTypeDto.Category) ? messageKeyValuePairs[PushNotificationFieldTypeDto.Category] : string.Empty;
            var externalDetailsUrl = checkKey(PushNotificationFieldTypeDto.ExternalDetailsUrl) ? messageKeyValuePairs[PushNotificationFieldTypeDto.ExternalDetailsUrl] : string.Empty;

            if (!string.IsNullOrEmpty(externalDetailsUrl))
            {
                //intent.PutExtra(nameof(MyTestApp.Integration.Dto.Notifications.PushMessageDto.ExternalDetailsUrl), externalDetailsUrl);
            }

            var pendingIntentFlags = (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable
            : PendingIntentFlags.UpdateCurrent;

            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, pendingIntentFlags);

            var notificationBuilder = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle(title)
                .SetSmallIcon(Resource.Drawable.maui_splash)
                .SetContentText(text)
                .SetChannelId(ChannelId)
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true)
                .SetPriority((int)AndroidX.Core.App.NotificationCompat.PriorityMax);

            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(NotificationID, notificationBuilder.Build());
        }
    }
}
