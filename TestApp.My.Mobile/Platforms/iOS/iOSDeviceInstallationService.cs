using MyTestApp.Dto.Notifications.Service;
using TestApp.My.Mobile.LocalServices.Interfaces;
using UIKit;

namespace TestApp.My.Mobile.Platforms.iOS
{
    /// <summary>
    /// iOS implementation of <see cref="IDeviceInstallationService"/>.
    /// <para>
    /// The token (the APNs device token) is set by <see cref="iOSPushNotificationService"/>, which
    /// is driven by the view model, not by the app delegate.
    /// </para>
    /// </summary>
    public class iOSDeviceInstallationService : IDeviceInstallationService
    {
        private const int SupportedVersionMajor = 13;
        private const int SupportedVersionMinor = 0;

        public string? Token { get; set; }

        public bool NotificationsSupported
            => UIDevice.CurrentDevice.CheckSystemVersion(SupportedVersionMajor, SupportedVersionMinor);

        public string? GetDeviceId()
            => UIDevice.CurrentDevice.IdentifierForVendor?.ToString();

        public DeviceInstallationResponseDto GetDeviceInstallation(List<string>? tags = null)
        {
            if (!NotificationsSupported)
            {
                return new DeviceInstallationResponseDto
                {
                    IsError = true,
                    Error = "Notifications not Supported",
                    ErrorDescription = GetIosPnsNotificationsSupportError()
                };
            }

            if (string.IsNullOrWhiteSpace(Token))
            {
                return new DeviceInstallationResponseDto
                {
                    IsError = true,
                    Error = "Unable to resolve token for APNS"
                };
            }

            return new DeviceInstallationResponseDto
            {
                InstallationId = GetDeviceId(),
                Platform = NotificationPlatform.ApnsV1.ToString(),
                PushChannel = Token,
                Tags = tags
            };
        }

        private string GetIosPnsNotificationsSupportError()
        {
            if (!NotificationsSupported)
            {
                return $"This app only supports notifications on iOS {SupportedVersionMajor}.{SupportedVersionMinor} and above. "
                       + $"You are running {UIDevice.CurrentDevice.SystemVersion}.";
            }

            if (Token == null)
            {
                return "This app can support notifications but you must enable this in your settings.";
            }

            return "An error occurred preventing the use of push notifications";
        }
    }
}
