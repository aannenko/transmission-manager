namespace TransmissionManager.Api.Shared.Dto.Torrents;

public sealed record TorrentDto(
    long Id,
    string HashString,
    string Name,
    Uri WebPageUri,
    string DownloadDir,
    string? MagnetRegexPattern,
    string? Cron);
