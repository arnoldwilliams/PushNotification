using Android.Gms.Common;
using MyTestApp.Dto.Notifications.Service;
using TestApp.My.Mobile.LocalServices.Interfaces;
using static Android.Provider.Settings;

namespace TestApp.My.Mobile.Platforms.Android
{
    /// <summary>
    /// Android implementation of <see cref="IDeviceInstallationService"/>.
    /// <para>
    /// The token (FCM registration token) is set by
    /// <see cref="AndroidPushNotificationService"/>, which is driven by the view model, not by
    /// <c>MainActivity.OnCreate</c>.
    /// </para>
    /// </summary>
    public class AndroidDeviceInstallationService : IDeviceInstallationService
    {
        public string? Token { get; set; }

        public bool NotificationsSupported
            => GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext) == ConnectionResult.Success;

        public string? GetDeviceId()
            => Secure.GetString(Platform.AppContext.ContentResolver, Secure.AndroidId);

        public DeviceInstallationResponseDto GetDeviceInstallation(List<string>? tags = null)
        {
            if (!NotificationsSupported)
            {
                return new DeviceInstallationResponseDto
                {
                    IsError = true,
                    Error = "Notifications not Supported",
                    ErrorDescription = GetGooglePlayServicesError()
                };
            }

            if (string.IsNullOrWhiteSpace(Token))
            {
                return new DeviceInstallationResponseDto
                {
                    IsError = true,
                    Error = "Unable to resolve token for FCMv1."
                };
            }

            return new DeviceInstallationResponseDto
            {
                InstallationId = GetDeviceId(),
                Platform = NotificationPlatform.FcmV1.ToString(),
                PushChannel = Token,
                Tags = tags
            };
        }

        private string GetGooglePlayServicesError()
        {
            var resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext);

            if (resultCode != ConnectionResult.Success)
            {
                return GoogleApiAvailability.Instance.IsUserResolvableError(resultCode)
                    ? GoogleApiAvailability.Instance.GetErrorString(resultCode)
                    : "This device isn't supported.";
            }

            return "An error occurred preventing the use of push notifications.";
        }
    }
}
