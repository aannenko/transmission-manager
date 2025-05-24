using TransmissionManager.Database.Models;

namespace TransmissionManager.Database.Dto;

internal static class TorrentAddDtoExtensions
{
    public static Torrent ToTorrent(this TorrentAddDto dto)
    {
        return new()
        {
            Id = default,
            HashString = dto.HashString,
            HashStringDate = dto.HashStringDate,
            Name = dto.Name,
            DownloadDir = dto.DownloadDir,
            WebPageUri = dto.WebPageUri.OriginalString,
            MagnetRegexPattern = dto.MagnetRegexPattern,
            Cron = dto.Cron,
        };
    }
}
