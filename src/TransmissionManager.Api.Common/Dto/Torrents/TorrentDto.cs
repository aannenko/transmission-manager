namespace TransmissionManager.Api.Common.Dto.Torrents;

/// <summary>Describes a torrent in the catalog.</summary>
/// <param name="Id">The catalog ID.</param>
/// <param name="HashString">The torrent info hash.</param>
/// <param name="RefreshDate">The last refresh time.</param>
/// <param name="Name">The torrent name.</param>
/// <param name="SourceUri">The source URI.</param>
/// <param name="SourceKind">How the source is interpreted.</param>
/// <param name="DownloadDir">The Transmission download directory.</param>
/// <param name="MagnetRegexPattern">The optional magnet-link regular expression.</param>
/// <param name="JsonValueFormat">The optional JSON value format.</param>
/// <param name="Cron">The optional refresh schedule.</param>
/// <param name="Version">The optimistic-concurrency version.</param>
public sealed record TorrentDto(
    long Id,
    string HashString,
    DateTimeOffset RefreshDate,
    string Name,
    Uri SourceUri,
    TorrentSourceKind SourceKind,
    string DownloadDir,
    string? MagnetRegexPattern,
    string? JsonValueFormat,
    string? Cron,
    long Version);
