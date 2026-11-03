using PushNotification.App.Models;

namespace PushNotification.App.Services.Preferences;

/// <summary>
/// Reads and writes the user's notification topic opt-ins and keeps the push
/// provider subscriptions in sync with the stored choices.
/// </summary>
public interface INotificationPreferencesService
{
    /// <summary>Returns every topic with its current enabled/disabled state.</summary>
    Task<IReadOnlyList<NotificationPreference>> GetPreferencesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns <c>true</c> when the topic is currently enabled.</summary>
    bool IsEnabled(NotificationTopic topic);

    /// <summary>
    /// Enables or disables a topic, persists the change and updates the underlying
    /// push subscription. Returns the saved preferences.
    /// </summary>
    Task<IReadOnlyList<NotificationPreference>> SetEnabledAsync(
        NotificationTopic topic,
        bool enabled,
        CancellationToken cancellationToken = default);

    /// <summary>Enables or disables every topic at once, e.g. a global master switch.</summary>
    Task<IReadOnlyList<NotificationPreference>> SetAllAsync(
        bool enabled,
        CancellationToken cancellationToken = default);

    /// <summary>Topics the user currently receives.</summary>
    IReadOnlyCollection<NotificationTopic> GetEnabledTopics();

    /// <summary>Re-applies every stored subscription to the push provider.</summary>
    Task SynchronizeSubscriptionsAsync(CancellationToken cancellationToken = default);
}
