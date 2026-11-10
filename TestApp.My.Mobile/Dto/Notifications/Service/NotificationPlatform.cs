namespace MyTestApp.Dto.Notifications.Service
{
    /// <summary>
    /// Platform identifier sent to the backend. Mirrors the names used by Azure Notification Hubs.
    /// </summary>
    public enum NotificationPlatform
    {
        FcmV1,
        ApnsV1
    }
}
