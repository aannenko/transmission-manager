using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Database.Services;

namespace TransmissionManager.Api.Endpoints.Services;

public sealed class StartupSchedulerService(TorrentSchedulerService torrentScheduler, TorrentService torrentService)
{
    public async Task ScheduleUpdatesForAllTorrentsAsync()
    {
        Torrent[] torrentPage;
        var page = new PageDescriptor(50, 0);
        var filter = new TorrentFilter(CronExists: true);
        while ((torrentPage = await torrentService.FindPageAsync(page, filter).ConfigureAwait(false)).Length > 0)
        {
            page = page with { AfterId = torrentPage.Last().Id };
            foreach (var torrent in torrentPage)
                torrentScheduler.ScheduleTorrentUpdates(torrent.Id, torrent.Cron!);
        }
    }
}
