namespace TestApp.My.Mobile.Helpers
{
    /// <summary>
    /// Application wide constants shared by every platform.
    /// </summary>
    public static class Constants
    {
        // Android notification channel
        public const string NotificationChannel = "testapp_notifications";
        public const string NotificationChannelDescription = "General notifications for TestApp";
        public const int NotificationID = 1001;

        // Secure storage keys
        public const string PushToken = "PushToken";
        public const string PushTags = "PushTags";
        public const string DeviceId = "DeviceId";

        // Messaging channels
        public const string CloseApp = "CloseApp";
        public const string NotificationReceived = "NotificationReceived";
    }
}
