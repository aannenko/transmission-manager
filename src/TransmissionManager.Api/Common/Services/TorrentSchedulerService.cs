using Coravel.Scheduling.Schedule;
using Coravel.Scheduling.Schedule.Interfaces;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Scheduling;

namespace TransmissionManager.Api.Common.Services;

public sealed class TorrentSchedulerService
{
    private readonly Scheduler _scheduler;

    public TorrentSchedulerService(IScheduler scheduler)
    {
        _scheduler = (Scheduler)scheduler;
        _scheduler.LogScheduledTaskProgress();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TorrentRefreshTask))]
    public void ScheduleTorrentRefresh(long torrentId, string cron)
    {
        _scheduler.ScheduleWithParams<TorrentRefreshTask>(torrentId)
            .Cron(cron)
            .Zoned(TimeZoneInfo.Local)
            .PreventOverlapping(torrentId.ToString());
    }

    public bool TryUnscheduleTorrentRefresh(long torrentId) =>
        _scheduler.TryUnschedule(torrentId.ToString());
}
