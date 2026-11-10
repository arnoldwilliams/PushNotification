using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyTestApp.Dto.Notifications;
using TestApp.My.Mobile.Common;
using TestApp.My.Mobile.Helpers;
using TestApp.My.Mobile.Interfaces;
using TestApp.My.Mobile.LocalServices.Interfaces;
using TestApp.My.Mobile.Services;

namespace TestApp.My.Mobile.ViewModels
{
    /// <summary>
    /// Owns the push notification device registration lifecycle.
    /// <para>
    /// This is the important difference from the original sample: the registration work
    /// (<c>FirebaseMessaging.Instance.GetToken()</c> + <c>RefreshRegistrationAsync</c>) no longer
    /// happens in <c>MainActivity.OnCreate</c>. Instead <see cref="InitializeAsync"/> is called
    /// from the page's <c>OnAppearing</c> (or at app start) and all of the logic lives here.
    /// </para>
    /// <para>
    /// The view model only knows about abstractions:
    /// <see cref="IPushNotificationService"/> for platform calls,
    /// <see cref="IDeviceInstallationService"/> for the device installation payload and
    /// <see cref="INotificationRegistrationService"/> for the backend calls.
    /// That keeps it unit testable and identical for Android and iOS.
    /// </para>
    /// </summary>
    public partial class PushNotificationViewModel : ObservableObject
    {
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IDeviceInstallationService _deviceInstallationService;
        private readonly INotificationRegistrationService _notificationRegistrationService;
        private readonly ISecureStorageService _secureStorageService;

        private bool _subscribed;

        public PushNotificationViewModel(
            IPushNotificationService pushNotificationService,
            IDeviceInstallationService deviceInstallationService,
            INotificationRegistrationService notificationRegistrationService,
            ISecureStorageService secureStorageService)
        {
            _pushNotificationService = pushNotificationService;
            _deviceInstallationService = deviceInstallationService;
            _notificationRegistrationService = notificationRegistrationService;
            _secureStorageService = secureStorageService;
        }

        [ObservableProperty]
        private string? _deviceToken;

        [ObservableProperty]
        private string? _deviceId;

        [ObservableProperty]
        private string _statusMessage = "Not registered";

        [ObservableProperty]
        private bool _isRegistered;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string? _lastMessageTitle;

        [ObservableProperty]
        private string? _lastMessageBody;

        /// <summary>
        /// Wires up the platform events. Safe to call more than once.
        /// </summary>
        public void Subscribe()
        {
            if (_subscribed || _pushNotificationService is null)
            {
                return;
            }

            _pushNotificationService.TokenRefreshed += OnTokenRefreshed;
            _pushNotificationService.NotificationReceived += OnNotificationReceived;
            _pushNotificationService.NotificationOpened += OnNotificationOpened;
            _subscribed = true;
        }

        /// <summary>
        /// Requests permission, resolves the push channel token and registers the device.
        /// Call this from the page's <c>OnAppearing</c> instead of <c>MainActivity.OnCreate</c>.
        /// </summary>
        [RelayCommand]
        public async Task InitializeAsync()
        {
            if (IsBusy || _pushNotificationService is null)
            {
                return;
            }

            try
            {
                IsBusy = true;

                Subscribe();

                DeviceId = _deviceInstallationService.GetDeviceId();

                // Restore the last known registration from secure storage so returning users see
                // their state immediately. The token is only written on a successful registration
                // and removed on deregistration, so its presence means the last attempt succeeded.
                var cachedToken = await _secureStorageService
                    .GetCacheItemAsync(Constants.PushToken)
                    .ConfigureAwait(true);

                DeviceToken = cachedToken;
                IsRegistered = !string.IsNullOrWhiteSpace(cachedToken);

                if (!_deviceInstallationService.NotificationsSupported)
                {
                    IsRegistered = false;
                    StatusMessage = "Notifications are not supported on this device.";
                    return;
                }

                var granted = await _pushNotificationService.RequestPermissionAsync().ConfigureAwait(true);

                if (!granted)
                {
                    IsRegistered = false;
                    StatusMessage = "Notification permission was denied.";
                    return;
                }

                var token = await _pushNotificationService.GetPushChannelTokenAsync().ConfigureAwait(true);

                if (string.IsNullOrWhiteSpace(token))
                {
                    // Keep the cached state; the last known token may still be valid server side.
                    StatusMessage = IsRegistered
                        ? "Using the last known push token; the live token could not be resolved."
                        : "Unable to resolve a push channel token.";
                    return;
                }

                await ApplyTokenAsync(token).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                LoggingService<PushNotificationViewModel>.LogError(ex);
                IsRegistered = false;
                StatusMessage = $"Registration failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Unregisters the current device from the backend.
        /// </summary>
        [RelayCommand]
        public async Task DeregisterAsync()
        {
            try
            {
                IsBusy = true;

                var result = await _notificationRegistrationService.DeregisterDeviceAsync().ConfigureAwait(true);

                if (result?.IsSuccess == true)
                {
                    // The backend removed the installation; drop the cached token so the next
                    // InitializeAsync does not restore a stale "registered" state.
                    await _secureStorageService
                        .RemoveCacheItemAsync(Constants.PushToken)
                        .ConfigureAwait(true);

                    DeviceToken = null;
                    IsRegistered = false;
                    StatusMessage = "Device deregistered.";
                }
                else
                {
                    StatusMessage = result?.ErrorDescription ?? "Deregistration failed.";
                }
            }
            catch (Exception ex)
            {
                LoggingService<PushNotificationViewModel>.LogError(ex);
                StatusMessage = $"Deregistration failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnTokenRefreshed(object? sender, string token)
        {
            // FCM / APNs can hand out a new token at any time (reinstall, restore, rotation).
            _ = ApplyTokenAsync(token);
        }

        private async Task ApplyTokenAsync(string token)
        {
            DeviceToken = token;
            _deviceInstallationService.Token = token;

            var result = await _notificationRegistrationService
                .RefreshRegistrationAsync(token)
                .ConfigureAwait(true);

            IsRegistered = result.IsSuccess;

            StatusMessage = result.IsSuccess
                ? "Device registered for push notifications."
                : $"Registration failed: {result.ErrorDescription ?? result.Error}";

            if (result.IsSuccess)
            {
                await _secureStorageService
                    .SaveItemToCacheAsync(Constants.PushToken, token)
                    .ConfigureAwait(true);
            }
        }

        private void OnNotificationReceived(object? sender, PushMessageDto message)
        {
            LastMessageTitle = message?.Title;
            LastMessageBody = message?.Body;

            MessageBus.Instance.Publish(Constants.NotificationReceived, message);
        }

        private void OnNotificationOpened(object? sender, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                Microsoft.Maui.Controls.Application.Current?.SendOnAppLinkRequestReceived(uri);
            }
        }
    }
}
