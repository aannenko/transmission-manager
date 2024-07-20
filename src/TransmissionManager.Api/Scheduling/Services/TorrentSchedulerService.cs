using Coravel.Scheduling.Schedule;
using Coravel.Scheduling.Schedule.Interfaces;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Database.Services;

namespace TransmissionManager.Api.Scheduling.Services;

public sealed class TorrentSchedulerService(IScheduler scheduler, TorrentService torrentService)
{
    private readonly Scheduler _scheduler = (Scheduler)scheduler;

    public void ScheduleUpdatesForAllTorrents()
    {
        long afterId = 0;
        Torrent[]? torrentPage;
        while ((torrentPage = torrentService.FindPage(new(50, afterId, CronExists: true)))?.Length is not null and > 0)
        {
            afterId = torrentPage.Last().Id;
            foreach (var torrent in torrentPage)
                ScheduleTorrentUpdates(torrent.Id, torrent.Cron!);
        }
    }

    public void ScheduleTorrentUpdates(long torrentId, string cron)
    {
        _scheduler.ScheduleWithParams<TorrentUpdateTask>(torrentId)
            .Cron(cron)
            .Zoned(TimeZoneInfo.Local)
            .PreventOverlapping(torrentId.ToString());
    }

    public bool TryUnscheduleTorrentUpdates(long torrentId)
    {
        return _scheduler.TryUnschedule(torrentId.ToString());
    }
}
