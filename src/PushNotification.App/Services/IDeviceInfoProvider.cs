using PushNotification.App.Models;

namespace PushNotification.App.Services;

/// <summary>
/// Collects device and installation metadata that is reported to the backend.
/// </summary>
public interface IDeviceInfoProvider
{
    /// <summary>
    /// Returns a stable identifier for this app installation.
    /// The value is generated once and persisted, so it survives app restarts and
    /// token refreshes but changes when the app is uninstalled and reinstalled.
    /// </summary>
    string GetOrCreateInstallationId();

    /// <summary>Builds a full snapshot of the current device.</summary>
    DeviceInstallation GetDeviceInfo();
}
