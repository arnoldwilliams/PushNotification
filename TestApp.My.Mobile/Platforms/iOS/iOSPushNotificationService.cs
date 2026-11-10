using Foundation;
using MyTestApp.Dto.Notifications;
using TestApp.My.Mobile.Interfaces;
using UIKit;
using UserNotifications;

namespace TestApp.My.Mobile.Platforms.iOS
{
    /// <summary>
    /// iOS implementation of <see cref="IPushNotificationService"/>.
    /// <para>
    /// Wraps APNs registration and the user notification centre so the view model stays platform
    /// agnostic. The app delegate forwards the APNs callbacks into this class through
    /// <see cref="IPushNotificationEventSink"/>.
    /// </para>
    /// </summary>
    public sealed class iOSPushNotificationService : IPushNotificationService, IPushNotificationEventSink
    {
        private TaskCompletionSource<string>? _tokenCompletion;

        // Initialised to a no-op so the events are never null (and no null checks are needed).
        public event EventHandler<string> TokenRefreshed = delegate { };

        public event EventHandler<PushMessageDto> NotificationReceived = delegate { };

        public event EventHandler<string> NotificationOpened = delegate { };

        public Task<bool> RequestPermissionAsync()
        {
            var options = UNAuthorizationOptions.Alert
                          | UNAuthorizationOptions.Badge
                          | UNAuthorizationOptions.Sound;

            var completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            UNUserNotificationCenter.Current.RequestAuthorization(options, (granted, error) =>
            {
                if (error != null)
                {
                    completion.TrySetException(
                        new InvalidOperationException($"Notification authorization failed: {error.LocalizedDescription}"));
                    return;
                }

                completion.TrySetResult(granted);
            });

            return completion.Task;
        }

        public Task<string> GetPushChannelTokenAsync()
        {
            // APNs only ever pushes the device token to the app delegate. Register and wait for
            // the callback (which may already have arrived).
            var completion = _tokenCompletion;

            if (completion is null || completion.Task.IsCompleted)
            {
                completion = new TaskCompletionSource<string>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                _tokenCompletion = completion;
            }

            UIApplication.SharedApplication.RegisterForRemoteNotifications();

            return completion.Task;
        }

        /// <summary>Called by the app delegate with the APNs device token bytes.</summary>
        public void RegisterDeviceToken(NSData deviceToken)
        {
            if (deviceToken is null)
            {
                return;
            }

            var token = deviceToken.ToHexString();

            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            _tokenCompletion?.TrySetResult(token);

            RaiseTokenRefreshed(token);
        }

        /// <summary>Called by the app delegate when APNs registration fails.</summary>
        public void RegisterFailed(NSError error)
        {
            _tokenCompletion?.TrySetException(
                new InvalidOperationException($"APNs registration failed: {error?.LocalizedDescription}"));
        }

        public void RaiseTokenRefreshed(string token) => TokenRefreshed?.Invoke(this, token);

        public void RaiseNotificationReceived(PushMessageDto message) => NotificationReceived?.Invoke(this, message);

        public void RaiseNotificationOpened(string url) => NotificationOpened?.Invoke(this, url);
    }
}
