namespace PushNotification.App.Models;

/// <summary>
/// State of the OS level push notification permission for this app.
/// </summary>
public enum NotificationPermissionStatus
{
    /// <summary>Permission has never been requested from the user.</summary>
    NotRequested = 0,

    /// <summary>The user granted permission.</summary>
    Granted = 1,

    /// <summary>The user denied permission.</summary>
    Denied = 2,

    /// <summary>Permission is granted but notifications are suppressed at the OS level.</summary>
    Suppressed = 3,

    /// <summary>The platform could not determine the state.</summary>
    Unknown = 4,
}
