using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Web.Extensions;

internal static class TorrentDtoExtensions
{
    /// <summary>
    /// Computes the state a torrent has after a PATCH the API accepted, by replaying it locally.
    /// </summary>
    /// <param name="previous">The torrent as it was before the request.</param>
    /// <param name="patch">The request the API accepted.</param>
    /// <returns>
    /// The updated torrent, with <see cref="TorrentDto.Version"/> incremented to match the server.
    /// </returns>
    /// <remarks>
    /// A <see langword="null"/> field leaves its property alone; an empty one clears the property,
    /// which only the three optional properties accept.
    /// </remarks>
    public static TorrentDto ApplyPatch(this TorrentDto previous, UpdateTorrentByIdRequest patch) =>
        previous with
        {
            DownloadDir = patch.DownloadDir is null ? previous.DownloadDir : patch.DownloadDir,
            MagnetRegexPattern = patch.MagnetRegexPattern is null
                ? previous.MagnetRegexPattern
                : patch.MagnetRegexPattern.Length == 0 ? null : patch.MagnetRegexPattern,
            JsonValueFormat = patch.JsonValueFormat is null
                ? previous.JsonValueFormat
                : patch.JsonValueFormat.Length == 0 ? null : patch.JsonValueFormat,
            Cron = patch.Cron is null
                ? previous.Cron
                : patch.Cron.Length == 0 ? null : patch.Cron,
            Version = previous.Version + 1,
        };
}
