using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.Extensions;

internal static class TorrentExtensions
{
    public static TorrentDto ToDto(this Torrent torrent)
    {
        return new(
            Id: torrent.Id,
            HashString: torrent.HashString,
            Name: torrent.Name,
            WebPageUri: new(torrent.WebPageUri),
            DownloadDir: torrent.DownloadDir,
            MagnetRegexPattern: torrent.MagnetRegexPattern,
            Cron: torrent.Cron);
    }
}
