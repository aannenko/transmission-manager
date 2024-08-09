using Coravel.Invocable;
using TransmissionManager.Api.Composite.Services;
using TransmissionManager.Api.Database.Services;

namespace TransmissionManager.Api.Scheduling.Services;

public sealed class TorrentRefreshTask(
    ILogger<TorrentRefreshTask> logger,
    CompositeService<TorrentService> compositeService,
    long torrentId)
    : IInvocable, ICancellableInvocable
{
    public CancellationToken CancellationToken { get; set; }

    public async Task Invoke()
    {
        var error = await compositeService.RefreshTorrentAsync(torrentId, CancellationToken);
        if (!string.IsNullOrEmpty(error))
        {
            logger.LogError("Could not refresh the torrent with id '{id}' on schedule: '{error}'.", torrentId, error);
        }
    }
}
