using TransmissionManager.Api.Database.Abstractions;
using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Scheduling.Services;

namespace TransmissionManager.Api.Composite.Services;

public sealed class SchedulableTorrentService(TorrentService torrentService, TorrentSchedulerService schedulerService)
    : ITorrentService
{
    public Torrent[] FindPage(TorrentPageDescriptor dto)
    {
        return torrentService.FindPage(dto);
    }

    public Torrent? FindOneById(long id)
    {
        return torrentService.FindOneById(id);
    }

    public long AddOne(TorrentAddDto dto)
    {
        var torrentId = torrentService.AddOne(dto);
        if (!string.IsNullOrEmpty(dto.Cron))
            schedulerService.ScheduleTorrentUpdates(torrentId, dto.Cron);

        return torrentId;
    }

    public bool TryUpdateOneById(long id, TorrentUpdateDto dto)
    {
        var result = torrentService.TryUpdateOneById(id, dto);
        schedulerService.TryUnscheduleTorrentUpdates(id);
        if (!string.IsNullOrEmpty(dto.Cron))
            schedulerService.ScheduleTorrentUpdates(id, dto.Cron);

        return result;
    }

    public bool TryDeleteOneById(long id)
    {
        var result = torrentService.TryDeleteOneById(id);
        schedulerService.TryUnscheduleTorrentUpdates(id);
        return result;
    }
}
