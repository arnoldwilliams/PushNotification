using Foundation;
using PushNotification.App.Services;
using UIKit;

namespace PushNotification.App.Services;

/// <summary>
/// iOS specific slice of the device snapshot.
/// </summary>
public sealed partial class DeviceInfoProvider
{
    private partial PlatformDeviceDetails GetPlatformDetails()
    {
        // hw.machine gives the hardware identifier, e.g. "iPhone15,2".
        var model = Sysctl("hw.machine") ?? UIDevice.CurrentDevice.Model;
        var emulator = model.StartsWith("x86_64", StringComparison.OrdinalIgnoreCase)
            || model.StartsWith("i386", StringComparison.OrdinalIgnoreCase)
            || model.StartsWith("arm64", StringComparison.OrdinalIgnoreCase);

        return new PlatformDeviceDetails(
            Model: model,
            Manufacturer: "Apple",
            IsEmulator: emulator);
    }

    private static string? Sysctl(string name)
    {
        var handle = System.Runtime.InteropServices.Marshal.AllocHGlobal(128);
        try
        {
            nuint size = 128;
            if (sysctlbyname(name, handle, ref size, IntPtr.Zero, 0) != 0)
            {
                return null;
            }

            return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(handle);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(handle);
        }
    }

    [System.Runtime.InteropServices.DllImport("libc")]
    private static extern int sysctlbyname(
        string name,
        IntPtr oldp,
        ref nuint oldlenp,
        IntPtr newp,
        nuint newlen);
}
