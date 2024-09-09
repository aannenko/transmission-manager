using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Services;

public sealed class SchedulableTorrentService(TorrentService torrentService, TorrentSchedulerService schedulerService)
{
    public Task<Torrent[]> FindPageAsync(
        PageDescriptor page,
        TorrentFilter filter = default,
        CancellationToken cancellationToken = default)
    {
        return torrentService.FindPageAsync(page, filter, cancellationToken);
    }

    public Task<Torrent?> FindOneByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return torrentService.FindOneByIdAsync(id, cancellationToken);
    }

    public async Task<long> AddOneAsync(TorrentAddDto dto, CancellationToken cancellationToken = default)
    {
        var torrentId = await torrentService.AddOneAsync(dto, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(dto.Cron))
            schedulerService.ScheduleTorrentRefresh(torrentId, dto.Cron);

        return torrentId;
    }

    public async Task<bool> TryUpdateOneByIdAsync(
        long id,
        TorrentUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        schedulerService.TryUnscheduleTorrentRefresh(id);
        var result = await torrentService.TryUpdateOneByIdAsync(id, dto, cancellationToken).ConfigureAwait(false);
        if (result && !string.IsNullOrEmpty(dto.Cron))
            schedulerService.ScheduleTorrentRefresh(id, dto.Cron);

        return result;
    }

    public Task<bool> TryDeleteOneByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        schedulerService.TryUnscheduleTorrentRefresh(id);
        return torrentService.TryDeleteOneByIdAsync(id, cancellationToken);
    }
}
