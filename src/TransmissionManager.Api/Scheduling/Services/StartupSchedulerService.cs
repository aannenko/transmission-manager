using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Database.Services;

namespace TransmissionManager.Api.Scheduling.Services;

public sealed class StartupSchedulerService(TorrentSchedulerService torrentScheduler, TorrentService torrentService)
{
    public async Task ScheduleUpdatesForAllTorrentsAsync()
    {
        Torrent[] torrentPage;
        var pageDescriptor = new TorrentPageDescriptor(Take: 50, AfterId: 0, CronExists: true);
        while ((torrentPage = await torrentService.FindPageAsync(pageDescriptor).ConfigureAwait(false)).Length > 0)
        {
            pageDescriptor = pageDescriptor with { AfterId = torrentPage.Last().Id };
            foreach (var torrent in torrentPage)
                torrentScheduler.ScheduleTorrentUpdates(torrent.Id, torrent.Cron!);
        }
    }
}
