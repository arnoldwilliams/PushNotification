using PushNotification.App.Models;

namespace PushNotification.App.Services.Notifications;

/// <summary>
/// Fallback push implementation for desktop targets (Windows/Mac Catalyst) where this
/// sample does not wire up a push provider. Keeps the shared UI runnable everywhere.
/// </summary>
public sealed class UnsupportedPushNotificationService : IPushNotificationService
{
    // The interface requires these events; this target never raises them.
#pragma warning disable CS0067
    public event EventHandler<string>? TokenRefreshed;

    public event EventHandler<PushMessage>? MessageReceived;

    public event EventHandler<PushMessage>? NotificationTapped;
#pragma warning restore CS0067

    public Task<bool> InitializeAsync(PushConfiguration configuration, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<string?> GetDeviceTokenAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);

    public Task SubscribeToTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task UnsubscribeFromTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DeleteTokenAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
