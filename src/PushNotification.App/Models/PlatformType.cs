namespace PushNotification.App.Models;

/// <summary>
/// The mobile platform an installation is running on.
/// </summary>
public enum PlatformType
{
    Unknown = 0,
    Android = 1,
    iOS = 2,
    Windows = 3,
    MacCatalyst = 4,
}
