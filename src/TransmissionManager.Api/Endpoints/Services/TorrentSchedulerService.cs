using Coravel.Scheduling.Schedule;
using Coravel.Scheduling.Schedule.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace TransmissionManager.Api.Endpoints.Services;

public sealed class TorrentSchedulerService
{
    private readonly Scheduler _scheduler;

    public TorrentSchedulerService(ILogger<IScheduler> logger, IScheduler scheduler)
    {
        _scheduler = (Scheduler)scheduler;
        _scheduler.LogScheduledTaskProgress(logger);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TorrentRefreshTask))]
    public void ScheduleTorrentUpdates(long torrentId, string cron)
    {
        _scheduler.ScheduleWithParams<TorrentRefreshTask>(torrentId)
            .Cron(cron)
            .Zoned(TimeZoneInfo.Local)
            .PreventOverlapping(torrentId.ToString());
    }

    public bool TryUnscheduleTorrentUpdates(long torrentId)
    {
        return _scheduler.TryUnschedule(torrentId.ToString());
    }
}
