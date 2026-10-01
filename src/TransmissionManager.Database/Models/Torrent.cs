using System.ComponentModel.DataAnnotations;
using TransmissionManager.Database.Dto;

namespace TransmissionManager.Database.Models;

/// <summary>Represents a torrent catalog row.</summary>
public sealed class Torrent
{
    /// <summary>Gets or sets the catalog ID.</summary>
    /// <returns>The catalog ID.</returns>
    public required long Id { get; set; }

    /// <summary>Gets or sets the torrent info hash.</summary>
    /// <returns>The info hash.</returns>
    public required string HashString { get; set; }

    /// <summary>Gets or sets the last refresh time.</summary>
    /// <returns>The refresh time.</returns>
    public required DateTime RefreshDate { get; set; }

    /// <summary>Gets or sets the torrent name.</summary>
    /// <returns>The torrent name.</returns>
    public required string Name { get; set; }

#pragma warning disable CA1056 // URI-like properties should not be strings - filtering is easier with strings
    /// <summary>Gets or sets the address from which to find the magnet link.</summary>
    /// <returns>The source URI.</returns>
    public required string SourceUri { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

    /// <summary>Gets or sets how the source is interpreted.</summary>
    /// <returns>The source kind.</returns>
    public required TorrentSourceKind SourceKind { get; set; }

    /// <summary>Gets or sets the target directory in Transmission.</summary>
    /// <returns>The download directory.</returns>
    public required string DownloadDir { get; set; }

    /// <summary>
    /// Extracts either a full magnet or a string value used to build one from what
    /// <see cref="SourceUri"/> yields. <c>null</c> means the default one for the source kind will
    /// be used.
    /// </summary>
    /// <returns>The magnet-link regular expression.</returns>
    public string? MagnetRegexPattern { get; set; }

    /// <summary>
    /// A string with a placeholder used to build a magnet link when <see cref="SourceKind"/> is
    /// <see cref="TorrentSourceKind.JsonPointer"/>. <c>null</c> means the configured default will
    /// be used.
    /// </summary>
    /// <returns>The JSON value format.</returns>
    public string? JsonValueFormat { get; set; }

    /// <summary>Gets or sets the optional refresh schedule.</summary>
    /// <returns>The five-field cron expression.</returns>
    public string? Cron { get; set; }

    /// <summary>
    /// Optimistic concurrency token, which a caller must echo to update or delete this torrent.
    /// </summary>
    /// <remarks>
    /// Starts at <c>1</c> and advances with every successful update. See the
    /// <c>&lt;remarks&gt;</c> on <c>TorrentService</c> for the OCC contract and the constraints any
    /// new mutation path must follow.
    /// </remarks>
    /// <returns>The current row version.</returns>
    [ConcurrencyCheck]
    public required long Version { get; set; }
}
