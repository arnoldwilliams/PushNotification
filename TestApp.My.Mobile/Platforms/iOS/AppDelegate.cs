using Foundation;
using TestApp.My.Mobile.Helpers;
using TestApp.My.Mobile.Interfaces;
using TestApp.My.Mobile.Platforms.iOS;
using UIKit;
using UserNotifications;

namespace TestApp.My.Mobile
{
    /// <summary>
    /// iOS application delegate.
    /// <para>
    /// It only forwards the APNs callbacks (token registration, remote notification, failure) to
    /// <see cref="IPushNotificationEventSink"/>, which the view model is subscribed to. No
    /// registration work happens here, which mirrors the Android change where nothing registers in
    /// <c>MainActivity.OnCreate</c>.
    /// </para>
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
        {
            UNUserNotificationCenter.Current.Delegate = new PushNotificationDelegate();

            return base.FinishedLaunching(application, launchOptions);
        }

        /// <summary>
        /// Called once APNs has issued the device token. The token is handed to
        /// <see cref="iOSPushNotificationService"/>, which raises <c>TokenRefreshed</c> for the
        /// view model; registration itself stays in the view model.
        /// </summary>
        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            ServiceHelper.GetService<iOSPushNotificationService>()?
                .RegisterDeviceToken(deviceToken);
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            ServiceHelper.GetService<iOSPushNotificationService>()?
                .RegisterFailed(error);
        }

        public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            userInfo?.ShowRemoteNotification();

            var message = PushMessageFactory.Build(userInfo);

            ServiceHelper.GetService<IPushNotificationEventSink>()?
                .RaiseNotificationReceived(message);
        }
    }
}
