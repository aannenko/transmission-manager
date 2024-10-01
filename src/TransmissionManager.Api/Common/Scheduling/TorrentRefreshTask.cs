using Coravel.Invocable;
using TransmissionManager.Api.RefreshTorrentById;

namespace TransmissionManager.Api.Common.Scheduling;

public sealed class TorrentRefreshTask(
    ILogger<TorrentRefreshTask> logger,
    RefreshTorrentByIdHandler refreshHandler,
    long torrentId)
    : IInvocable, ICancellableInvocable
{
    public CancellationToken CancellationToken { get; set; }

    public async Task Invoke()
    {
        logger.LogInformation("Refreshing a torrent with id {torrentId} on schedule.", torrentId);
        var (_, transmissionResult, error) = await refreshHandler
            .RefreshTorrentByIdAsync(torrentId, CancellationToken)
            .ConfigureAwait(false);

        if (error is null)
        {
            const string message = "Scheduled refresh of the torrent with id {torrentId} succeeded. " +
                "Transmission response: {transmissionResult}.";

            logger.LogInformation(message, torrentId, transmissionResult);
        }
        else
        {
            const string message = "Scheduled refresh of the torrent with id {torrentId} failed: '{error}'. " +
                "Transmission response: {transmissionResult}.";

            logger.LogWarning(message, torrentId, error, transmissionResult);
        }
    }
}
