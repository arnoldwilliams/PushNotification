using MyTestApp.Dto.Notifications;
using MyTestApp.Dto.Notifications.Service;

namespace TestApp.My.Mobile.LocalServices.Interfaces
{
    /// <summary>
    /// Registers / deregisters the current device installation with the notification backend.
    /// </summary>
    public interface INotificationRegistrationService
    {
        /// <summary>Re-registers the device when the push channel token changed. Safe to call repeatedly.</summary>
        Task<NotificationRegistrationResult> RefreshRegistrationAsync(string? pushChannelToken);

        /// <summary>Registers (or updates) the device installation.</summary>
        Task<NotificationRegistrationResult> RegisterDeviceAsync(string? strTags = null);

        /// <summary>Removes the device installation from the backend.</summary>
        Task<NotificationRegistrationResponseDto> DeregisterDeviceAsync();

        Task<bool> SaveDeviceRegistrationDetailsAsync(DeviceRegistrationDto deviceRegdto);

        Task<bool> DeleteDeviceRegistrationDetailsAsync(string deviceToken);

        Task<bool> IsDeviceRegisteredAsync(string deviceToken);
    }
}
