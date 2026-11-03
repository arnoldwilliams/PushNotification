using Microsoft.Extensions.Logging;
using PushNotification.App.Models;
using PushNotification.App.Services.Api;
using PushNotification.App.Services.Notifications;

namespace PushNotification.App.Services.Preferences;

/// <summary>
/// Persists topic opt-ins in <see cref="IPreferences"/> and mirrors each change to the
/// push provider (FCM/APNs topic subscription) and the backend.
/// </summary>
public sealed class NotificationPreferencesService : INotificationPreferencesService
{
    private static readonly (NotificationTopic Topic, string Display, string Description)[] Catalog =
    {
        (NotificationTopic.Announcements, "Product announcements", "News about releases and major changes."),
        (NotificationTopic.AccountAlerts, "Account alerts", "Security, sign-in and billing alerts."),
        (NotificationTopic.OrderUpdates, "Order updates", "Shipping and delivery status changes."),
        (NotificationTopic.Promotions, "Promotions", "Offers, discounts and limited-time deals."),
        (NotificationTopic.Tips, "Tips and guides", "How-to content and feature highlights."),
        (NotificationTopic.WeeklyDigest, "Weekly digest", "A single summary email-style push each week."),
    };

    private readonly IPreferences _preferences;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IDeviceRegistrationService _registrationService;
    private readonly IDeviceInfoProvider _deviceInfoProvider;
    private readonly ILogger<NotificationPreferencesService> _logger;

    public NotificationPreferencesService(
        IPreferences preferences,
        IPushNotificationService pushNotificationService,
        IDeviceRegistrationService registrationService,
        IDeviceInfoProvider deviceInfoProvider,
        ILogger<NotificationPreferencesService> logger)
    {
        _preferences = preferences;
        _pushNotificationService = pushNotificationService;
        _registrationService = registrationService;
        _deviceInfoProvider = deviceInfoProvider;
        _logger = logger;
    }

    public Task<IReadOnlyList<NotificationPreference>> GetPreferencesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<NotificationPreference> preferences = Catalog
            .Select(entry => new NotificationPreference(entry.Topic, entry.Display, entry.Description)
            {
                IsEnabled = IsEnabled(entry.Topic),
            })
            .ToList();

        return Task.FromResult(preferences);
    }

    public bool IsEnabled(NotificationTopic topic) =>
        _preferences.Get(PreferenceKey(topic), DefaultEnabled(topic));

    public async Task<IReadOnlyList<NotificationPreference>> SetEnabledAsync(
        NotificationTopic topic,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        _preferences.Set(PreferenceKey(topic), enabled);

        await ApplySubscriptionAsync(topic, enabled, cancellationToken).ConfigureAwait(false);
        await PersistToBackendAsync(cancellationToken).ConfigureAwait(false);

        return await GetPreferencesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<NotificationPreference>> SetAllAsync(
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        foreach (var entry in Catalog)
        {
            _preferences.Set(PreferenceKey(entry.Topic), enabled);
            await ApplySubscriptionAsync(entry.Topic, enabled, cancellationToken).ConfigureAwait(false);
        }

        await PersistToBackendAsync(cancellationToken).ConfigureAwait(false);

        return await GetPreferencesAsync(cancellationToken).ConfigureAwait(false);
    }

    public IReadOnlyCollection<NotificationTopic> GetEnabledTopics() =>
        Catalog
            .Where(entry => IsEnabled(entry.Topic))
            .Select(entry => entry.Topic)
            .ToArray();

    public async Task SynchronizeSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in Catalog)
        {
            await ApplySubscriptionAsync(entry.Topic, IsEnabled(entry.Topic), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task ApplySubscriptionAsync(
        NotificationTopic topic,
        bool enabled,
        CancellationToken cancellationToken)
    {
        try
        {
            if (enabled)
            {
                await _pushNotificationService.SubscribeToTopicAsync(topic, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _pushNotificationService.UnsubscribeFromTopicAsync(topic, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // A failed subscription must not block saving the user's choice locally.
            _logger.LogWarning(ex, "Could not update push subscription for topic {Topic}.", topic);
        }
    }

    private async Task PersistToBackendAsync(CancellationToken cancellationToken)
    {
        try
        {
            var installationId = _deviceInfoProvider.GetOrCreateInstallationId();
            await _registrationService
                .SaveNotificationPreferencesAsync(installationId, GetEnabledTopics(), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not sync notification preferences with the backend.");
        }
    }

    private static string PreferenceKey(NotificationTopic topic) => $"pn.topic.{topic}";

    private static bool DefaultEnabled(NotificationTopic topic) =>
        topic is NotificationTopic.AccountAlerts or NotificationTopic.OrderUpdates;
}
