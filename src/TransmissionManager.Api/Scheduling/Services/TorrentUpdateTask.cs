using Coravel.Invocable;
using TransmissionManager.Api.Composite.Services;
using TransmissionManager.Api.Database.Services;

namespace TransmissionManager.Api.Scheduling.Services;

public sealed class TorrentUpdateTask(CompositeService<TorrentService> compositeService, long torrentId)
    : IInvocable, ICancellableInvocable
{
    public CancellationToken CancellationToken { get; set; }

    public Task Invoke()
    {
        return compositeService.TryRefreshTorrentAsync(torrentId, CancellationToken);
    }
}
