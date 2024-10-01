using TransmissionManager.Database.Dto;

namespace TransmissionManager.Api.UpdateTorrentById;

public static class UpdateTorrentByIdRequestExtensions
{
    public static TorrentUpdateDto ToTorrentUpdateDto(this UpdateTorrentByIdRequest dto) =>
        new(downloadDir: dto.DownloadDir, magnetRegexPattern: dto.MagnetRegexPattern, cron: dto.Cron);
}
