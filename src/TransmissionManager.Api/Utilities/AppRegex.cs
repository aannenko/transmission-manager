using System.Text.RegularExpressions;

namespace TransmissionManager.Api.Utilities;

public static partial class AppRegex
{
    // language=regex
    internal const string IsCron =
        @"^((\*(\/\d{1,2})?|\d{1,2}(\/\d{1,2})?|(\d{1,2}-\d{1,2})(\/\d{1,2})?|((\d{1,2},)+\d{1,2}))\s){4}(\*(\/\d{1,2})?|\d{1,2}(\/\d{1,2})?|(\d{1,2}-\d{1,2})(\/\d{1,2})?|((\d{1,2},)+\d{1,2}))$";

    // language=regex
    internal const string FindMagnet = $@"\(\?<{FindMagnetGroup}>magnet:\\\?.*\)";
    internal const string FindMagnetGroup = "magnet";

    [GeneratedRegex(FindMagnet, RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture, 100)]
    internal static partial Regex FindMagnetRegex();
}
