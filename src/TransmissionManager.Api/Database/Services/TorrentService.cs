using Microsoft.EntityFrameworkCore;
using TransmissionManager.Api.Database.Abstractions;
using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Extensions;
using TransmissionManager.Api.Database.Models;

namespace TransmissionManager.Api.Database.Services;

public sealed class TorrentService(AppDbContext dbContext) : ITorrentService
{
    public Torrent[] FindPage(TorrentPageDescriptor dto)
    {
        var query = dbContext.Torrents.AsNoTracking();

        if (!string.IsNullOrEmpty(dto.NameStartsWith))
            query = query.Where(torrent => torrent.Name.StartsWith(dto.NameStartsWith));

        if (!string.IsNullOrEmpty(dto.WebPageUri))
            query = query.Where(torrent => torrent.WebPageUri == dto.WebPageUri);

        if (dto.CronExists is not null)
            query = query.Where(torrent => torrent.Cron != null);

        return query.Where(torrent => torrent.Id > dto.AfterId)
            .OrderBy(static torrent => torrent.Id)
            .Take(dto.Take)
            .ToArray();
    }

    public Torrent? FindOneById(long id)
    {
        return dbContext.Torrents.AsNoTracking().FirstOrDefault(torrent => torrent.Id == id);
    }

    public long AddOne(TorrentAddDto dto)
    {
        var torrent = dto.ToTorrent();
        dbContext.Torrents.Add(torrent);
        dbContext.SaveChanges();
        return torrent.Id;
    }

    public bool TryUpdateOneById(long id, TorrentUpdateDto dto)
    {
        var updatedRows = dbContext.Torrents
            .AsNoTracking()
            .Where(torrent => torrent.Id == id)
            .ExecuteUpdate(properties => properties
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
                        : dto.Cron ?? torrent.Cron));

        return updatedRows is 1;
    }

    public bool TryDeleteOneById(long id)
    {
        var deletedRows = dbContext.Torrents
            .AsNoTracking()
            .Where(torrent => torrent.Id == id)
            .ExecuteDelete();

        return deletedRows is 1;
    }
}
