using Android.OS;
namespace PushNotification.App.Services;

/// <summary>
/// Android specific slice of the device snapshot.
/// </summary>
public sealed partial class DeviceInfoProvider
{
    private partial PlatformDeviceDetails GetPlatformDetails()
    {
        var isEmulator = Build.Fingerprint is not null
            && (Build.Fingerprint.Contains("generic", StringComparison.OrdinalIgnoreCase)
                || Build.Fingerprint.Contains("emulator", StringComparison.OrdinalIgnoreCase)
                || Build.Fingerprint.Contains("sdk", StringComparison.OrdinalIgnoreCase));

        return new PlatformDeviceDetails(
            Model: Build.Model ?? string.Empty,
            Manufacturer: Build.Manufacturer ?? string.Empty,
            IsEmulator: isEmulator);
    }
}
