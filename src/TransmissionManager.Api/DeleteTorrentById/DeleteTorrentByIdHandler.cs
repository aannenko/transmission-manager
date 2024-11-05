using TransmissionManager.Api.Shared.Scheduling;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.DeleteTorrentById;

public sealed class DeleteTorrentByIdHandler(TorrentCommandService commandService, TorrentSchedulerService scheduler)
{
    public Task<bool> TryDeleteTorrentByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        scheduler.TryUnscheduleTorrentRefresh(id);
        return commandService.TryDeleteOneByIdAsync(id, cancellationToken);
    }
}
