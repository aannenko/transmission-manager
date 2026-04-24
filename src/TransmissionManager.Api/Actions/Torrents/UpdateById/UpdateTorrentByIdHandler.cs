using TransmissionManager.Api.Services.Scheduling;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.Torrents.UpdateById;

internal sealed class UpdateTorrentByIdHandler(TorrentService torrentService, TorrentSchedulerService scheduler)
{
    public async Task<TorrentUpdateResult> TryUpdateTorrentByIdAsync(
        long id,
        TorrentUpdateDto dto,
        uint? expectedVersion,
        CancellationToken cancellationToken)
    {
        _ = scheduler.TryUnscheduleTorrentRefresh(id);
        var result = await torrentService
            .TryUpdateOneByIdAsync(id, dto, expectedVersion, cancellationToken)
            .ConfigureAwait(false);

        if (result is TorrentUpdateResult.Updated && !string.IsNullOrEmpty(dto.Cron))
            scheduler.ScheduleTorrentRefresh(id, dto.Cron);

        return result;
    }
}
