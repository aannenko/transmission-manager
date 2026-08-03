using TransmissionManager.TorrentSources.Dto;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class MagnetSearchResultExtensions
{
    /// <summary>
    /// Determines whether the specified result faults the input rather than the torrent web page.
    /// </summary>
    /// <param name="result">The result to examine.</param>
    /// <returns>Whether the caller must correct its input before retrying.</returns>
    /// <remarks>
    /// Only these results justify a 4xx blaming the caller; the rest are dependency failures.
    /// </remarks>
    public static bool IsInvalidInput(this MagnetSearchResult result) =>
        result is MagnetSearchResult.InvalidSource or MagnetSearchResult.InvalidSelector;
}
