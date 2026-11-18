using Android.OS;
using Firebase.Messaging;
using MyTestApp.Dto.Notifications;
using TestApp.My.Mobile.Interfaces;

namespace TestApp.My.Mobile.Platforms.Android
{
    /// <summary>
    /// Android implementation of <see cref="IPushNotificationService"/>.
    /// <para>
    /// This is the class the view model talks to. It wraps <see cref="FirebaseMessaging"/> so the
    /// view model never references the FCM SDK directly. Compare this with the original code where
    /// <c>FirebaseMessaging.Instance.GetToken()</c> was called from <c>MainActivity.OnCreate</c>.
    /// </para>
    /// </summary>
    public sealed class AndroidPushNotificationService : IPushNotificationService, IPushNotificationEventSink
    {
        private string? _pendingDeepLink;

        // Initialised to a no-op so the events are never null (and no null checks are needed).
        public event EventHandler<string> TokenRefreshed = delegate { };

        public event EventHandler<PushMessageDto> NotificationReceived = delegate { };

        public event EventHandler<string> NotificationOpened = delegate { };

        public async Task<bool> RequestPermissionAsync()
        {
            // POST_NOTIFICATIONS only exists from Android 13 (API 33). Below that the permission is
            // granted implicitly at install time.
            if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
            {
                return true;
            }

            var status = await Permissions
                .RequestAsync<Permissions.PostNotifications>()
                .ConfigureAwait(true);

            return status == PermissionStatus.Granted;
        }

        public async Task<string> GetPushChannelTokenAsync()
        {
            // The FCM SDK exposes a Google Play Services task; adapt it to a .NET task.
            var completion = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);

#pragma warning disable CS0618 // GetToken is deprecated but still supported
            FirebaseMessaging.Instance
                .GetToken()
                .AddOnSuccessListener(new PNSTokenSuccessListener(token => completion.TrySetResult(token)))
                .AddOnFailureListener(new PNSTokenFailureListener(exception =>
                    completion.TrySetException(new InvalidOperationException(
                        $"FCM token request failed: {exception?.Message}"))))
                .AddOnCompleteListener(new PNSTokenCompleteListener(task =>
                {
                    if (task.IsComplete && !task.IsSuccessful)
                    {
                        completion.TrySetException(new InvalidOperationException("FCM token request did not complete."));
                    }
                }));
#pragma warning restore CS0618

            return await completion.Task.ConfigureAwait(true);
        }

        /// <inheritdoc />
        public void RaiseTokenRefreshed(string token) => TokenRefreshed?.Invoke(this, token);

        /// <inheritdoc />
        public void RaiseNotificationReceived(PushMessageDto message) => NotificationReceived?.Invoke(this, message);

        /// <inheritdoc />
        public void RaiseNotificationOpened(string url)
        {
            // Buffer first, then notify. The buffer is the single source of truth for the view model,
            // so a tap that arrives before it subscribes (MainActivity.OnCreate runs before the page
            // appears) is not lost.
            Interlocked.Exchange(ref _pendingDeepLink, url);

            NotificationOpened?.Invoke(this, url);
        }

        /// <inheritdoc />
        public string? ConsumePendingDeepLink() => Interlocked.Exchange(ref _pendingDeepLink, null);
    }
}
