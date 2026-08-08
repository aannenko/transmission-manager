namespace TransmissionManager.Api.Common.Dto.Torrents;

public sealed record TorrentDto(
    long Id,
    string HashString,
    DateTimeOffset RefreshDate,
    string Name,
    Uri SourceUri,
    TorrentSourceKind SourceKind,
    string DownloadDir,
    string? MagnetRegexPattern,
    string? Cron,
    long Version);
