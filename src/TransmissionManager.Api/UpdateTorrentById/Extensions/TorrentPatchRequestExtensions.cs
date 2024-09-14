using TransmissionManager.Api.UpdateTorrentById.Request;
using TransmissionManager.Database.Dto;

namespace TransmissionManager.Api.UpdateTorrentById.Extensions;

public static class TorrentPatchRequestExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(this TorrentPatchRequest dto)
    {
        return new(
            magnetRegexPattern: dto.MagnetRegexPattern,
            cron: dto.Cron);
    }
}
