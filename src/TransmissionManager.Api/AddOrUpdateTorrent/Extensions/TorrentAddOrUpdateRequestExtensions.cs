using TransmissionManager.Api.AddOrUpdateTorrent.Request;
using TransmissionManager.Database.Dto;
using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.AddOrUpdateTorrent.Extensions;

public static class TorrentAddOrUpdateRequestExtensions
{
    public static TorrentAddDto ToTorrentAddDto(
        this TorrentAddOrUpdateRequest dto,
        TransmissionTorrentAddResponseItem transmissionTorrent)
    {
        return new(
            hashString: transmissionTorrent.HashString,
            name: transmissionTorrent.Name,
            webPageUri: dto.WebPageUri,
            downloadDir: dto.DownloadDir,
            magnetRegexPattern: dto.MagnetRegexPattern,
            cron: dto.Cron);
    }

    public static TorrentUpdateDto ToTorrentUpdateDto(
        this TorrentAddOrUpdateRequest dto,
        TransmissionTorrentAddResponseItem transmissionTorrent)
    {
        return new(
            hashString: transmissionTorrent.HashString,
            name: transmissionTorrent.Name,
            downloadDir: dto.DownloadDir,
            magnetRegexPattern: dto.MagnetRegexPattern,
            cron: dto.Cron);
    }
}
