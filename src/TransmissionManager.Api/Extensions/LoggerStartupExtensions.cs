namespace TransmissionManager.Api.Extensions;

public static partial class LoggerStartupExtensions
{
    public static void LogStartup(this ILogger logger)
    {
        LogStartingApplication(logger, typeof(Program).Assembly.FullName);
        LogStartTime(logger, DateTime.Now.ToString());
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting application {AssemblyFullName}")]
    private static partial void LogStartingApplication(ILogger logger, string? assemblyFullName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Start time: {StartTime:o}")]
    private static partial void LogStartTime(ILogger logger, string startTime);
}
