using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Common.Scheduling;

public sealed class StartupTorrentSchedulerService(TorrentQueryService queryService, TorrentSchedulerService scheduler)
{
    private static readonly TorrentFilter _filter = new(CronExists: true);

    public async Task ScheduleUpdatesForAllTorrentsAsync()
    {
        Torrent[] torrents;
        var pageDescriptor = new PageDescriptor(50, 0);
        while ((torrents = await queryService.FindPageAsync(pageDescriptor, _filter).ConfigureAwait(false)).Length > 0)
        {
            pageDescriptor = pageDescriptor with { AfterId = torrents[^1].Id };
            foreach (var torrent in torrents)
                scheduler.ScheduleTorrentRefresh(torrent.Id, torrent.Cron!);
        }
    }
}
