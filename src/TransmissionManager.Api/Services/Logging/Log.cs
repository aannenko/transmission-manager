using TransmissionManager.Api.Common.Dto.Transmission;

namespace TransmissionManager.Api.Services.Logging;

internal sealed partial class Log<T>(ILogger<T> logger)
{
    // Startup

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting application {AssemblyFullName}")]
    public partial void StartingApplication(string? assemblyFullName);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Start time: {StartTime:o}")]
    public partial void ApplicationStartTime(DateTime startTime);

    // Torrent scheduling

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Refreshing a torrent with id {TorrentId} on schedule.")]
    public partial void ScheduledRefreshStarted(long torrentId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Scheduled refresh of the torrent with id {TorrentId} succeeded. Transmission response: {TransmissionResult}.")]
    public partial void ScheduledRefreshSucceeded(long torrentId, TransmissionAddResult? transmissionResult);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Scheduled refresh of the torrent with id {TorrentId} failed: '{Error}'. Transmission response: {TransmissionResult}.")]
    public partial void ScheduledRefreshFailed(long torrentId, string error, TransmissionAddResult? transmissionResult);
}
