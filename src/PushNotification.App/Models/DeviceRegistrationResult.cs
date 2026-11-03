namespace PushNotification.App.Models;

/// <summary>
/// Response returned by the backend after a registration or installation save.
/// </summary>
public sealed class DeviceRegistrationResult
{
    public bool Success { get; set; }

    /// <summary>Server-side identifier for the stored installation.</summary>
    public string? InstallationId { get; set; }

    /// <summary>Human readable status or error message.</summary>
    public string? Message { get; set; }

    public DateTimeOffset ProcessedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public static DeviceRegistrationResult Ok(string installationId, string message = "Registered") =>
        new() { Success = true, InstallationId = installationId, Message = message };

    public static DeviceRegistrationResult Fail(string message) =>
        new() { Success = false, Message = message };
}
