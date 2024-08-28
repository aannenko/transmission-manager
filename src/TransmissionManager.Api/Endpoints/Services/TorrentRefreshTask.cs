using Coravel.Invocable;

namespace TransmissionManager.Api.Endpoints.Services;

public sealed class TorrentRefreshTask(
    ILogger<TorrentRefreshTask> logger,
    CompositeRefreshTorrentService compositeService,
    long torrentId)
    : IInvocable, ICancellableInvocable
{
    public CancellationToken CancellationToken { get; set; }

    public async Task Invoke()
    {
        var (_, errorMessage) = await compositeService
            .RefreshTorrentAsync(torrentId, CancellationToken)
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
