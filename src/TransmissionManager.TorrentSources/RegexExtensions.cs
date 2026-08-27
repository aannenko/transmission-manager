using System.Diagnostics.CodeAnalysis;
using TransmissionManager.TorrentSources;

namespace System.Text.RegularExpressions;

internal static class RegexExtensions
{
    public static bool TryGetFirstMatch(this Regex regex, ReadOnlySpan<char> span, out Range matchRange)
    {
        foreach (var match in regex.EnumerateMatches(span))
        {
            matchRange = new(match.Index, match.Index + match.Length);
            return true;
        }

        matchRange = default;
        return false;
    }

    public static bool TryGetFirstMatch(
        this ReadOnlySpan<char> span,
        [StringSyntax(StringSyntaxAttribute.Regex)] string pattern,
        TimeSpan matchTimeout,
        out Range matchRange)
    {
        foreach (var match in Regex.EnumerateMatches(span, pattern, RegexUtils.PatternOptions, matchTimeout))
        {
            matchRange = new(match.Index, match.Index + match.Length);
            return true;
        }

        matchRange = default;
        return false;
    }
}
