namespace PushNotification.App.Models;

/// <summary>
/// Payload sent to the backend when a device should start receiving push notifications.
/// </summary>
public sealed class DeviceRegistrationRequest
{
    public string InstallationId { get; set; } = string.Empty;

    public string DeviceToken { get; set; } = string.Empty;

    public PlatformType Platform { get; set; } = PlatformType.Unknown;

    /// <summary>Signed-in user, when the app has one. Anonymous installs are allowed.</summary>
    public string? UserId { get; set; }

    /// <summary>Topics the installation wants to receive at registration time.</summary>
    public List<string> Topics { get; set; } = new();

    /// <summary>App version that produced the token, used for support diagnostics.</summary>
    public string AppVersion { get; set; } = string.Empty;
}
