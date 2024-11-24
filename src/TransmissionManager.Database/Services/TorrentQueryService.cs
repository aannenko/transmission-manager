using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Services;

public sealed class TorrentQueryService(AppDbContext dbContext)
{
    public async Task<Torrent?> FindOneByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Torrents.AsNoTracking()
            .FirstOrDefaultAsync(torrent => torrent.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Torrent[]> FindPageAsync(
        PageDescriptor pageDescriptor,
        TorrentFilter filter = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageDescriptor.Take);

        var query = dbContext.Torrents.AsNoTracking();

        if (!string.IsNullOrEmpty(filter.PropertyStartsWith))
        {
            query = query.Where(torrent =>
                torrent.HashString.StartsWith(filter.PropertyStartsWith) ||
                torrent.Name.StartsWith(filter.PropertyStartsWith) ||
                torrent.WebPageUri.StartsWith(filter.PropertyStartsWith) ||
                torrent.DownloadDir.StartsWith(filter.PropertyStartsWith));
        }

        if (filter.CronExists is not null)
            query = query.Where(torrent => filter.CronExists.Value ? torrent.Cron != null : torrent.Cron == null);

        return await query.Where(torrent => torrent.Id > pageDescriptor.AfterId)
            .Take(pageDescriptor.Take)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
