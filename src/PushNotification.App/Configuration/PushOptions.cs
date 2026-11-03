namespace PushNotification.App.Configuration;

/// <summary>
/// App configuration for push providers and the backend. Bind this from
/// appsettings.json, environment variables or a secrets store in a real app.
/// The defaults below are placeholders for the sample.
/// </summary>
public sealed class PushOptions
{
    public const string SectionName = "Push";

    /// <summary>Base URL of the installation/registration backend.</summary>
    public string ApiBaseUrl { get; set; } = "https://api.example.com";

    /// <summary>Android: FCM sender id (project number) from google-services.json.</summary>
    public string? FcmSenderId { get; set; }

    /// <summary>Android: notification channel id created at startup.</summary>
    public string FcmNotificationChannelId { get; set; } = "general";

    /// <summary>Android: user visible channel name.</summary>
    public string FcmNotificationChannelName { get; set; } = "General";

    /// <summary>iOS: APNs environment, "development" or "production".</summary>
    public string ApnsEnvironment { get; set; } = "development";

    /// <summary>iOS: APNs topic, normally the app bundle id.</summary>
    public string ApnsTopic { get; set; } = "com.companyname.pushnotification.app";
}
