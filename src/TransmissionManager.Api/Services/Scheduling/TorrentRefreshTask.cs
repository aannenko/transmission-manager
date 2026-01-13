using Coravel.Invocable;
using TransmissionManager.Api.Actions.Torrents.RefreshById;
using TransmissionManager.Api.Services.Logging;

namespace TransmissionManager.Api.Services.Scheduling;

#pragma warning disable CA1812 // Uninstantiated class - this class gets instantiated by Coravel at run time
internal sealed partial class TorrentRefreshTask(
    Log<TorrentRefreshTask> log,
    RefreshTorrentByIdHandler refreshHandler,
    long torrentId) : IInvocable, ICancellableInvocable
#pragma warning restore CA1812 // Uninstantiated class
{
    public CancellationToken CancellationToken { get; set; }

    public async Task Invoke()
    {
        log.ScheduledRefreshStarted(torrentId);

        var (_, _, transmissionResult, error) = await refreshHandler
            .RefreshTorrentByIdAsync(torrentId, CancellationToken)
            .ConfigureAwait(false);

        if (error is null)
            log.ScheduledRefreshSucceeded(torrentId, transmissionResult);
        else
            log.ScheduledRefreshFailed(torrentId, error, transmissionResult);
    }
}
