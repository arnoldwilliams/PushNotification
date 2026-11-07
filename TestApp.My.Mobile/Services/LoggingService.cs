using Microsoft.Extensions.Logging;

namespace TestApp.My.Mobile.Services
{
    /// <summary>
    /// Very small logging facade. Keeps call sites simple and consistent
    /// (e.g. <c>LoggingService&lt;NotificationRegistrationService&gt;.LogError(ex)</c>).
    /// </summary>
    public static class LoggingService<T>
    {
        private static readonly Lazy<ILogger> Logger =
            new(() => LoggerFactoryHolder.Factory.CreateLogger(typeof(T).FullName ?? typeof(T).Name));

        public static void LogInformation(string message) => Logger.Value.LogInformation("{Message}", message);

        public static void LogWarning(string message) => Logger.Value.LogWarning("{Message}", message);

        public static void LogError(Exception? exception, string? message = null)
            => Logger.Value.LogError(exception, "{Message}", message ?? exception?.Message);
    }

    /// <summary>
    /// Holds the single <see cref="ILoggerFactory"/> used by the app.
    /// </summary>
    public static class LoggerFactoryHolder
    {
        public static ILoggerFactory Factory { get; set; } = LoggerFactory.Create(_ => { });
    }
}
