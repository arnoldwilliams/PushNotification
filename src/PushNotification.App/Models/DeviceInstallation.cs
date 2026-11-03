namespace PushNotification.App.Models;

/// <summary>
/// A single point-in-time snapshot of the device the app is running on.
/// Collected once per installation and re-sent whenever the token or app version changes.
/// </summary>
public sealed class DeviceInstallation
{
    /// <summary>Stable identifier for this app installation (survives token refreshes).</summary>
    public string InstallationId { get; set; } = string.Empty;

    /// <summary>FCM registration token (Android) or APNs device token (iOS).</summary>
    public string DeviceToken { get; set; } = string.Empty;

    /// <summary>Which platform produced the token.</summary>
    public PlatformType Platform { get; set; } = PlatformType.Unknown;

    /// <summary>Raw platform string, e.g. "Android" / "iOS" / "WinUI".</summary>
    public string PlatformName { get; set; } = string.Empty;

    /// <summary>Hardware model, e.g. "Pixel 8" or "iPhone15,2".</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Device manufacturer, e.g. "Google" or "Apple".</summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>Marketing device name the user would recognize.</summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>OS version string, e.g. "Android 15" or "iOS 18.1".</summary>
    public string OsVersion { get; set; } = string.Empty;

    /// <summary>App display version, e.g. "1.0".</summary>
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>Monotonic build number of the app.</summary>
    public string AppBuild { get; set; } = string.Empty;

    /// <summary>App bundle/package identifier.</summary>
    public string BundleId { get; set; } = string.Empty;

    /// <summary>Two letter ISO language code reported by the device.</summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>IANA time zone of the device.</summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>Locale identifier, e.g. "en-US".</summary>
    public string Locale { get; set; } = string.Empty;

    /// <summary>Logical screen resolution in device independent pixels.</summary>
    public string ScreenResolution { get; set; } = string.Empty;

    /// <summary>Device pixel density.</summary>
    public double ScreenDensity { get; set; }

    /// <summary>Idiom of the device: Phone, Tablet, Desktop.</summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>True when the device reports a physical/emulated simulator.</summary>
    public bool IsEmulator { get; set; }

    /// <summary>UTC timestamp this snapshot was captured.</summary>
    public DateTimeOffset CapturedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
