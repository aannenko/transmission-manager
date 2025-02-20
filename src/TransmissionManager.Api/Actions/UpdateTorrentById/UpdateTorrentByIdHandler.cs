using TransmissionManager.Api.Common.Scheduling;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.UpdateTorrentById;

internal sealed class UpdateTorrentByIdHandler(TorrentService torrentService, TorrentSchedulerService scheduler)
{
    public async Task<bool> TryUpdateTorrentByIdAsync(
        long id,
        TorrentUpdateDto dto,
        CancellationToken cancellationToken)
    {
        scheduler.TryUnscheduleTorrentRefresh(id);
        var result = await torrentService.TryUpdateOneByIdAsync(id, dto, cancellationToken).ConfigureAwait(false);
        if (result && !string.IsNullOrEmpty(dto.Cron))
            scheduler.ScheduleTorrentRefresh(id, dto.Cron);

        return result;
    }
}
