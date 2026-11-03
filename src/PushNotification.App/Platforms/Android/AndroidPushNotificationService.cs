using Android.Gms.Extensions;
using Firebase.Messaging;
using Microsoft.Extensions.Logging;
using PushNotification.App.Models;

namespace PushNotification.App.Services.Notifications;

/// <summary>
/// Android push implementation backed by Firebase Cloud Messaging (FCM).
/// The token is obtained through <see cref="FirebaseMessaging"/> and messages are
/// delivered by <see cref="AppFirebaseMessagingService"/> into <see cref="FirebasePushBridge"/>.
/// </summary>
public sealed class AndroidPushNotificationService : IPushNotificationService
{
    private readonly ILogger<AndroidPushNotificationService> _logger;

    private bool _initialized;

    public AndroidPushNotificationService(ILogger<AndroidPushNotificationService> logger)
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
            FirebasePushBridge.TokenRefreshed += OnTokenRefreshed;
            FirebasePushBridge.MessageReceived += OnMessageReceived;
            FirebasePushBridge.NotificationTapped += OnNotificationTapped;
            _initialized = true;
        }

        CreateNotificationChannel(configuration);

        _logger.LogInformation("FCM initialized. SenderId={SenderId}, Channel={Channel}",
            configuration.FcmSenderId, configuration.FcmNotificationChannelId);

        return Task.FromResult(true);
    }

    public async Task<string?> GetDeviceTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // The Xamarin binding marks the token APIs obsolete, but they remain the
            // supported way to read and rotate the FCM registration token.
#pragma warning disable CS0618
            var token = await FirebaseMessaging.Instance.GetToken().AsAsync<Java.Lang.String>();
#pragma warning restore CS0618
            return token?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not obtain the FCM registration token.");
            return null;
        }
    }

    public async Task SubscribeToTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default)
    {
        var name = TopicName(topic);
        try
        {
            await FirebaseMessaging.Instance.SubscribeToTopic(name).AsAsync<Java.Lang.Object>();
            _logger.LogInformation("Subscribed to FCM topic {Topic}.", name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not subscribe to FCM topic {Topic}.", name);
        }
    }

    public async Task UnsubscribeFromTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default)
    {
        var name = TopicName(topic);
        try
        {
            await FirebaseMessaging.Instance.UnsubscribeFromTopic(name).AsAsync<Java.Lang.Object>();
            _logger.LogInformation("Unsubscribed from FCM topic {Topic}.", name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not unsubscribe from FCM topic {Topic}.", name);
        }
    }

    public async Task DeleteTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
#pragma warning disable CS0618
            await FirebaseMessaging.Instance.DeleteToken().AsAsync<Java.Lang.Object>();
#pragma warning restore CS0618
            _logger.LogInformation("FCM registration token deleted.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete the FCM registration token.");
        }
    }

    /// <summary>Canonical FCM topic name for a <see cref="NotificationTopic"/>.</summary>
    public static string TopicName(NotificationTopic topic) => topic.ToString().ToLowerInvariant();

    private void CreateNotificationChannel(PushConfiguration configuration)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var manager = (Android.App.NotificationManager?)Android.App.Application.Context
            .GetSystemService(Android.Content.Context.NotificationService);

        if (manager is null || manager.GetNotificationChannel(configuration.FcmNotificationChannelId) is not null)
        {
            return;
        }

        var channel = new Android.App.NotificationChannel(
            configuration.FcmNotificationChannelId,
            configuration.FcmNotificationChannelName,
            Android.App.NotificationImportance.Default)
        {
            Description = "Push notifications from the app.",
        };

        manager.CreateNotificationChannel(channel);
    }

    private void OnTokenRefreshed(object? sender, string token) => TokenRefreshed?.Invoke(this, token);

    private void OnMessageReceived(object? sender, PushMessage message) => MessageReceived?.Invoke(this, message);

    private void OnNotificationTapped(object? sender, PushMessage message) => NotificationTapped?.Invoke(this, message);
}
