using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.TransmissionClient.Dto;

namespace TransmissionManager.Api.Endpoints.Extensions;

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
