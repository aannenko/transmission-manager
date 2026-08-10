using TransmissionManager.TorrentSources.Dto;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class MagnetSearchResultExtensions
{
    /// <summary>
    /// Determines whether the specified result means the source cannot yield a magnet until
    /// something changes outside this application.
    /// </summary>
    /// <param name="result">The result to examine.</param>
    /// <returns>Whether retrying unchanged is pointless.</returns>
    /// <remarks>
    /// <see cref="MagnetSearchResult.NotFound"/> is the debatable member and sits here deliberately:
    /// the source answered, so a retry changes nothing about the dependency. The known misjudgement,
    /// measured rather than imagined: an API answering <c>200</c> with
    /// <c>{"error":{"code":1,"text":"Temporarily disabled"}}</c> lands here too and does resolve
    /// itself. Telling it apart needs vendor-specific parsing, which this project refuses; the
    /// message carries what happened, because no status code can express "probably".
    /// </remarks>
    public static bool IsUnprocessableSource(this MagnetSearchResult result) =>
        result is MagnetSearchResult.InvalidSource
            or MagnetSearchResult.InvalidSelector
            or MagnetSearchResult.NotFound;
}
