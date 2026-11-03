using Foundation;
using PushNotification.App.Models;
using PushNotification.App.Services.Notifications;
using UIKit;
using UserNotifications;

namespace PushNotification.App;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        UNUserNotificationCenter.Current.Delegate = new AppUserNotificationCenterDelegate();
        return base.FinishedLaunching(application, launchOptions);
    }

    /// <summary>APNs issued (or refreshed) a device token.</summary>
    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        var token = deviceToken.Description?.Trim('<', '>').Replace(" ", string.Empty);
        if (!string.IsNullOrWhiteSpace(token))
        {
            ApplePushBridge.SetDeviceToken(token);
        }
    }

    /// <summary>APNs registration failed. Log the reason and continue without a token.</summary>
    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    {
        System.Diagnostics.Debug.WriteLine($"APNs registration failed: {error.LocalizedDescription}");
    }

    /// <summary>Silent/background data push arrived.</summary>
    [Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
    public void ReceivedRemoteNotification(
        UIApplication application,
        NSDictionary userInfo,
        Action<UIBackgroundFetchResult> completionHandler)
    {
        ApplePushBridge.RaiseMessageReceived(new PushMessage
        {
            WasForeground = UIApplication.SharedApplication.ApplicationState == UIApplicationState.Active,
            Data = userInfo.ToDictionary(),
        });

        completionHandler(UIBackgroundFetchResult.NewData);
    }
}

/// <summary>
/// Handles notification presentation and taps that arrive outside the app's own
/// <see cref="UNUserNotificationCenterDelegate"/>.
/// </summary>
public sealed class AppUserNotificationCenterDelegate : UNUserNotificationCenterDelegate
{
    public override void WillPresentNotification(
        UNUserNotificationCenter center,
        UNNotification notification,
        Action<UNNotificationPresentationOptions> completionHandler)
    {
        var content = notification.Request.Content;
        ApplePushBridge.RaiseMessageReceived(new PushMessage
        {
            MessageId = notification.Request.Identifier,
            Title = content.Title,
            Body = content.Body,
            WasForeground = true,
            Data = content.UserInfo.ToDictionary(),
        });

        completionHandler(UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.Sound);
    }

    public override void DidReceiveNotificationResponse(
        UNUserNotificationCenter center,
        UNNotificationResponse response,
        Action completionHandler)
    {
        var content = response.Notification.Request.Content;
        ApplePushBridge.RaiseNotificationTapped(new PushMessage
        {
            MessageId = response.Notification.Request.Identifier,
            Title = content.Title,
            Body = content.Body,
            WasForeground = false,
            Data = content.UserInfo.ToDictionary(),
        });

        completionHandler();
    }
}

internal static class NSDictionaryExtensions
{
    public static Dictionary<string, string> ToDictionary(this NSDictionary? dictionary)
    {
        var result = new Dictionary<string, string>();
        if (dictionary is null)
        {
            return result;
        }

        foreach (var key in dictionary.Keys)
        {
            var value = dictionary[key]?.ToString();
            if (value is not null)
            {
                result[key.ToString()] = value;
            }
        }

        return result;
    }
}
