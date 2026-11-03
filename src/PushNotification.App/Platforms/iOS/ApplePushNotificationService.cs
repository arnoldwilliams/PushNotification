using Foundation;
using Microsoft.Extensions.Logging;
using PushNotification.App.Models;
using UIKit;
using UserNotifications;

namespace PushNotification.App.Services.Notifications;

/// <summary>
/// iOS push implementation backed by Apple Push Notification service (APNs).
/// Device tokens are captured by the <see cref="AppDelegate"/> and surfaced through
/// <see cref="ApplePushBridge"/>; topics are modeled as APNs categories/subscriptions.
/// </summary>
public sealed class ApplePushNotificationService : IPushNotificationService
{
    private readonly ILogger<ApplePushNotificationService> _logger;

    private bool _initialized;

    public ApplePushNotificationService(ILogger<ApplePushNotificationService> logger)
    {
        _logger = logger;
    }

    public event EventHandler<string>? TokenRefreshed;

    public event EventHandler<PushMessage>? MessageReceived;

    public event EventHandler<PushMessage>? NotificationTapped;

    public Task<bool> InitializeAsync(PushConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!_initialized)
        {
            ApplePushBridge.TokenRefreshed += OnTokenRefreshed;
            ApplePushBridge.MessageReceived += OnMessageReceived;
            ApplePushBridge.NotificationTapped += OnNotificationTapped;
            _initialized = true;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            UIApplication.SharedApplication.RegisterForRemoteNotifications();
        });

        _logger.LogInformation("APNs initialized. Environment={Environment}, Topic={Topic}",
            configuration.ApnsEnvironment, configuration.ApnsTopic);

        return Task.FromResult(true);
    }

    public Task<string?> GetDeviceTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = ApplePushBridge.DeviceToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult<string?>(token);
        }

        // Ask again in case the registration callback has not fired yet.
        MainThread.BeginInvokeOnMainThread(() => UIApplication.SharedApplication.RegisterForRemoteNotifications());
        return Task.FromResult<string?>(ApplePushBridge.DeviceToken);
    }

    public Task SubscribeToTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default)
    {
        // APNs has no server-side topic subscription API. Topic opt-in is enforced by the
        // backend when it builds the APNs payload (via the "topic" custom key) and locally
        // by the preferences service. This is where you would call your own backend.
        _logger.LogInformation("APNs topic enabled: {Topic}.", topic);
        return Task.CompletedTask;
    }

    public Task UnsubscribeFromTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("APNs topic disabled: {Topic}.", topic);
        return Task.CompletedTask;
    }

    public Task DeleteTokenAsync(CancellationToken cancellationToken = default)
    {
        MainThread.BeginInvokeOnMainThread(() => UIApplication.SharedApplication.UnregisterForRemoteNotifications());
        return Task.CompletedTask;
    }

    private void OnTokenRefreshed(object? sender, string token) => TokenRefreshed?.Invoke(this, token);

    private void OnMessageReceived(object? sender, PushMessage message) => MessageReceived?.Invoke(this, message);

    private void OnNotificationTapped(object? sender, PushMessage message) => NotificationTapped?.Invoke(this, message);
}
