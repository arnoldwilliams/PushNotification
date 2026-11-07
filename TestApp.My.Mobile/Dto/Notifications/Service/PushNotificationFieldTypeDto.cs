namespace MyTestApp.Dto.Notifications.Service
{
    /// <summary>
    /// Keys used in the FCM data payload.
    /// </summary>
    public static class PushNotificationFieldTypeDto
    {
        public const string Title = "title";
        public const string Body = "body";
        public const string CreatedAt = "createdAt";
        public const string Category = "category";
        public const string JsonPayload = "jsonPayload";
        public const string ExternalDetailsUrl = "externalDetailsUrl";
    }
}
