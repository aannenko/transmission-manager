namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed record TorrentDto(
    long Id,
    string HashString,
    DateTimeOffset RefreshDate,
    string Name,
    Uri WebPageUri,
    string DownloadDir,
    string? MagnetRegexPattern,
    string? Cron);
