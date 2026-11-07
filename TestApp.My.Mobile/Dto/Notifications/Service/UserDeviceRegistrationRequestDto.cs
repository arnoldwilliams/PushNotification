namespace MyTestApp.Dto.Notifications.Service
{
    /// <summary>
    /// Payload posted to the registration endpoint.
    /// </summary>
    public class UserDeviceRegistrationRequestDto
    {
        public string? InstallationId { get; set; }

        public string? Platform { get; set; }

        public string? PushChannel { get; set; }

        public string? UserId { get; set; }

        public List<string>? Tags { get; set; } = new();
    }
}
