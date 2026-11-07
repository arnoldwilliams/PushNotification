using MyTestApp.Dto.Notifications.Service;
using TestApp.My.Mobile.LocalServices.Interfaces;
using UIKit;

namespace TestApp.My.Mobile.Platforms.iOS
{
    /// <summary>
    /// iOS implementation of <see cref="IDeviceInstallationService"/>.
    /// <para>
    /// The token (APNs device token) is set by <see cref="iOSPushNotificationService"/>, which is
    /// driven by the view model, not by the app delegate.
    /// </para>
    /// </summary>
    public class iOSDeviceInstallationService : IDeviceInstallationService
    {
        public string? Token { get; set; }

        public bool NotificationsSupported
            => UIDevice.CurrentDevice.CheckSystemVersion(15, 0);

        public string GetDeviceId()
            => UIDevice.CurrentDevice.IdentifierForVendor?.ToString() ?? string.Empty;

        public DeviceInstallationResponseDto GetDeviceInstallation(List<string>? tags = null)
        {
            if (!NotificationsSupported)
            {
                return new DeviceInstallationResponseDto
                {
                    IsError = true,
                    Error = "Notifications not Supported",
                    ErrorDescription = "This iOS version does not support the required notification APIs."
                };
            }

            if (string.IsNullOrWhiteSpace(Token))
            {
                return new DeviceInstallationResponseDto
                {
                    IsError = true,
                    Error = "Unable to resolve token for APNSv1."
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
    }
}
