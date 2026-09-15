using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentSources.JsonPointer;

internal static partial class JsonValueRegex
{
    /// <summary>
    /// Determines if the tested string format holds nothing but literal text and one or more
    /// <c>{0}</c> placeholders.
    /// </summary>
    // language=regex
    public const string IsJsonValueFormat = @"^[^{}]*(\{0\}[^{}]*)+$";

    [GeneratedRegex(IsJsonValueFormat, RegexOptions.ExplicitCapture, 50)]
    public static partial Regex IsJsonValueFormatRegex();
}
