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

    public void UpdateOneById(long id, TorrentUpdateDto dto)
    {
        torrentService.UpdateOneById(id, dto);
        schedulerService.TryUnscheduleTorrentUpdates(id);
        if (!string.IsNullOrEmpty(dto.Cron))
            schedulerService.ScheduleTorrentUpdates(id, dto.Cron);
    }

    public void DeleteOneById(long id)
    {
        torrentService.DeleteOneById(id);
        schedulerService.TryUnscheduleTorrentUpdates(id);
    }
}
