using Foundation;
using MyTestApp.Dto.Notifications;
using MyTestApp.Dto.Notifications.Service;

namespace TestApp.My.Mobile.Platforms.iOS
{
    /// <summary>
    /// Maps an APNs payload (<see cref="NSDictionary"/>) onto the platform agnostic
    /// <see cref="PushMessageDto"/> used by the view model.
    /// </summary>
    internal static class PushMessageFactory
    {
        public static PushMessageDto Build(NSDictionary? userInfo)
        {
            var data = userInfo?[PushNotificationFieldTypeDto.Data] as NSDictionary;

            string Get(string key) => data?[new NSString(key)]?.ToString() ?? string.Empty;

            return new PushMessageDto
            {
                Title = Get(PushNotificationFieldTypeDto.Title),
                Body = Get(PushNotificationFieldTypeDto.Body),
                Category = Get(PushNotificationFieldTypeDto.Category),
                JsonPayload = Get(PushNotificationFieldTypeDto.JsonPayload),
                ExternalDetailsUrl = Get(PushNotificationFieldTypeDto.ExternalDetailsUrl),
                CreatedAtUtc = DateTime.TryParse(Get(PushNotificationFieldTypeDto.CreatedAt), out var createdAt)
                    ? createdAt.ToUniversalTime()
                    : DateTime.UtcNow
            };
        }
    }
}
