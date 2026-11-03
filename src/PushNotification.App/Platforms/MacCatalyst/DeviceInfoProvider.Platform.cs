namespace PushNotification.App.Services;

/// <summary>
/// Mac Catalyst specific slice of the device snapshot.
/// </summary>
public sealed partial class DeviceInfoProvider
{
    private partial PlatformDeviceDetails GetPlatformDetails()
    {
        var deviceInfo = Microsoft.Maui.Devices.DeviceInfo.Current;

        return new PlatformDeviceDetails(
            Model: deviceInfo.Model ?? string.Empty,
            Manufacturer: "Apple",
            IsEmulator: false);
    }
}
