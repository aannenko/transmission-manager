using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Endpoints.Dto;

namespace TransmissionManager.Api.Endpoints.Extensions;

public static class TorrentPatchRequestExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(this TorrentPatchRequest dto)
    {
        return new(
            transmissionId: dto.TransmissionId,
            name: null,
            downloadDir: dto.DownloadDir,
            magnetRegexPattern: dto.MagnetRegexPattern,
            cron: dto.Cron);
    }
}
