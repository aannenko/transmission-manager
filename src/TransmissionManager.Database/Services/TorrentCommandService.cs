using Microsoft.EntityFrameworkCore;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Extensions;

namespace TransmissionManager.Database.Services;

public sealed class TorrentCommandService(AppDbContext dbContext)
{
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
