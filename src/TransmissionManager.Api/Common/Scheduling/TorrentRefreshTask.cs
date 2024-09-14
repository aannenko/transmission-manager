using Coravel.Invocable;
using TransmissionManager.Api.RefreshTorrentById.Handlers;

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
        logger.LogInformation("Refreshing torrent with id '{torrentId}' on schedule.", torrentId);
        var (_, errorMessage) = await refreshHandler
            .RefreshTorrentByIdAsync(torrentId, CancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            logger.LogError(
                "Could not refresh the torrent with id '{torrentId}' on schedule: '{errorMessage}'.",
                torrentId,
                errorMessage);
        }
    }
}
