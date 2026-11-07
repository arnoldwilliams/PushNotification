using TestApp.My.Mobile.LocalServices.Interfaces;

namespace TestApp.My.Mobile.LocalServices.Services
{
    /// <summary>
    /// Configuration values. Replace the constants with values from your own
    /// configuration provider (appsettings, remote config, key vault, ...).
    /// </summary>
    public sealed class ConfigurationService : IConfigurationService
    {
        public string? BasePushNotificationsUrl { get; } = "https://your-notification-api.azurewebsites.net/api";
    }
}
