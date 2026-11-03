using System.Text.Json;
using Microsoft.Extensions.Logging;
using PushNotification.App.Models;

namespace PushNotification.App.Services.Api;

/// <summary>
/// Dummy backend client used by the sample. It does not perform any network calls; it
/// simulates latency and logs the payload that would be sent to a real API.
/// Replace the method bodies with HTTP calls (e.g. <see cref="HttpClient"/>) when wiring
/// the app to a real service.
/// </summary>
public sealed class DummyDeviceRegistrationService : IDeviceRegistrationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private readonly ILogger<DummyDeviceRegistrationService> _logger;

    public DummyDeviceRegistrationService(ILogger<DummyDeviceRegistrationService> logger)
    {
        _logger = logger;
    }

    public async Task<DeviceRegistrationResult> RegisterDeviceAsync(
        DeviceRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.DeviceToken))
        {
            _logger.LogWarning("RegisterDeviceAsync called without a device token.");
            return DeviceRegistrationResult.Fail("A device token is required to register.");
        }

        await SimulateNetworkAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "[DUMMY] POST /api/devices/register\n{Payload}",
            JsonSerializer.Serialize(request, SerializerOptions));

        return DeviceRegistrationResult.Ok(request.InstallationId, "Device token registered.");
    }

    public async Task<DeviceRegistrationResult> SaveInstallationAsync(
        DeviceInstallation installation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installation);

        await SimulateNetworkAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "[DUMMY] PUT /api/installations/{InstallationId}\n{Payload}",
            installation.InstallationId,
            JsonSerializer.Serialize(installation, SerializerOptions));

        return DeviceRegistrationResult.Ok(installation.InstallationId, "Installation saved.");
    }

    public async Task<DeviceRegistrationResult> UnregisterDeviceAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationId);

        await SimulateNetworkAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("[DUMMY] DELETE /api/installations/{InstallationId}", installationId);

        return DeviceRegistrationResult.Ok(installationId, "Installation removed.");
    }

    public async Task<DeviceRegistrationResult> SaveNotificationPreferencesAsync(
        string installationId,
        IReadOnlyCollection<NotificationTopic> enabledTopics,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationId);
        ArgumentNullException.ThrowIfNull(enabledTopics);

        await SimulateNetworkAsync(cancellationToken).ConfigureAwait(false);

        var topics = enabledTopics.Select(t => t.ToString()).ToArray();
        _logger.LogInformation(
            "[DUMMY] PUT /api/installations/{InstallationId}/preferences\n{Payload}",
            installationId,
            JsonSerializer.Serialize(new { topics }, SerializerOptions));

        return DeviceRegistrationResult.Ok(installationId, "Preferences saved.");
    }

    private static Task SimulateNetworkAsync(CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken);
}
