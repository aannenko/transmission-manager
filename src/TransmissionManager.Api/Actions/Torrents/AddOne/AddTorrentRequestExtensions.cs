using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Dto;
using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.Actions.Torrents.AddOne;

internal static class AddTorrentRequestExtensions
{
    public static TorrentAddDto ToTorrentAddDto(
        this AddTorrentRequest dto,
        TransmissionTorrentAddResponseItem transmissionTorrent,
        DateTime date)
    {
        return new(
            hashString: transmissionTorrent.HashString,
            refreshDate: date,
            name: transmissionTorrent.Name,
            webPageUri: dto.WebPageUri,
            downloadDir: dto.DownloadDir,
            magnetRegexPattern: dto.MagnetRegexPattern,
            cron: dto.Cron);
    }
}
