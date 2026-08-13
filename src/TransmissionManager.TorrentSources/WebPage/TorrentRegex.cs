using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentSources.WebPage;

internal static partial class TorrentRegex
{
    // language=regex
    public const string IsFindMagnet = @"^.*magnet:\\\?.+$";

    [GeneratedRegex(IsFindMagnet, RegexOptions.ExplicitCapture, 50)]
    public static partial Regex IsFindMagnetRegex();
}
