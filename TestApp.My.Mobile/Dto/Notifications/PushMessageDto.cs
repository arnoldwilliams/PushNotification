namespace MyTestApp.Dto.Notifications
{
    /// <summary>
    /// A push message that was received and is handed to the UI layer.
    /// </summary>
    public class PushMessageDto
    {
        public string? Title { get; set; }

        public string? Body { get; set; }

        public string? Category { get; set; }

        public string? JsonPayload { get; set; }

        public string? ExternalDetailsUrl { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
