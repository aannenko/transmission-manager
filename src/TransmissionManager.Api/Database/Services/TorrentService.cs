using Microsoft.EntityFrameworkCore;
using TransmissionManager.Api.Database.Abstractions;
using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Extensions;
using TransmissionManager.Api.Database.Models;

namespace TransmissionManager.Api.Database.Services;

public sealed class TorrentService(AppDbContext dbContext) : ITorrentService
{
    public async Task<Torrent[]> FindPageAsync(TorrentPageDescriptor dto)
    {
        var query = dbContext.Torrents.AsNoTracking();

        if (!string.IsNullOrEmpty(dto.NameStartsWith))
            query = query.Where(torrent => torrent.Name.StartsWith(dto.NameStartsWith));

        if (!string.IsNullOrEmpty(dto.WebPageUri))
            query = query.Where(torrent => torrent.WebPageUri == dto.WebPageUri);

        if (dto.CronExists is not null)
            query = query.Where(static torrent => torrent.Cron != null);

        return await query.Where(torrent => torrent.Id > dto.AfterId)
            .OrderBy(static torrent => torrent.Id)
            .Take(dto.Take)
            .ToArrayAsync()
            .ConfigureAwait(false);
    }

    public async Task<Torrent?> FindOneByIdAsync(long id)
    {
        return await dbContext.Torrents.AsNoTracking()
            .FirstOrDefaultAsync(torrent => torrent.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<long> AddOneAsync(TorrentAddDto dto)
    {
        var torrent = dto.ToTorrent();
        dbContext.Torrents.Add(torrent);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
        return torrent.Id;
    }

    public async Task<bool> TryUpdateOneByIdAsync(long id, TorrentUpdateDto dto)
    {
        var updatedRows = await dbContext.Torrents
            .Where(torrent => torrent.Id == id)
            .ExecuteUpdateAsync(properties => properties
                .SetProperty(
                    static torrent => torrent.TransmissionId,
                    torrent => dto.TransmissionId ?? torrent.TransmissionId)
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
                        : dto.Cron ?? torrent.Cron))
            .ConfigureAwait(false);

        return updatedRows is 1;
    }

    public async Task<bool> TryDeleteOneByIdAsync(long id)
    {
        var deletedRows = await dbContext.Torrents
            .Where(torrent => torrent.Id == id)
            .ExecuteDeleteAsync()
            .ConfigureAwait(false);

        return deletedRows is 1;
    }
}
