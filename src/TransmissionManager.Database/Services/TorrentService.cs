using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Services;

public sealed class TorrentService(AppDbContext dbContext)
{
    public async Task<Torrent?> FindOneByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Torrents.AsNoTracking()
            .FirstOrDefaultAsync(torrent => torrent.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Torrent[]> GetPageAsync<T>(
        TorrentPageDescriptor<T> page = default,
        TorrentFilter filter = default,
        CancellationToken cancellationToken = default)
    {
        if (page == default)
            page = new TorrentPageDescriptor<T>();

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

        return await query.WhereOrderByTake(page).ToArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Torrent> AddOneAsync(TorrentAddDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var torrent = dto.ToTorrent();
        dbContext.Torrents.Add(torrent);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return torrent;
    }

    public async Task<bool> TryUpdateOneByIdAsync(
        long id,
        TorrentUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var updatedRows = await dbContext.Torrents
            .Where(torrent => torrent.Id == id)
            .ExecuteUpdateAsync(
                properties => properties
                    .SetProperty(
                        static torrent => torrent.HashString,
                        torrent => dto.HashString ?? torrent.HashString)
                    .SetProperty(
                        static torrent => torrent.RefreshDate,
                        torrent => dto.RefreshDate ?? torrent.RefreshDate)
                    .SetProperty(
                        static torrent => torrent.Name,
                        torrent => dto.Name ?? torrent.Name)
                    .SetProperty(
                        static torrent => torrent.DownloadDir,
                        torrent => dto.DownloadDir ?? torrent.DownloadDir)
                    .SetProperty(
                        static torrent => torrent.MagnetRegexPattern,
                        torrent => dto.MagnetRegexPattern != null && dto.MagnetRegexPattern.Length == 0
                            ? null
                            : dto.MagnetRegexPattern ?? torrent.MagnetRegexPattern)
                    .SetProperty(
                        static torrent => torrent.Cron,
                        torrent => dto.Cron != null && dto.Cron.Length == 0
                            ? null
                            : dto.Cron ?? torrent.Cron),
                cancellationToken)
            .ConfigureAwait(false);

        return updatedRows is 1;
    }

    public async Task<bool> TryDeleteOneByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var deletedRows = await dbContext.Torrents
            .Where(torrent => torrent.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        return deletedRows is 1;
    }
}
