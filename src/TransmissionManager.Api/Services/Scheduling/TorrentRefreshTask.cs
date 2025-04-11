using Coravel.Invocable;
using TransmissionManager.Api.Actions.Torrents.RefreshById;
using TransmissionManager.Api.Shared.Dto.Transmission;

namespace TransmissionManager.Api.Services.Scheduling;

#pragma warning disable CA1812 // Uninstantiated class - this class gets instantiated by Coravel at run time
internal sealed partial class TorrentRefreshTask(
    ILogger<TorrentRefreshTask> logger,
    RefreshTorrentByIdHandler refreshHandler,
    long torrentId) : IInvocable, ICancellableInvocable
#pragma warning restore CA1812 // Uninstantiated class
{
    public CancellationToken CancellationToken { get; set; }

    public async Task Invoke()
    {
        LogRefreshStarted(logger, torrentId);

        var (_, transmissionResult, error) = await refreshHandler
            .RefreshTorrentByIdAsync(torrentId, CancellationToken)
            .ConfigureAwait(false);

        if (error is null)
            LogRefreshSucceeded(logger, torrentId, transmissionResult);
        else
            LogRefreshFailed(logger, torrentId, error, transmissionResult);
    }

    private const string _refreshStarted = "Refreshing a torrent with id {TorrentId} on schedule.";

    private const string _refreshSucceeded = "Scheduled refresh of the torrent with id {TorrentId} succeeded. " +
        "Transmission response: {TransmissionResult}.";

    private const string _refreshFailed = "Scheduled refresh of the torrent with id {TorrentId} failed: '{Error}'. " +
        "Transmission response: {TransmissionResult}.";

    [LoggerMessage(Level = LogLevel.Information, Message = _refreshStarted)]
    private static partial void LogRefreshStarted(ILogger logger, long torrentId);

    [LoggerMessage(Level = LogLevel.Information, Message = _refreshSucceeded)]
    private static partial void LogRefreshSucceeded(
        ILogger logger,
        long torrentId,
        TransmissionAddResult? transmissionResult);

    [LoggerMessage(Level = LogLevel.Warning, Message = _refreshFailed)]
    private static partial void LogRefreshFailed(
        ILogger logger,
        long torrentId,
        string error,
        TransmissionAddResult? transmissionResult);
}
