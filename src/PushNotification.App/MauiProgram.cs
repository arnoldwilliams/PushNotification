using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PushNotification.App.Configuration;
using PushNotification.App.Services;
using PushNotification.App.Services.Api;
using PushNotification.App.Services.Notifications;
using PushNotification.App.Services.Preferences;
using PushNotification.App.ViewModels;
using PushNotification.App.Views;

namespace PushNotification.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Configuration: appsettings.json is embedded in the assembly; environment
        // variables prefixed with PUSH_ override individual values at runtime.
        var assembly = typeof(MauiProgram).Assembly;
        var settingsResource = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase));

        if (settingsResource is not null)
        {
            using var stream = assembly.GetManifestResourceStream(settingsResource);
            if (stream is not null)
            {
                builder.Configuration.AddJsonStream(stream);
            }
        }

        builder.Configuration.AddEnvironmentVariables("PUSH_");
        builder.Services.Configure<PushOptions>(builder.Configuration.GetSection(PushOptions.SectionName));

        RegisterPlatformServices(builder.Services);
        RegisterSharedServices(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void RegisterSharedServices(IServiceCollection services)
    {
        services.AddSingleton(Preferences.Default);
        services.AddSingleton<IDeviceInfoProvider, DeviceInfoProvider>();
        services.AddSingleton<IDeviceRegistrationService, DummyDeviceRegistrationService>();
        services.AddSingleton<INotificationPreferencesService, NotificationPreferencesService>();
        services.AddSingleton<IPushRegistrationCoordinator, PushRegistrationCoordinator>();

        services.AddSingleton<AppShell>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<HomePage>();
        services.AddSingleton<NotificationPreferencesViewModel>();
        services.AddSingleton<NotificationPreferencesPage>();
        services.AddSingleton<DeviceInfoViewModel>();
        services.AddSingleton<DeviceInfoPage>();
    }

    /// <summary>
    /// Registers the push provider and permission service for the current target.
    /// Firebase Cloud Messaging on Android, Apple Push Notification service on iOS.
    /// </summary>
    private static void RegisterPlatformServices(IServiceCollection services)
    {
#if ANDROID
        services.AddSingleton<IPushNotificationService, AndroidPushNotificationService>();
        services.AddSingleton<INotificationPermissionService, AndroidNotificationPermissionService>();
#elif IOS
        services.AddSingleton<IPushNotificationService, ApplePushNotificationService>();
        services.AddSingleton<INotificationPermissionService, AppleNotificationPermissionService>();
#else
        services.AddSingleton<IPushNotificationService, UnsupportedPushNotificationService>();
        services.AddSingleton<INotificationPermissionService, AlwaysGrantedNotificationPermissionService>();
#endif
    }
}
