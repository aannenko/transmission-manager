using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Scheduling.Services;

namespace TransmissionManager.Api.Composite.Services;

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
            schedulerService.ScheduleTorrentUpdates(torrentId, dto.Cron);

        return torrentId;
    }

    public async Task<bool> TryUpdateOneByIdAsync(
        long id,
        TorrentUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        schedulerService.TryUnscheduleTorrentUpdates(id);
        var result = await torrentService.TryUpdateOneByIdAsync(id, dto, cancellationToken).ConfigureAwait(false);
        if (result && !string.IsNullOrEmpty(dto.Cron))
            schedulerService.ScheduleTorrentUpdates(id, dto.Cron);

        return result;
    }

    public Task<bool> TryDeleteOneByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        schedulerService.TryUnscheduleTorrentUpdates(id);
        return torrentService.TryDeleteOneByIdAsync(id, cancellationToken);
    }
}
