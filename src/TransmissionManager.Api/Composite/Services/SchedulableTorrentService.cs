using TransmissionManager.Api.Database.Abstractions;
using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Scheduling.Services;

namespace TransmissionManager.Api.Composite.Services;

public sealed class SchedulableTorrentService(TorrentService torrentService, TorrentSchedulerService schedulerService)
    : ITorrentService
{
    public Torrent[] FindPage(TorrentGetPageDescriptor dto)
    {
        return torrentService.FindPage(dto);
    }

    public Torrent? FindOneById(long id)
    {
        return torrentService.FindOneById(id);
    }

    public long AddOne(TorrentAddDto dto)
    {
        var id = torrentService.AddOne(dto);
        if (dto.Cron is not null)
            schedulerService.ScheduleTorrentUpdates(id, dto.Cron);

        return id;
    }

    public void UpdateOne(long id, TorrentUpdateDto dto)
    {
        torrentService.UpdateOne(id, dto);
        schedulerService.TryUnscheduleTorrentUpdates(id);
        if (dto.Cron is not null)
            schedulerService.ScheduleTorrentUpdates(id, dto.Cron);
    }

    public void RemoveOne(long id)
    {
        torrentService.RemoveOne(id);
        schedulerService.TryUnscheduleTorrentUpdates(id);
    }
}
