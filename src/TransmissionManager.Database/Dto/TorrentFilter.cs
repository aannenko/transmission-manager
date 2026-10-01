namespace TransmissionManager.Database.Dto;

/// <summary>Describes optional torrent filters.</summary>
/// <param name="PropertyStartsWith">The optional case-insensitive property prefix.</param>
/// <param name="CronExists">Whether to require or exclude torrents with a refresh schedule.</param>
public readonly record struct TorrentFilter(string? PropertyStartsWith = null, bool? CronExists = null);
