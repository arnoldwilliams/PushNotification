namespace PushNotification.App.Models;

/// <summary>
/// The notification categories a user can opt in or out of.
/// Each topic maps to an FCM topic (Android) and an APNs subscription (iOS).
/// </summary>
public sealed class NotificationPreference
{
    public NotificationPreference(NotificationTopic topic, string displayName, string description)
    {
        Topic = topic;
        DisplayName = displayName;
        Description = description;
    }

    public NotificationTopic Topic { get; }

    /// <summary>Human readable label shown in the preferences view.</summary>
    public string DisplayName { get; }

    /// <summary>Short explanation of what the user will receive.</summary>
    public string Description { get; }

    /// <summary>Whether the user has opted in. Persisted per installation.</summary>
    public bool IsEnabled { get; set; }
}
