using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Web.Extensions;

internal static class TorrentDtoExtensions
{
    /// <summary>
    /// Computes the post-PATCH state of <paramref name="previous"/> deterministically by replaying
    /// the request locally:
    ///   null  -> property unchanged,
    ///   ""    -> for MagnetRegexPattern/Cron only, clears the property to null.
    /// Increments <see cref="TorrentDto.Version"/> by 1 to match the server's increment on Success.
    /// </summary>
    /// <remarks>
    /// Replays only the fields this application sends today. The request can also carry the JSON
    /// source's magnet format; whoever starts sending it must replay it here too, or the page will
    /// show a stale value after a successful update.
    /// </remarks>
    public static TorrentDto ApplyPatch(this TorrentDto previous, UpdateTorrentByIdRequest patch) =>
        previous with
        {
            DownloadDir = patch.DownloadDir is null ? previous.DownloadDir : patch.DownloadDir,
            MagnetRegexPattern = patch.MagnetRegexPattern is null
                ? previous.MagnetRegexPattern
                : patch.MagnetRegexPattern.Length == 0 ? null : patch.MagnetRegexPattern,
            Cron = patch.Cron is null
                ? previous.Cron
                : patch.Cron.Length == 0 ? null : patch.Cron,
            Version = previous.Version + 1,
        };
}
