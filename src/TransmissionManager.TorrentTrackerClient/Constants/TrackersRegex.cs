using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentTrackerClient.Constants;

public static partial class TrackersRegex
{
    // language=regex
    public const string IsFindMagnet = $@"^.*\(\?<{MagnetGroup}>magnet:\\\?.*\).*$";
    public const string MagnetGroup = "magnet";

    [GeneratedRegex(IsFindMagnet, RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture, 100)]
    public static partial Regex IsFindMagnetRegex();
}
