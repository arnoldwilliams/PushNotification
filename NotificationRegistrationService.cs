using MyTestApp.Dto;
using MyTestApp.Dto.Notifications;
using MyTestApp.Dto.Notifications.Service;
using Newtonsoft.Json;
using TestApp.My.Mobile.Interfaces;
using TestApp.My.Mobile.LocalServices.Interfaces;
using TestApp.My.Mobile.Services;

namespace TestApp.My.Mobile.LocalServices.Services
{

    public sealed class NotificationRegistrationService : INotificationRegistrationService
    {


        private static IDeviceInstallationService _deviceInstallationService;


        private readonly IHttpCommunication _communicationService;


        private ISecureStorageService _secureStorageService;


        private readonly IConfigurationService _configurationService;


        private readonly string _basePushNotificationsUrl = string.Empty;

        public NotificationRegistrationService(IDeviceInstallationService deviceInstallationService, IHttpCommunication communicationService, ISecureStorageService secureStorageService, IConfigurationService configurationService)
        {
            _communicationService = communicationService;
            _secureStorageService = secureStorageService;
            _deviceInstallationService = deviceInstallationService;
            _configurationService = configurationService;
            _basePushNotificationsUrl = _configurationService?.BasePushNotificationsUrl;
        }


        public async Task<NotificationRegistrationResponseDto> DeregisterDeviceAsync()
        {
            try
            {
                string cachedToken = await _secureStorageService.GetCacheItemAsync(Helpers.Constants.PushToken).ConfigureAwait(true);
                if (cachedToken == null)
                {
                    return new NotificationRegistrationResponseDto
                    {
                        Error = "Push Notification Token",
                        ErrorDescription = "Push Token is missing"
                    };
                }

                var deviceId = _deviceInstallationService?.GetDeviceId();
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    return new NotificationRegistrationResponseDto
                    {
                        Error = "Device ID",
                        ErrorDescription = "Unable to resolve an ID for this device."
                    };
                }

                if (string.IsNullOrWhiteSpace(_basePushNotificationsUrl))
                {
                    return new NotificationRegistrationResponseDto
                    {
                        Error = "Base Push Notification Url",
                        ErrorDescription = "Push Notifications base Url is missing."
                    };
                }

                var accessToken = await _secureStorageService.GetAccessToken().ConfigureAwait(true);
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    return new NotificationRegistrationResponseDto
                    {
                        Error = "Access Token",
                        ErrorDescription = "accessToken is missing."
                    };
                }

                var deleteDeviceResourceUrl = $"{_basePushNotificationsUrl}/installations/{deviceId}";

                var result = await _communicationService.DeleteAsync(new Uri(deleteDeviceResourceUrl), accessToken).ConfigureAwait(true);

                await _secureStorageService.RemoveCacheItemAsync(Helpers.Constants.PushToken);

                await _secureStorageService.RemoveCacheItemAsync(Helpers.Constants.PushTags);

                var userId = await _secureStorageService.GetUserId();
                return new NotificationRegistrationResponseDto
                {
                    UserId = userId,
                    InstallationId = deviceId,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                LoggingService<NotificationRegistrationService>.LogError(ex);
                throw;
            }
        }


        public async Task RefreshRegistrationAsync(string pushChannelToken)
        {
            try
            {
                var cachedToken = await _secureStorageService.GetCacheItemAsync(Helpers.Constants.PushToken).ConfigureAwait(false);

                var strTags = await _secureStorageService.GetCacheItemAsync(Helpers.Constants.PushTags).ConfigureAwait(false);

                string newToken = pushChannelToken;
                if (cachedToken == newToken)
                {
                    return;
                }

                await RegisterDeviceAsync(strTags);
            }
            catch (Exception ex)
            {
                LoggingService<NotificationRegistrationService>.LogError(ex);
            }
        }


