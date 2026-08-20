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
            RefreshDate = dto.RefreshDate,
            Name = dto.Name,
            DownloadDir = dto.DownloadDir,
            SourceUri = dto.SourceUri.OriginalString,
            SourceKind = dto.SourceKind,
            MagnetRegexPattern = dto.MagnetRegexPattern,
            JsonValueFormat = dto.JsonValueFormat,
            Cron = dto.Cron,
            Version = 1,
        };
    }
}
