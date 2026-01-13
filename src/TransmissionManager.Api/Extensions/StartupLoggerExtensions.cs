using TransmissionManager.Api.Services.Logging;

internal static partial class StartupLoggerExtensions
{
    public static void LogStartup<T>(this Log<T> log)
    {
        log.StartingApplication(typeof(Program).Assembly.FullName);
        log.ApplicationStartTime(DateTime.Now);
    }
}
