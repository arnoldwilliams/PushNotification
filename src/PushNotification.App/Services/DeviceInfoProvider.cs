using PushNotification.App.Models;

namespace PushNotification.App.Services;

/// <summary>
/// Default <see cref="IDeviceInfoProvider"/> built on MAUI Essentials.
/// Platform specific details (model, manufacturer, emulator flag) are filled in by
/// the partial <c>DeviceInfoProvider.Platform</c> implementation for each target.
/// </summary>
public sealed partial class DeviceInfoProvider : IDeviceInfoProvider
{
    private const string InstallationIdKey = "pn.installation_id";

    private readonly IPreferences _preferences;

    public DeviceInfoProvider(IPreferences preferences)
    {
        _preferences = preferences;
    }

    public string GetOrCreateInstallationId()
    {
        var existing = _preferences.Get<string?>(InstallationIdKey, null);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var installationId = Guid.NewGuid().ToString("D");
        _preferences.Set(InstallationIdKey, installationId);
        return installationId;
    }

    public DeviceInstallation GetDeviceInfo()
    {
        var platform = GetPlatform();
        var platformInfo = GetPlatformDetails();
        var deviceInfo = Microsoft.Maui.Devices.DeviceInfo.Current;

        return new DeviceInstallation
        {
            InstallationId = GetOrCreateInstallationId(),
            Platform = platform,
            PlatformName = platform.ToString(),
            Model = platformInfo.Model,
            Manufacturer = platformInfo.Manufacturer,
            DeviceName = deviceInfo.Name,
            OsVersion = $"{deviceInfo.Platform} {deviceInfo.VersionString}",
            AppVersion = AppInfo.Current.VersionString,
            AppBuild = AppInfo.Current.BuildString,
            BundleId = AppInfo.Current.PackageName,
            Language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
            TimeZone = TimeZoneInfo.Local.Id,
            Locale = System.Globalization.CultureInfo.CurrentCulture.Name,
            ScreenResolution = GetScreenResolution(),
            ScreenDensity = DeviceDisplay.MainDisplayInfo.Density,
            DeviceType = deviceInfo.Idiom.ToString(),
            IsEmulator = platformInfo.IsEmulator,
            CapturedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    private static PlatformType GetPlatform() =>
        Microsoft.Maui.Devices.DeviceInfo.Current.Platform == DevicePlatform.Android ? PlatformType.Android
        : Microsoft.Maui.Devices.DeviceInfo.Current.Platform == DevicePlatform.iOS ? PlatformType.iOS
        : Microsoft.Maui.Devices.DeviceInfo.Current.Platform == DevicePlatform.WinUI ? PlatformType.Windows
        : Microsoft.Maui.Devices.DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst ? PlatformType.MacCatalyst
        : PlatformType.Unknown;

    private static string GetScreenResolution()
    {
        var display = DeviceDisplay.MainDisplayInfo;
        return $"{display.Width}x{display.Height}";
    }

    /// <summary>Platform specific model/manufacturer/emulator details.</summary>
    private partial PlatformDeviceDetails GetPlatformDetails();
}

/// <summary>Container for the platform specific slice of <see cref="DeviceInstallation"/>.</summary>
public readonly record struct PlatformDeviceDetails(string Model, string Manufacturer, bool IsEmulator);

