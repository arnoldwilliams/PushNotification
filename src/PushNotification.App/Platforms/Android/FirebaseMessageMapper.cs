using Android.Content;
using Firebase.Messaging;
using PushNotification.App.Models;

namespace PushNotification.App.Services.Notifications;

/// <summary>
/// Converts FCM payloads into the platform neutral <see cref="PushMessage"/>.
/// Shared by the messaging service (receive) and the main activity (notification tap).
/// </summary>
internal static class FirebaseMessageMapper
{
    public static PushMessage Map(RemoteMessage message, bool wasForeground)
    {
        var data = new Dictionary<string, string>();
        if (message.Data is not null)
        {
            foreach (var pair in message.Data)
            {
                data[pair.Key] = pair.Value;
            }
        }

        return new PushMessage
        {
            MessageId = message.MessageId,
            Title = message.GetNotification()?.Title ?? Get(data, "title"),
            Body = message.GetNotification()?.Body ?? Get(data, "body"),
            Topic = message.From,
            Data = data,
            WasForeground = wasForeground,
        };
    }

    /// <summary>Maps the intent extras that accompany a notification tap.</summary>
    public static PushMessage MapIntent(Intent intent)
    {
        var data = new Dictionary<string, string>();
        var extras = intent.Extras;
        var keys = extras?.KeySet();
        if (keys is not null)
        {
            foreach (var key in keys)
            {
                if (key is null)
                {
                    continue;
                }

                var value = extras!.Get(key)?.ToString();
                if (value is not null)
                {
                    data[key] = value;
                }
            }
        }

        return new PushMessage
        {
            MessageId = Get(data, "google.message_id"),
            Title = Get(data, "gcm.n.title") ?? Get(data, "title"),
            Body = Get(data, "gcm.n.body") ?? Get(data, "body"),
            Topic = Get(data, "from"),
            Data = data,
            WasForeground = false,
        };
    }

    private static string? Get(IReadOnlyDictionary<string, string> data, string key) =>
        data.TryGetValue(key, out var value) ? value : null;
}
