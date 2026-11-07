namespace TestApp.My.Mobile.LocalServices.Interfaces
{
    /// <summary>
    /// Application configuration values.
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>Base url of the push notification registration API.</summary>
        string? BasePushNotificationsUrl { get; }
    }
}
