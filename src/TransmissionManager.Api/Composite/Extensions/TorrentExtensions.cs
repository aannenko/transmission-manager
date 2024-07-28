using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Transmission.Models;

namespace TransmissionManager.Api.Composite.Extensions;

public static class TorrentExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(
        this Torrent torrent,
        TransmissionTorrentAddResponseItem transmissionTorrent)
    {
        return new()
        {
            TransmissionId = transmissionTorrent.Id,
            Name = transmissionTorrent.Name,
            DownloadDir = torrent.DownloadDir,
            MagnetRegexPattern = torrent.MagnetRegexPattern,
            Cron = torrent.Cron,
        };
    }
}
