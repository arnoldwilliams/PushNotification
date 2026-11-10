namespace MyTestApp.Dto.Notifications.Service
{
    /// <summary>
    /// Keys used in the FCM data payload and the APNs payload.
    /// </summary>
    public static class PushNotificationFieldTypeDto
    {
        // FCM data keys (also used inside the APNs "data" dictionary).
        public const string Title = "title";
        public const string Body = "body";
        public const string CreatedAt = "createdAt";
        public const string Category = "category";
        public const string JsonPayload = "jsonPayload";
        public const string ExternalDetailsUrl = "externalDetailsUrl";
        public const string Version = "version";

        // APNs top level keys.
        public const string APS = "aps";
        public const string Alert = "alert";
        public const string Data = "data";
    }
}
