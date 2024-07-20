using Coravel.Scheduling.Schedule;
using Coravel.Scheduling.Schedule.Event;
using Coravel.Scheduling.Schedule.Interfaces;
using System.Collections.Concurrent;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Database.Services;

namespace TransmissionManager.Api.Scheduling.Services;

public sealed class TorrentSchedulerService(IScheduler scheduler, TorrentService torrentService)
{
    private static readonly ConcurrentDictionary<long, string> _scheduledTaskIdentifiers = [];

    private readonly Scheduler _scheduler = (scheduler as Scheduler)!;

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
        var schedule = (ScheduledEvent)_scheduler.ScheduleWithParams<TorrentUpdateTask>(torrentId).Cron(cron);
        _scheduledTaskIdentifiers[torrentId] = schedule.OverlappingUniqueIdentifier();
    }

    public bool TryUnscheduleTorrentUpdates(long torrentId)
    {
        return _scheduledTaskIdentifiers.Remove(torrentId, out var scheduleIdentifier)
            && _scheduler.TryUnschedule(scheduleIdentifier);
    }
}
