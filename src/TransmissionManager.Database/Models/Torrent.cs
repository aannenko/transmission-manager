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
    /// <c>TorrentAddDtoExtensions.ToTorrent</c> and bumped by <c>TorrentService.UpdateOneAsync</c>
    /// via an explicit <c>SetProperty</c> inside <c>ExecuteUpdateAsync</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="ConcurrencyCheckAttribute"/> is defence in depth for any future code path that
    /// mutates a <c>Torrent</c> via the EF change tracker. See the <c>&lt;remarks&gt;</c> on
    /// <c>TorrentService</c> for the OCC contract and the constraints such code must follow.
    /// </remarks>
    [ConcurrencyCheck]
    public required long Version { get; set; }
}
