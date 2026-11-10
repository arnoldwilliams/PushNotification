using System.Net;
using MyTestApp.Dto.Notifications;
using MyTestApp.Dto.Notifications.Service;
using TestApp.My.Mobile.Interfaces;
using TestApp.My.Mobile.LocalServices.Interfaces;
using TestApp.My.Mobile.Services;

namespace TestApp.My.Mobile.LocalServices.Services
{
    /// <summary>
    /// Registers / deregisters the current device installation with the notification backend.
    /// The push channel token (FCM registration token on Android, APNs device token on iOS)
    /// always comes from <see cref="IDeviceInstallationService"/>, so this class is platform agnostic.
    /// </summary>
    public sealed class NotificationRegistrationService : INotificationRegistrationService
    {
        private readonly IDeviceInstallationService _deviceInstallationService;

        private readonly IHttpCommunication _communicationService;

        private readonly ISecureStorageService _secureStorageService;

        private readonly string? _basePushNotificationsUrl;

        public NotificationRegistrationService(
            IDeviceInstallationService deviceInstallationService,
            IHttpCommunication communicationService,
            ISecureStorageService secureStorageService,
            IConfigurationService configurationService)
        {
            _deviceInstallationService = deviceInstallationService;
            _communicationService = communicationService;
            _secureStorageService = secureStorageService;
            _basePushNotificationsUrl = configurationService?.BasePushNotificationsUrl;
        }

        public async Task<NotificationRegistrationResult> RefreshRegistrationAsync(string? pushChannelToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pushChannelToken))
                {
                    return NotificationRegistrationResult.Failure(
                        "Push Notification Token", "The push channel token is empty.");
                }

                var cachedToken = await _secureStorageService
                    .GetCacheItemAsync(Helpers.Constants.PushToken)
                    .ConfigureAwait(false);

                var strTags = await _secureStorageService
                    .GetCacheItemAsync(Helpers.Constants.PushTags)
                    .ConfigureAwait(false);

                if (cachedToken == pushChannelToken)
                {
                    // Nothing changed since the last successful registration.
                    return NotificationRegistrationResult.Success();
                }

                return await RegisterDeviceAsync(strTags).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggingService<NotificationRegistrationService>.LogError(ex);
                return NotificationRegistrationResult.Failure("Refresh Registration", ex.Message);
            }
        }

        public async Task<NotificationRegistrationResult> RegisterDeviceAsync(string? strTags = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_basePushNotificationsUrl))
                {
                    return NotificationRegistrationResult.Failure(
                        "Base Push Notification Url", "Push Notifications base Url is missing.");
                }

                if (_deviceInstallationService is null)
                {
                    return NotificationRegistrationResult.Failure(
                        "Device Installation Service", "No device installation service is registered.");
                }

                var pushMessageTags = ConvertTagsToListString(strTags);

                var accessToken = await _secureStorageService.GetAccessToken().ConfigureAwait(true);

                var deviceInstallationContext = _deviceInstallationService.GetDeviceInstallation(pushMessageTags);

                if (deviceInstallationContext == null || deviceInstallationContext.IsError)
                {
                    return NotificationRegistrationResult.Failure(
                        deviceInstallationContext?.Error ?? "Device Installation",
                        deviceInstallationContext?.ErrorDescription);
                }

                var userId = await _secureStorageService.GetUserId() ?? string.Empty;

                var installationRequest = new UserDeviceRegistrationRequestDto
                {
                    InstallationId = deviceInstallationContext.InstallationId,
                    Platform = deviceInstallationContext.Platform,
                    PushChannel = deviceInstallationContext.PushChannel,
                    UserId = userId,
                    Tags = pushMessageTags ?? new List<string>()
                };

                if (string.IsNullOrWhiteSpace(installationRequest.InstallationId)
                    || string.IsNullOrWhiteSpace(installationRequest.Platform)
                    || string.IsNullOrWhiteSpace(installationRequest.PushChannel))
                {
                    return NotificationRegistrationResult.Failure(
                        "Installation Request", "Installation id, platform and push channel are all required.");
                }

                var registerUrl = $"{_basePushNotificationsUrl}/installations";

                var httpResult = await _communicationService
                    .PostAsync(new Uri(registerUrl), installationRequest, accessToken)
                    .ConfigureAwait(true);

                httpResult.EnsureSuccessStatusCode();

                await _secureStorageService
                    .SaveItemToCacheAsync(Helpers.Constants.DeviceId, installationRequest.InstallationId)
                    .ConfigureAwait(false);

                await _secureStorageService
                    .SaveItemToCacheAsync(Helpers.Constants.PushToken, installationRequest.PushChannel)
                    .ConfigureAwait(false);

                await _secureStorageService
                    .SaveItemToCacheAsync(Helpers.Constants.PushTags, strTags ?? string.Empty)
                    .ConfigureAwait(false);

                return NotificationRegistrationResult.Success();
            }
            catch (Exception ex)
            {
                LoggingService<NotificationRegistrationService>.LogError(ex);
                return NotificationRegistrationResult.Failure("Register Device", ex.Message);
            }
        }

        public async Task<NotificationRegistrationResponseDto> DeregisterDeviceAsync()
        {
            try
            {
                var cachedToken = await _secureStorageService
                    .GetCacheItemAsync(Helpers.Constants.PushToken)
                    .ConfigureAwait(true);

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

                await _communicationService
                    .DeleteAsync(new Uri(deleteDeviceResourceUrl), accessToken)
                    .ConfigureAwait(true);

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

        public async Task<bool> SaveDeviceRegistrationDetailsAsync(DeviceRegistrationDto deviceRegdto)
        {
            try
            {
                var accessToken = await _secureStorageService.GetAccessToken();

                if (string.IsNullOrEmpty(accessToken))
                {
                    return false;
                }

                var httpResult = await _communicationService
                    .PostAsync(new Uri($"{_basePushNotificationsUrl}/DeviceRegistration/register"), deviceRegdto, accessToken)
                    .ConfigureAwait(true);

                return httpResult.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LoggingService<NotificationRegistrationService>.LogError(ex);
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

                var httpResult = await _communicationService
                    .PostAsync(new Uri($"{_basePushNotificationsUrl}/DeviceRegistration/unregister"), deviceToken, accessToken)
                    .ConfigureAwait(true);

                return httpResult.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LoggingService<NotificationRegistrationService>.LogError(ex);
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

                var httpResult = await _communicationService
                    .GetAsync(new Uri($"{_basePushNotificationsUrl}/DeviceRegistration/isdeviceregistered/{deviceToken}"), accessToken)
                    .ConfigureAwait(true);

                if (httpResult.StatusCode == HttpStatusCode.OK)
                {
                    using var content = httpResult.Content;
                    var resultString = await content.ReadAsStringAsync().ConfigureAwait(true);
                    return bool.TryParse(resultString, out var isRegistered) && isRegistered;
                }
            }
            catch (Exception ex)
            {
                LoggingService<NotificationRegistrationService>.LogError(ex);
            }

            return false;
        }

        private static List<string>? ConvertTagsToListString(string? tags)
            => string.IsNullOrWhiteSpace(tags)
                ? null
                : tags.Split(',').ToList();
    }
}
