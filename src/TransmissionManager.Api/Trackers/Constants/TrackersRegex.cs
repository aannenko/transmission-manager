using System.Text.RegularExpressions;

namespace TransmissionManager.Api.Trackers.Constants;

public static partial class TrackersRegex
{
    // language=regex
    internal const string IsFindMagnet = $@"^.*\(\?<{MagnetGroup}>magnet:\\\?.*\).*$";
    internal const string MagnetGroup = "magnet";

    [GeneratedRegex(IsFindMagnet, RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture, 100)]
    internal static partial Regex IsFindMagnetRegex();
}
