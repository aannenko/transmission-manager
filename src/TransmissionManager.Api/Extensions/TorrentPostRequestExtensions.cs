using TransmissionManager.Api.Dto;
using TransmissionManager.Database.Dto;
using TransmissionManager.TransmissionClient.Dto;

namespace TransmissionManager.Api.Extensions;

public static class TorrentPostRequestExtensions
{
    public static TorrentAddDto ToTorrentAddDto(
        this TorrentPostRequest dto,
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
        this TorrentPostRequest dto,
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
