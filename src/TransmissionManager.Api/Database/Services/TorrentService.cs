using Microsoft.EntityFrameworkCore;
using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Extensions;
using TransmissionManager.Api.Database.Models;

namespace TransmissionManager.Api.Database.Services;

public sealed class TorrentService(AppDbContext dbContext)
{
    public async Task<Torrent[]> FindPageAsync(
        PageDescriptor pageDescriptor,
        TorrentFilter filter = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageDescriptor.Take);
        ArgumentOutOfRangeException.ThrowIfNegative(pageDescriptor.AfterId);

        var query = dbContext.Torrents.AsNoTracking();

        if (!string.IsNullOrEmpty(filter.NameStartsWith))
            query = query.Where(torrent => torrent.Name.StartsWith(filter.NameStartsWith));

        if (!string.IsNullOrEmpty(filter.WebPageUri))
            query = query.Where(torrent => torrent.WebPageUri == filter.WebPageUri);

        if (filter.CronExists is not null)
            query = query.Where(torrent => filter.CronExists.Value ? torrent.Cron != null : torrent.Cron == null);

        return await query.Where(torrent => torrent.Id > pageDescriptor.AfterId)
            .OrderBy(static torrent => torrent.Id)
            .Take(pageDescriptor.Take)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Torrent?> FindOneByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        return await dbContext.Torrents.AsNoTracking()
            .FirstOrDefaultAsync(torrent => torrent.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<long> AddOneAsync(TorrentAddDto dto, CancellationToken cancellationToken = default)
    {
        var torrent = dto.ToTorrent();
        dbContext.Torrents.Add(torrent);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return torrent.Id;
    }

    public async Task<bool> TryUpdateOneByIdAsync(
        long id,
        TorrentUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        var updatedRows = await dbContext.Torrents
            .Where(torrent => torrent.Id == id)
            .ExecuteUpdateAsync(
                properties => properties
                    .SetProperty(
                        static torrent => torrent.HashString,
                        torrent => dto.HashString ?? torrent.HashString)
                    .SetProperty(
                        static torrent => torrent.Name,
                        torrent => dto.Name ?? torrent.Name)
                    .SetProperty(
                        static torrent => torrent.DownloadDir,
                        torrent => dto.DownloadDir ?? torrent.DownloadDir)
                    .SetProperty(
                        static torrent => torrent.MagnetRegexPattern,
                        torrent => dto.MagnetRegexPattern == string.Empty
                            ? null
                            : dto.MagnetRegexPattern ?? torrent.MagnetRegexPattern)
                    .SetProperty(
                        static torrent => torrent.Cron,
                        torrent => dto.Cron == string.Empty
                            ? null
                            : dto.Cron ?? torrent.Cron),
                cancellationToken)
            .ConfigureAwait(false);

        return updatedRows is 1;
    }

    public async Task<bool> TryDeleteOneByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        var deletedRows = await dbContext.Torrents
            .Where(torrent => torrent.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        return deletedRows is 1;
    }
}
