namespace MyTestApp.Dto.Notifications.Service
{
    /// <summary>
    /// Lightweight result used by the view model so it can surface success/failure without throwing.
    /// </summary>
    public sealed class NotificationRegistrationResult
    {
        public bool IsSuccess { get; init; }

        public string? Error { get; init; }

        public string? ErrorDescription { get; init; }

        public static NotificationRegistrationResult Success() => new() { IsSuccess = true };

        public static NotificationRegistrationResult Failure(string error, string? description = null)
            => new() { IsSuccess = false, Error = error, ErrorDescription = description };
    }
}
