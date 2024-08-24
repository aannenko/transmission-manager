using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Transmission.Dto;

namespace TransmissionManager.Api.Composite.Extensions;

public static class TorrentExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(
        this Torrent torrent,
        TransmissionTorrentAddResponseItem transmissionTorrent)
    {
        return new(
            transmissionId: transmissionTorrent.Id,
            name: transmissionTorrent.Name,
            downloadDir: torrent.DownloadDir,
            magnetRegexPattern: torrent.MagnetRegexPattern,
            cron: torrent.Cron);
    }
}
