using PushNotification.App.Models;

namespace PushNotification.App.Services.Notifications;

/// <summary>
/// Platform independent push notification operations.
/// Implemented per platform with Firebase Cloud Messaging (Android) and Apple Push
/// Notification service (iOS).
/// </summary>
public interface IPushNotificationService
{
    /// <summary>Raised when a new registration/device token is issued by the push service.</summary>
    event EventHandler<string>? TokenRefreshed;

    /// <summary>Raised when a push message arrives while the app is in the foreground.</summary>
    event EventHandler<PushMessage>? MessageReceived;

    /// <summary>Raised when the user taps a notification and the app is opened from it.</summary>
    event EventHandler<PushMessage>? NotificationTapped;

    /// <summary>
    /// Initializes the underlying push SDK. Safe to call more than once.
    /// </summary>
    Task<bool> InitializeAsync(PushConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current FCM registration token (Android) or APNs device token (iOS).
    /// Returns <c>null</c> when no token is available yet.
    /// </summary>
    Task<string?> GetDeviceTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>Subscribes the installation to a topic, e.g. "promotions".</summary>
    Task SubscribeToTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default);

    /// <summary>Unsubscribes the installation from a topic.</summary>
    Task UnsubscribeFromTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default);

    /// <summary>Deletes the push token on the device, used at sign-out.</summary>
    Task DeleteTokenAsync(CancellationToken cancellationToken = default);
}
