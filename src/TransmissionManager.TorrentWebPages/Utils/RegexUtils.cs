using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentWebPages.Utils;

internal static class RegexUtils
{
    public static Regex CreateRegex(string pattern, TimeSpan matchTimeout) =>
        new(pattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeout);
}
