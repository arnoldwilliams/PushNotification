using MyTestApp.Dto.Notifications.Service;

namespace TestApp.My.Mobile.LocalServices.Interfaces
{
    /// <summary>
    /// Platform specific access to the device push channel (token) and device identity.
    /// </summary>
    public interface IDeviceInstallationService
    {
        /// <summary>Current push channel token, or null when not yet resolved.</summary>
        string? Token { get; set; }

        /// <summary>True when the device is capable of receiving push notifications.</summary>
        bool NotificationsSupported { get; }

        /// <summary>Stable, installation scoped device identifier.</summary>
        string? GetDeviceId();

        /// <summary>Builds the installation payload that is sent to the backend.</summary>
        DeviceInstallationResponseDto GetDeviceInstallation(List<string>? tags = null);
    }
}
