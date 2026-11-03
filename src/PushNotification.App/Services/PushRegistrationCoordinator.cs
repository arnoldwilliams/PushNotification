using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PushNotification.App.Configuration;
using PushNotification.App.Models;
using PushNotification.App.Services.Api;
using PushNotification.App.Services.Notifications;
using PushNotification.App.Services.Preferences;

namespace PushNotification.App.Services;

/// <summary>
/// Orchestrates the full push lifecycle:
/// initialize providers, request permission, obtain the token, register the device,
/// save the installation record and keep the topic subscriptions in sync.
/// </summary>
public interface IPushRegistrationCoordinator
{
    /// <summary>Current push token, when one has been obtained.</summary>
    string? CurrentDeviceToken { get; }

    /// <summary>Latest device snapshot, refreshed after each registration.</summary>
    DeviceInstallation? CurrentInstallation { get; }

    /// <summary>
    /// Runs the full registration flow. Call this after the app has a UI and the user
    /// has had a chance to see why notifications are useful.
    /// </summary>
    Task<DeviceRegistrationResult> RegisterAsync(bool requestPermission = true, CancellationToken cancellationToken = default);

    /// <summary>Refreshes the device details and re-saves the installation record.</summary>
    Task<DeviceRegistrationResult> SaveInstallationAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes the installation from the backend and deletes the local token.</summary>
    Task UnregisterAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class PushRegistrationCoordinator : IPushRegistrationCoordinator
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly INotificationPermissionService _permissionService;
    private readonly IDeviceRegistrationService _registrationService;
    private readonly INotificationPreferencesService _preferencesService;
    private readonly IDeviceInfoProvider _deviceInfoProvider;
    private readonly PushOptions _options;
    private readonly ILogger<PushRegistrationCoordinator> _logger;

    public PushRegistrationCoordinator(
        IPushNotificationService pushNotificationService,
        INotificationPermissionService permissionService,
        IDeviceRegistrationService registrationService,
        INotificationPreferencesService preferencesService,
        IDeviceInfoProvider deviceInfoProvider,
        IOptions<PushOptions> options,
        ILogger<PushRegistrationCoordinator> logger)
    {
        _pushNotificationService = pushNotificationService;
        _permissionService = permissionService;
        _registrationService = registrationService;
        _preferencesService = preferencesService;
        _deviceInfoProvider = deviceInfoProvider;
        _options = options.Value;
        _logger = logger;

        _pushNotificationService.TokenRefreshed += OnTokenRefreshed;
    }

    public string? CurrentDeviceToken { get; private set; }

    public DeviceInstallation? CurrentInstallation { get; private set; }

    public async Task<DeviceRegistrationResult> RegisterAsync(
        bool requestPermission = true,
        CancellationToken cancellationToken = default)
    {
        var permission = requestPermission
            ? await _permissionService.RequestAsync(cancellationToken).ConfigureAwait(false)
            : await _permissionService.GetStatusAsync(cancellationToken).ConfigureAwait(false);

        if (permission != NotificationPermissionStatus.Granted)
        {
            _logger.LogInformation("Notification permission is {Status}; skipping registration.", permission);
            return DeviceRegistrationResult.Fail($"Notification permission is {permission}.");
        }

        await _pushNotificationService.InitializeAsync(BuildPushConfiguration(), cancellationToken)
            .ConfigureAwait(false);

        var token = await _pushNotificationService.GetDeviceTokenAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogInformation("No device token available yet; registration deferred.");
            return DeviceRegistrationResult.Fail("Device token is not available yet.");
        }

        CurrentDeviceToken = token;
        return await RegisterWithBackendAsync(token, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DeviceRegistrationResult> SaveInstallationAsync(CancellationToken cancellationToken = default)
    {
        var installation = _deviceInfoProvider.GetDeviceInfo();
        installation.DeviceToken = CurrentDeviceToken ?? string.Empty;
        CurrentInstallation = installation;

        return await _registrationService.SaveInstallationAsync(installation, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UnregisterAsync(CancellationToken cancellationToken = default)
    {
        var installationId = _deviceInfoProvider.GetOrCreateInstallationId();
        await _registrationService.UnregisterDeviceAsync(installationId, cancellationToken).ConfigureAwait(false);
        await _pushNotificationService.DeleteTokenAsync(cancellationToken).ConfigureAwait(false);
        CurrentDeviceToken = null;
    }

    private async Task<DeviceRegistrationResult> RegisterWithBackendAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var installation = _deviceInfoProvider.GetDeviceInfo();
        installation.DeviceToken = token;
        CurrentInstallation = installation;

        var request = new DeviceRegistrationRequest
        {
            InstallationId = installation.InstallationId,
            DeviceToken = token,
            Platform = installation.Platform,
            AppVersion = installation.AppVersion,
            Topics = _preferencesService.GetEnabledTopics().Select(t => t.ToString()).ToList(),
        };

        var registrationResult = await _registrationService
            .RegisterDeviceAsync(request, cancellationToken)
            .ConfigureAwait(false);

        // Persist the richer installation record alongside the token registration.
        await _registrationService.SaveInstallationAsync(installation, cancellationToken).ConfigureAwait(false);

        await _preferencesService.SynchronizeSubscriptionsAsync(cancellationToken).ConfigureAwait(false);

        return registrationResult;
    }

    private PushConfiguration BuildPushConfiguration() => new()
    {
        FcmSenderId = _options.FcmSenderId,
        FcmNotificationChannelId = _options.FcmNotificationChannelId,
        FcmNotificationChannelName = _options.FcmNotificationChannelName,
        ApnsEnvironment = _options.ApnsEnvironment,
        ApnsTopic = _options.ApnsTopic,
        ApiBaseUrl = _options.ApiBaseUrl,
    };

    private async void OnTokenRefreshed(object? sender, string token)
    {
        CurrentDeviceToken = token;
        _logger.LogInformation("Push token refreshed.");

        try
        {
            await RegisterWithBackendAsync(token, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not re-register the refreshed token.");
        }
    }
}
