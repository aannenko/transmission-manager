using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Common.Services;

public sealed class SchedulableTorrentCommandService(
    TorrentCommandService commandService,
    TorrentSchedulerService scheduler)
{
    public async Task<long> AddOneAsync(TorrentAddDto dto, CancellationToken cancellationToken = default)
    {
        var torrentId = await commandService.AddOneAsync(dto, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(dto.Cron))
            scheduler.ScheduleTorrentRefresh(torrentId, dto.Cron);

        return torrentId;
    }

    public async Task<bool> TryUpdateOneByIdAsync(
        long id,
        TorrentUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        scheduler.TryUnscheduleTorrentRefresh(id);
        var result = await commandService.TryUpdateOneByIdAsync(id, dto, cancellationToken).ConfigureAwait(false);
        if (result && !string.IsNullOrEmpty(dto.Cron))
            scheduler.ScheduleTorrentRefresh(id, dto.Cron);

        return result;
    }

    public Task<bool> TryDeleteOneByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        scheduler.TryUnscheduleTorrentRefresh(id);
        return commandService.TryDeleteOneByIdAsync(id, cancellationToken);
    }
}
