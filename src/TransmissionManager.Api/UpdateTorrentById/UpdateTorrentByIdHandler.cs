using TransmissionManager.Api.Common.Scheduling;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.UpdateTorrentById;

public sealed class UpdateTorrentByIdHandler(TorrentCommandService commandService, TorrentSchedulerService scheduler)
{
    public async Task<bool> TryUpdateTorrentByIdAsync(
        long id,
        TorrentUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        scheduler.TryUnscheduleTorrentRefresh(id);
        var result = await commandService.TryUpdateOneByIdAsync(id, dto, cancellationToken).ConfigureAwait(false);
        if (result && !string.IsNullOrEmpty(dto.Cron))
            scheduler.ScheduleTorrentRefresh(id, dto.Cron);

        return result;
    }
}
