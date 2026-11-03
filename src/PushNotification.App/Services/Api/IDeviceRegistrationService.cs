using PushNotification.App.Models;

namespace PushNotification.App.Services.Api;

/// <summary>
/// Backend operations for device/installation lifecycle.
/// </summary>
public interface IDeviceRegistrationService
{
    /// <summary>
    /// Registers a device token with the backend so the server can target this install.
    /// </summary>
    Task<DeviceRegistrationResult> RegisterDeviceAsync(
        DeviceRegistrationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves (upserts) the full installation record including device details.
    /// </summary>
    Task<DeviceRegistrationResult> SaveInstallationAsync(
        DeviceInstallation installation,
        CancellationToken cancellationToken = default);

    /// <summary>Removes the installation, e.g. on sign-out or when the token is invalid.</summary>
    Task<DeviceRegistrationResult> UnregisterDeviceAsync(
        string installationId,
        CancellationToken cancellationToken = default);

    /// <summary>Pushes the user's topic opt-in choices to the backend.</summary>
    Task<DeviceRegistrationResult> SaveNotificationPreferencesAsync(
        string installationId,
        IReadOnlyCollection<NotificationTopic> enabledTopics,
        CancellationToken cancellationToken = default);
}
