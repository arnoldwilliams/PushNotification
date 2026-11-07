using Android.Gms.Common;
using Microsoft.Azure.NotificationHubs;
using MyTestApp.Dto.Notifications.Service;
using TestApp.My.Mobile.LocalServices.Interfaces;
using static Android.Provider.Settings;


namespace TestApp.My.Mobile.Platforms.Android
{
    public class AndroidDeviceInstallationService : IDeviceInstallationService
    {
        public string Token { get; set; }

        public bool NotificationsSupported
            => GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext) == ConnectionResult.Success;

        public string GetDeviceId()
            => Secure.GetString(Platform.AppContext.ContentResolver, Secure.AndroidId);

        public DeviceInstallationResponseDto GetDeviceInstallation(List<string> tags = default)
        {
            if (!NotificationsSupported)
            {
                var error = GetGooglePlayServicesError();
                return new DeviceInstallationResponseDto
                {
                    IsError = true,
                    Error = "Notifications not Supported",
                    ErrorDescription = error
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

            var installation = new DeviceInstallationResponseDto
            {
                InstallationId = GetDeviceId(),
                Platform = NotificationPlatform.FcmV1.ToString(),
                PushChannel = Token,
                Tags = tags
            };

            return installation;
        }

        private string GetGooglePlayServicesError()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext);

            if (resultCode != ConnectionResult.Success)
            {
                return GoogleApiAvailability.Instance.IsUserResolvableError(resultCode) ?
                          GoogleApiAvailability.Instance.GetErrorString(resultCode) :
                          "This device isn't supported.";
            }

            return "An error occurred preventing the use of push notifications.";
        }
    }
}
