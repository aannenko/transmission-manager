using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Services;

public sealed class TorrentQueryService(AppDbContext dbContext)
{
    public async Task<Torrent?> FindOneByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

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
        ArgumentOutOfRangeException.ThrowIfNegative(pageDescriptor.AfterId);

        var query = dbContext.Torrents.AsNoTracking();

        if (!string.IsNullOrEmpty(filter.HashString))
            query = query.Where(torrent => torrent.HashString == filter.HashString);

        if (!string.IsNullOrEmpty(filter.WebPageUri))
            query = query.Where(torrent => torrent.WebPageUri == filter.WebPageUri);

        if (!string.IsNullOrEmpty(filter.NameStartsWith))
            query = query.Where(torrent => torrent.Name.StartsWith(filter.NameStartsWith));

        if (filter.CronExists is not null)
            query = query.Where(torrent => filter.CronExists.Value ? torrent.Cron != null : torrent.Cron == null);

        return await query.Where(torrent => torrent.Id > pageDescriptor.AfterId)
            .OrderBy(static torrent => torrent.Id)
            .Take(pageDescriptor.Take)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
