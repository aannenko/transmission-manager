using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Database.Services;

namespace TransmissionManager.Api.Scheduling.Services;

public sealed class StartupSchedulerService(TorrentSchedulerService torrentScheduler, TorrentService torrentService)
{
    public void ScheduleUpdatesForAllTorrents()
    {
        long afterId = 0;
        Torrent[]? torrentPage;
        while ((torrentPage = torrentService.FindPage(new(50, afterId, CronExists: true)))?.Length > 0)
        {
            afterId = torrentPage.Last().Id;
            foreach (var torrent in torrentPage)
                torrentScheduler.ScheduleTorrentUpdates(torrent.Id, torrent.Cron!);
        }
    }
}
