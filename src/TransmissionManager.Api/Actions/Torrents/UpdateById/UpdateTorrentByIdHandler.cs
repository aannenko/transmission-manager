using TransmissionManager.Api.Services.Scheduling;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.Torrents.UpdateById;

internal sealed class UpdateTorrentByIdHandler(TorrentService torrentService, TorrentSchedulerService scheduler)
{
    public async Task<bool> TryUpdateTorrentByIdAsync(
        long id,
        TorrentUpdateDto dto,
        CancellationToken cancellationToken)
    {
        scheduler.TryUnscheduleTorrentRefresh(id);
        var isUpdated = await torrentService.TryUpdateOneByIdAsync(id, dto, cancellationToken).ConfigureAwait(false);
        if (isUpdated && !string.IsNullOrEmpty(dto.Cron))
            scheduler.ScheduleTorrentRefresh(id, dto.Cron);

        return isUpdated;
    }
}
