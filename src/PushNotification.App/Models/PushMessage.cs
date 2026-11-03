namespace PushNotification.App.Models;

/// <summary>
/// A push message received while the app was running, normalized across FCM and APNs.
/// </summary>
public sealed class PushMessage
{
    /// <summary>Unique message id assigned by the push service, when available.</summary>
    public string? MessageId { get; set; }

    /// <summary>Notification title.</summary>
    public string? Title { get; set; }

    /// <summary>Notification body.</summary>
    public string? Body { get; set; }

    /// <summary>Topic the message was delivered on, when the push service reports it.</summary>
    public string? Topic { get; set; }

    /// <summary>Custom key/value data attached to the message.</summary>
    public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();

    /// <summary>True when the message arrived while the app was in the foreground.</summary>
    public bool WasForeground { get; set; }

    /// <summary>UTC time the message was received by the app.</summary>
    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
