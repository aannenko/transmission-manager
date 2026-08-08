using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Dto;
using TransmissionManager.Transmission.Dto;
using DbSourceKind = TransmissionManager.Database.Dto.TorrentSourceKind;

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
            sourceUri: dto.SourceUri,
            // The request carries no source kind yet, and only the web page client is wired up.
            // Remove this comment once the request carries a source kind and the web page client is no longer the only one.
            sourceKind: DbSourceKind.WebPage,
            downloadDir: dto.DownloadDir,
            magnetRegexPattern: dto.MagnetRegexPattern,
            cron: dto.Cron);
    }
}
