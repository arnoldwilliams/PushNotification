using MyTestApp.Dto.Notifications;

namespace TestApp.My.Mobile.Interfaces
{
    /// <summary>
    /// Platform entry point for the push notification lifecycle.
    /// Android and iOS each provide an implementation.
    /// </summary>
    public interface IPushNotificationService
    {
        /// <summary>Requests the OS permission required to display notifications.</summary>
        Task<bool> RequestPermissionAsync();

        /// <summary>Asks the platform for the current push channel token (FCM registration token / APNs device token).</summary>
        Task<string> GetPushChannelTokenAsync();

        /// <summary>
        /// Returns the deep link from a notification tap that arrived before anything subscribed to
        /// <see cref="NotificationOpened"/>, and clears it. Returns null when there is none.
        /// <para>
        /// A tap can arrive before the view model subscribes: on Android <c>MainActivity.OnCreate</c>
        /// (which calls <c>HandleIntent</c>) completes before the page appears, so the event would be
        /// lost. The platform service buffers the link and the view model drains it once it subscribes.
        /// </para>
        /// </summary>
        string? ConsumePendingDeepLink();

        /// <summary>Raised when the platform pushes a new token (FCM OnNewToken / APNs Register).</summary>
        event EventHandler<string> TokenRefreshed;

        /// <summary>Raised when a notification is received while the app is running.</summary>
        event EventHandler<PushMessageDto> NotificationReceived;

        /// <summary>Raised when the user taps a notification that carried a deep link.</summary>
        event EventHandler<string> NotificationOpened;
    }
}
