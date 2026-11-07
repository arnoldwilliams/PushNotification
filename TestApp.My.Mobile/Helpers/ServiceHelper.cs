namespace TestApp.My.Mobile.Helpers
{
    /// <summary>
    /// Resolves services from the platform application's service provider.
    /// Used by code that cannot receive constructor injection (activities, services, listeners).
    /// </summary>
    public static class ServiceHelper
    {
        public static IServiceProvider? Current => IPlatformApplication.Current?.Services;

        public static T? GetService<T>() where T : class => Current?.GetService<T>();
    }
}
