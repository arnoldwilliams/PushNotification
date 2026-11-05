namespace PushNotification.App.Models;

/// <summary>
/// Outcome of the guided permission flow that starts with the in-app soft prompt.
/// </summary>
public enum PushPermissionOutcome
{
    /// <summary>The flow could not start, for example no visible page was available.</summary>
    Unavailable = 0,

    /// <summary>The user declined the soft prompt, or dismissed it without deciding.</summary>
    SoftPromptDeclined = 1,

    /// <summary>The operating system permission dialog reported a denial.</summary>
    OsPermissionDenied = 2,

    /// <summary>The user allowed the operating system permission; preferences may be shown.</summary>
    OsPermissionGranted = 3,
}
