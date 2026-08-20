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
            sourceKind: (DbSourceKind)dto.SourceKind,
            downloadDir: dto.DownloadDir,
            magnetRegexPattern: OrNullOnEmptyString(dto.MagnetRegexPattern),
            jsonValueFormat: OrNullOnEmptyString(dto.JsonValueFormat),
            cron: OrNullOnEmptyString(dto.Cron));
    }

    private static string? OrNullOnEmptyString(string? value) =>
        string.IsNullOrEmpty(value) ? null : value;
}
