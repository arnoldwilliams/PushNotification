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

        /// <summary>Raised when the platform pushes a new token (FCM OnNewToken / APNs Register).</summary>
        event EventHandler<string> TokenRefreshed;

        /// <summary>Raised when a notification is received while the app is running.</summary>
        event EventHandler<PushMessageDto> NotificationReceived;

        /// <summary>Raised when the user taps a notification that carried a deep link.</summary>
        event EventHandler<string> NotificationOpened;
    }
}
