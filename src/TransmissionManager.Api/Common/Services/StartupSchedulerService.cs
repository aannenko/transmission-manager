using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Common.Services;

public sealed class StartupSchedulerService(TorrentQueryService queryService, TorrentSchedulerService scheduler)
{
    public async Task ScheduleUpdatesForAllTorrentsAsync()
    {
        Torrent[] torrentPage;
        var page = new PageDescriptor(50, 0);
        var filter = new TorrentFilter(CronExists: true);
        while ((torrentPage = await queryService.FindPageAsync(page, filter).ConfigureAwait(false)).Length > 0)
        {
            page = page with { AfterId = torrentPage.Last().Id };
            foreach (var torrent in torrentPage)
                scheduler.ScheduleTorrentRefresh(torrent.Id, torrent.Cron!);
        }
    }
}
