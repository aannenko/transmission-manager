using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Database.Models;

internal static class TorrentExtensions
{
    public static TorrentDto ToDto(this Torrent torrent)
    {
        return new(
            Id: torrent.Id,
            HashString: torrent.HashString,
            HashStringDate: torrent.HashStringDate,
            Name: torrent.Name,
            WebPageUri: new(torrent.WebPageUri),
            DownloadDir: torrent.DownloadDir,
            MagnetRegexPattern: torrent.MagnetRegexPattern,
            Cron: torrent.Cron);
    }
}
