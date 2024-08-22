using TransmissionManager.Api.Database.Abstractions;
using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Scheduling.Services;

namespace TransmissionManager.Api.Composite.Services;

public sealed class SchedulableTorrentService(TorrentService torrentService, TorrentSchedulerService schedulerService)
    : ITorrentService
{
    public Task<Torrent[]> FindPageAsync(TorrentPageDescriptor dto)
    {
        return torrentService.FindPageAsync(dto);
    }

    public Task<Torrent?> FindOneByIdAsync(long id)
    {
        return torrentService.FindOneByIdAsync(id);
    }

    public async Task<long> AddOneAsync(TorrentAddDto dto)
    {
        var torrentId = await torrentService.AddOneAsync(dto).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(dto.Cron))
            schedulerService.ScheduleTorrentUpdates(torrentId, dto.Cron);

        return torrentId;
    }

    public async Task<bool> TryUpdateOneByIdAsync(long id, TorrentUpdateDto dto)
    {
        schedulerService.TryUnscheduleTorrentUpdates(id);
        var result = await torrentService.TryUpdateOneByIdAsync(id, dto).ConfigureAwait(false);
        if (result && !string.IsNullOrEmpty(dto.Cron))
            schedulerService.ScheduleTorrentUpdates(id, dto.Cron);

        return result;
    }

    public Task<bool> TryDeleteOneByIdAsync(long id)
    {
        schedulerService.TryUnscheduleTorrentUpdates(id);
        return torrentService.TryDeleteOneByIdAsync(id);
    }
}
