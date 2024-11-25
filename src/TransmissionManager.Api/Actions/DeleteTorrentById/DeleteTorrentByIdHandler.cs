using TransmissionManager.Api.Common.Scheduling;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.DeleteTorrentById;

internal sealed class DeleteTorrentByIdHandler(TorrentService torrentService, TorrentSchedulerService scheduler)
{
    public Task<bool> TryDeleteTorrentByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        scheduler.TryUnscheduleTorrentRefresh(id);
        return torrentService.TryDeleteOneByIdAsync(id, cancellationToken);
    }
}
