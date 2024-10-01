using TransmissionManager.Database.Dto;
using TransmissionManager.Transmission.Dto;

namespace TransmissionManager.Api.AddTorrent;

public static class AddTorrentRequestExtensions
{
    public static TorrentAddDto ToTorrentAddDto(
        this AddTorrentRequest dto,
        TransmissionTorrentAddResponseItem transmissionTorrent)
    {
        return new(
            hashString: transmissionTorrent.HashString,
            name: transmissionTorrent.Name,
            webPageUri: dto.WebPageUri.AbsoluteUri,
            downloadDir: dto.DownloadDir,
            magnetRegexPattern: dto.MagnetRegexPattern,
            cron: dto.Cron);
    }
}
