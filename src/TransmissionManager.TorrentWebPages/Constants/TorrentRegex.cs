using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentWebPages.Constants;

public static partial class TorrentRegex
{
    // language=regex
    public const string IsFindMagnet = $@"^.*\(\?<{MagnetGroup}>magnet:\\\?.*?\).*$";
    public const string MagnetGroup = "magnet";

    [GeneratedRegex(IsFindMagnet, RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture, 50)]
    public static partial Regex IsFindMagnetRegex();
}
