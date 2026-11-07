namespace MyTestApp.Dto.Notifications.Service
{
    /// <summary>
    /// Describes a device installation that can be registered with the notification hub.
    /// </summary>
    public class DeviceInstallationResponseDto
    {
        public string? InstallationId { get; set; }

        public string? Platform { get; set; }

        public string? PushChannel { get; set; }

        public List<string>? Tags { get; set; }

        public bool IsError { get; set; }

        public string? Error { get; set; }

        public string? ErrorDescription { get; set; }
    }
}
