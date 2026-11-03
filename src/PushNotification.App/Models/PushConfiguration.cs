namespace PushNotification.App.Models;

/// <summary>
/// Configuration required to initialize a push provider (FCM or APNs).
/// Populated from <see cref="Configuration.PushOptions"/> at startup.
/// </summary>
public sealed class PushConfiguration
{
    /// <summary>Android only: the FCM sender id from google-services.json.</summary>
    public string? FcmSenderId { get; set; }

    /// <summary>Android only: default notification channel id created at startup.</summary>
    public string FcmNotificationChannelId { get; set; } = "general";

    /// <summary>Android only: user visible name of the default channel.</summary>
    public string FcmNotificationChannelName { get; set; } = "General";

    /// <summary>iOS only: APNs environment, "development" or "production".</summary>
    public string ApnsEnvironment { get; set; } = "development";

    /// <summary>iOS only: bundle identifier used for the APNs token topic.</summary>
    public string ApnsTopic { get; set; } = string.Empty;

    /// <summary>Base URL of the backend that stores installations.</summary>
    public string ApiBaseUrl { get; set; } = "https://api.example.com";
}
