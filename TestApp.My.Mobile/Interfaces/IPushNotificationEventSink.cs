using MyTestApp.Dto.Notifications;

namespace TestApp.My.Mobile.Interfaces
{
    /// <summary>
    /// Implemented by the platform push notification service so platform entry points
    /// (Android activity / FCM service, iOS app delegate) can forward events into the
    /// service which the view model is subscribed to.
    /// </summary>
    public interface IPushNotificationEventSink
    {
        void RaiseTokenRefreshed(string token);

        void RaiseNotificationReceived(PushMessageDto message);

        void RaiseNotificationOpened(string url);
    }
}
