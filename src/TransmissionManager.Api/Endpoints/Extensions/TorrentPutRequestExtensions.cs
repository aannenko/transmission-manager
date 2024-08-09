using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Endpoints.Dto;

namespace TransmissionManager.Api.Endpoints.Extensions;

public static class TorrentPutRequestExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(this TorrentPatchRequest dto)
    {
        return new()
        {
            TransmissionId = dto.TransmissionId,
            DownloadDir = dto.DownloadDir,
            MagnetRegexPattern = dto.MagnetRegexPattern,
            Cron = dto.Cron,
        };
    }
}
