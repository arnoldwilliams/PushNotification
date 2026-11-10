using Microsoft.Extensions.Logging;
using TestApp.My.Mobile.Interfaces;
using TestApp.My.Mobile.LocalServices.Interfaces;
using TestApp.My.Mobile.LocalServices.Services;
using TestApp.My.Mobile.Services;
using TestApp.My.Mobile.ViewModels;
using TestApp.My.Mobile.Views;

namespace TestApp.My.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    // Add your fonts here, e.g.
                    // fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                })
#if IOS
                .ConfigureMauiHandlers(handlers =>
                {
                    // Replace the default iOS Shell renderer with one that can show a native tab bar
                    // badge, which the push notification view model drives through UpdateBadgeMessage.
                    handlers.AddHandler<Shell, Platforms.iOS.BadgeShellRenderer>();
                })
#endif
                ;

            RegisterSharedServices(builder.Services);

#if ANDROID
            RegisterAndroidServices(builder.Services);
#elif IOS
            RegisterIosServices(builder.Services);
#endif

            var app = builder.Build();

            LoggerFactoryHolder.Factory = app.Services.GetRequiredService<ILoggerFactory>();

            return app;
        }

        private static void RegisterSharedServices(IServiceCollection services)
        {
            services.AddSingleton(new HttpClient());

            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<ISecureStorageService, SecureStorageService>();
            services.AddSingleton<IHttpCommunication, HttpCommunication>();
            services.AddSingleton<INotificationRegistrationService, NotificationRegistrationService>();

            services.AddSingleton<PushNotificationViewModel>();
            services.AddSingleton<AppShell>();
            services.AddSingleton<MainPage>();
        }

#if ANDROID
        private static void RegisterAndroidServices(IServiceCollection services)
        {
            // One instance implements both interfaces; the view model subscribes to the
            // IPushNotificationService events while the FCM service raises them through
            // IPushNotificationEventSink. They must resolve to the same object.
            services.AddSingleton<Platforms.Android.AndroidPushNotificationService>();
            services.AddSingleton<IPushNotificationService>(sp => sp.GetRequiredService<Platforms.Android.AndroidPushNotificationService>());
            services.AddSingleton<IPushNotificationEventSink>(sp => sp.GetRequiredService<Platforms.Android.AndroidPushNotificationService>());

            services.AddSingleton<IDeviceInstallationService, Platforms.Android.AndroidDeviceInstallationService>();
        }
#elif IOS
        private static void RegisterIosServices(IServiceCollection services)
        {
            services.AddSingleton<Platforms.iOS.iOSPushNotificationService>();
            services.AddSingleton<IPushNotificationService>(sp => sp.GetRequiredService<Platforms.iOS.iOSPushNotificationService>());
            services.AddSingleton<IPushNotificationEventSink>(sp => sp.GetRequiredService<Platforms.iOS.iOSPushNotificationService>());

            services.AddSingleton<IDeviceInstallationService, Platforms.iOS.iOSDeviceInstallationService>();
        }
#endif
    }
}
