using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentWebPages.Utils;

internal static class RegexUtils
{
    public static Regex CreateRegex(string pattern, TimeSpan matchTimeout)
    {
        return new(
            pattern,
            RegexOptions.Compiled | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture,
            matchTimeout);
    }
}