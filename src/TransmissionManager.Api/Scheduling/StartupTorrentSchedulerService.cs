using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Scheduling;

internal sealed class StartupTorrentSchedulerService(AppDbContext dbContext, TorrentSchedulerService scheduler)
{
    private readonly record struct TorrentIdCron(long Id, string Cron);

    public async Task ScheduleUpdatesForAllTorrentsAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var torrent in dbContext.Torrents
            .Where(static torrent => torrent.Cron != null)
            .Select(static torrent => new TorrentIdCron(torrent.Id, torrent.Cron!))
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            scheduler.ScheduleTorrentRefresh(torrent.Id, torrent.Cron);
        }
    }
}
