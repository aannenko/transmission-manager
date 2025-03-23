using Coravel.Scheduling.Schedule;
using Coravel.Scheduling.Schedule.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace TransmissionManager.Api.Services.Scheduling;

internal sealed class TorrentSchedulerService
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
            .PreventOverlapping(torrentId.ToString(CultureInfo.InvariantCulture));
    }

    public bool TryUnscheduleTorrentRefresh(long torrentId) =>
        _scheduler.TryUnschedule(torrentId.ToString(CultureInfo.InvariantCulture));
}
