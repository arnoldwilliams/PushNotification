namespace MyTestApp.Dto.Notifications
{
    /// <summary>
    /// Legacy device registration payload used by the secondary registration endpoints.
    /// </summary>
    public class DeviceRegistrationDto
    {
        public string? DeviceToken { get; set; }

        public string? UserId { get; set; }

        public string? Platform { get; set; }
    }
}
