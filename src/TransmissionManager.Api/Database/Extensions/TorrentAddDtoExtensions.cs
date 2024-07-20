using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;

namespace TransmissionManager.Api.Database.Extensions;

public static class TorrentAddDtoExtensions
{
    public static Torrent ToTorrent(this TorrentAddDto dto)
    {
        return new()
        {
            Id = default,
            TransmissionId = dto.TransmissionId,
            Name = dto.Name,
            DownloadDir = dto.DownloadDir,
            WebPageUri = dto.WebPageUri,
            MagnetRegexPattern = dto.MagnetRegexPattern,
            Cron = dto.Cron,
        };
    }
}
