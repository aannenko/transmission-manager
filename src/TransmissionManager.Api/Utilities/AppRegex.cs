using System.Text.RegularExpressions;

namespace TransmissionManager.Api.Utilities;

public static partial class AppRegex
{
    // language=regex
    internal const string IsCron =
        @"^((\*(\/\d{1,2})?|\d{1,2}(\/\d{1,2})?|(\d{1,2}-\d{1,2})(\/\d{1,2})?|((\d{1,2},)+\d{1,2}))\s){4}(\*(\/\d{1,2})?|\d{1,2}(\/\d{1,2})?|(\d{1,2}-\d{1,2})(\/\d{1,2})?|((\d{1,2},)+\d{1,2}))$";

    // language=regex
    internal const string IsFindMagnet = $@"^.*\(\?<{MagnetGroup}>magnet:\\\?.*\).*$";
    internal const string MagnetGroup = "magnet";

    [GeneratedRegex(IsFindMagnet, RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture, 100)]
    internal static partial Regex IsFindMagnetRegex();
}
