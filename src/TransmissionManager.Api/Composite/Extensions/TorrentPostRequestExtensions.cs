using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Endpoints.Dto;
using TransmissionManager.Api.Transmission.Dto;

namespace TransmissionManager.Api.Composite.Extensions;

public static class TorrentPostRequestExtensions
{
    public static TorrentAddDto ToTorrentAddDto(
        this TorrentPostRequest dto,
        TransmissionTorrentAddResponseItem transmissionTorrent)
    {
        return new()
        {
            TransmissionId = transmissionTorrent.Id,
            Name = transmissionTorrent.Name,
            DownloadDir = dto.DownloadDir,
            WebPageUri = dto.WebPageUri,
            MagnetRegexPattern = dto.MagnetRegexPattern,
            Cron = dto.Cron,
        };
    }

    public static TorrentUpdateDto ToTorrentUpdateDto(
        this TorrentPostRequest dto,
        TransmissionTorrentAddResponseItem transmissionTorrent)
    {
        return new()
        {
            TransmissionId = transmissionTorrent.Id,
            Name = transmissionTorrent.Name,
            DownloadDir = dto.DownloadDir,
            MagnetRegexPattern = dto.MagnetRegexPattern,
            Cron = dto.Cron,
        };
    }
}
