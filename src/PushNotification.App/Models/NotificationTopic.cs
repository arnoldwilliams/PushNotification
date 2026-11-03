namespace PushNotification.App.Models;

/// <summary>
/// Every notification topic the app can subscribe an installation to.
/// The string value is the canonical topic name used by FCM/APNs.
/// </summary>
public enum NotificationTopic
{
    /// <summary>General product announcements.</summary>
    Announcements,

    /// <summary>Account, security and billing alerts.</summary>
    AccountAlerts,

    /// <summary>Transactional order/shipment updates.</summary>
    OrderUpdates,

    /// <summary>Promotional offers and discounts.</summary>
    Promotions,

    /// <summary>Tips, guides and feature highlights.</summary>
    Tips,

    /// <summary>Weekly digest summary.</summary>
    WeeklyDigest,
}
