using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentWebPages.Extensions;

internal static class RegexExtensions
{
    public static bool TryGetFirstMatch(this Regex regex, ReadOnlySpan<char> span, out Range matchRange)
    {
        foreach (var match in regex.EnumerateMatches(span))
        {
            matchRange = new(match.Index, match.Index + match.Length);
            return true;
        }

        matchRange = Range.EndAt(0);
        return false;
    }
}
