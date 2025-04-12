using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Dto;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class UpdateTorrentByIdRequestExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(this UpdateTorrentByIdRequest dto) =>
        new(downloadDir: dto.DownloadDir, magnetRegexPattern: dto.MagnetRegexPattern, cron: dto.Cron);
}