        public async Task RegisterDeviceAsync(string strTags = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_basePushNotificationsUrl))
                {
                    throw new Exception("Push Notifications Url is missing");
                }

                List<string> pushMessageTags = ConvertTagsToListString(strTags);

                var accessToken = await _secureStorageService.GetAccessToken().ConfigureAwait(true);

                var deviceInstallationContext = _deviceInstallationService?.GetDeviceInstallation(pushMessageTags);
                if (deviceInstallationContext == null || deviceInstallationContext.IsError)
                {
                    return;
                }

                var userId = await _secureStorageService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    userId = "";
                }

                var installationRequest = new UserDeviceRegistrationRequestDto
                {
                    InstallationId = deviceInstallationContext.InstallationId,
                    Platform = deviceInstallationContext.Platform,
                    PushChannel = deviceInstallationContext.PushChannel,
                    UserId = userId,
                    Tags = new List<string>()
                };

                if (string.IsNullOrWhiteSpace(installationRequest?.InstallationId))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(installationRequest?.Platform))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(installationRequest?.PushChannel))
                {
                    return;
                }

                var registerUrl = $"{_basePushNotificationsUrl}/installations";

                var httpResult = await _communicationService.PostAsync(new Uri(registerUrl), installationRequest, accessToken);
                httpResult.EnsureSuccessStatusCode();

                var responseContentText = await httpResult.Content.ReadAsStringAsync();

                await _secureStorageService.SaveItemToCacheAsync(Helpers.Constants.DeviceId, installationRequest.InstallationId).ConfigureAwait(false);

                await _secureStorageService.SaveItemToCacheAsync(Helpers.Constants.PushToken, installationRequest.PushChannel).ConfigureAwait(false);

                await _secureStorageService.SaveItemToCacheAsync(Helpers.Constants.PushTags, strTags);

                string test = await _secureStorageService.GetCacheItemAsync(Helpers.Constants.PushToken).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                LoggingService<NotificationRegistrationService>.LogError(ex);
            }
        }



        private List<string> ConvertTagsToListString(string tags)
        {
            List<string> listTags = string.IsNullOrWhiteSpace(tags)
                ? null :
                tags.Split(',').ToList();
            return listTags;
        }


        public async Task<bool> SaveDeviceRegistrationDetailsAsync(DeviceRegistrationDto deviceRegdto)
        {
            try
            {
                var accessToken = await _secureStorageService.GetAccessToken();

                if (string.IsNullOrEmpty(accessToken))
                {
                    return false;
                }


                var httpResult = await _communicationService.PostAsync(new Uri($"{_configurationService.ViewitUrl}/DeviceRegistration/register"), deviceRegdto, accessToken).ConfigureAwait(true);

                return httpResult.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LoggingService<DataService>.LogError(ex);
            }

            return false;
        }


        public async Task<bool> DeleteDeviceRegistrationDetailsAsync(string deviceToken)
        {
            try
            {
                var accessToken = await _secureStorageService.GetAccessToken();

                if (string.IsNullOrEmpty(accessToken))
                {
                    return false;
                }


                var httpResult = await _communicationService.PostAsync(new Uri($"{_configurationService.ViewitUrl}/DeviceRegistration/unregister"), deviceToken, accessToken).ConfigureAwait(true);

                return httpResult.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LoggingService<DataService>.LogError(ex);
            }

            return false;
        }


        public async Task<bool> IsDeviceRegisteredAsync(string deviceToken)
        {
            try
            {
                var accessToken = await _secureStorageService.GetAccessToken();

                if (string.IsNullOrEmpty(accessToken))
                {
                    return false;
                }


                var httpResult = await _communicationService.GetAsync(new Uri($"{_configurationService.ViewitUrl}/DeviceRegistration/isdeviceregistered/{deviceToken}"), accessToken).ConfigureAwait(true);

                if (httpResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    using (var content = httpResult.Content)
                    {
                        var resultString = await content.ReadAsStringAsync().ConfigureAwait(true);
                        return JsonConvert.DeserializeObject<bool>(resultString);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService<DataService>.LogError(ex);
            }

            return false;
        }

    }
}
