namespace MyTestApp.Dto.Notifications.Service
{
    /// <summary>
    /// Result of a register / deregister call.
    /// </summary>
    public class NotificationRegistrationResponseDto
    {
        public string? UserId { get; set; }

        public string? InstallationId { get; set; }

        public bool IsSuccess { get; set; }

        public string? Error { get; set; }

        public string? ErrorDescription { get; set; }
    }
}
