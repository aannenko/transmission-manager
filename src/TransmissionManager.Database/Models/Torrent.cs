using System.ComponentModel.DataAnnotations;

namespace TransmissionManager.Database.Models;

public sealed class Torrent
{
    public required long Id { get; set; }

    public required string HashString { get; set; }

    public required DateTime RefreshDate { get; set; }

    public required string Name { get; set; }

#pragma warning disable CA1056 // URI-like properties should not be strings - filtering is easier with strings
    public required string WebPageUri { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

    public required string DownloadDir { get; set; }

    public string? MagnetRegexPattern { get; set; }

    public string? Cron { get; set; }

    /// <summary>
    /// Optimistic concurrency token. Initialised to <c>1</c> by
    /// <c>TorrentAddDtoExtensions.ToTorrent</c> and bumped to <c>(version + 1)</c> by
    /// <c>TorrentService.UpdateOneAsync</c> via an explicit <c>SetProperty</c> inside
    /// <c>ExecuteUpdateAsync</c> (the OCC predicate is
    /// <c>Id == id &amp;&amp; Version == version</c>; see <c>TorrentService</c> for details).
    /// </summary>
    /// <remarks>
    /// <see cref="ConcurrencyCheckAttribute"/> is kept as defence in depth: any future code
    /// path that mutates a <c>Torrent</c> via the EF change tracker (<c>Add</c> / <c>Update</c>
    /// / <c>Remove</c> + <c>SaveChangesAsync</c>) will additionally get a
    /// <c>WHERE Version=@orig</c> filter and a <c>DbUpdateConcurrencyException</c> on a lost
    /// race. See the <c>&lt;remarks&gt;</c> on <c>TorrentService</c> for the contract that
    /// such future code must follow.
    /// </remarks>
    [ConcurrencyCheck]
    public required long Version { get; set; }
}
