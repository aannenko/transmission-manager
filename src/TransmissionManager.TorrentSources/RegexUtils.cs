using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentSources;

/// <remarks>
/// <see cref="RegexOptions.ExplicitCapture"/> is for performance: neither source reads a capture, so
/// a plain <c>(…)</c> in an operator's pattern groups without the engine collecting anything for it.
/// </remarks>
internal static class RegexUtils
{
    /// <summary>
    /// The options every pattern either source matches with is built with.
    /// </summary>
    /// <remarks>
    /// The options decide what parses, so the API checks a pattern with these same ones - otherwise
    /// a pattern accepted there could still be refused when it is used.
    /// </remarks>
    public const RegexOptions PatternOptions = RegexOptions.ExplicitCapture;

    /// <summary>
    /// The longest pattern either source builds, in characters.
    /// </summary>
    /// <remarks>
    /// Building a regular expression takes longer the longer its pattern is and cannot be cancelled,
    /// so a configured default is held to this as much as a torrent's own pattern is. The API keeps
    /// its own copy of the number, since it has to refuse an over-long pattern before storing one.
    /// </remarks>
    public const int MaxPatternLength = 512;

    /// <summary>
    /// Builds a pattern that will be held for the lifetime of the Regex object and matched repeatedly.
    /// </summary>
    /// <remarks>
    /// Compiling emits code, which costs on the order of a millisecond and tens of kilobytes that
    /// are never reclaimed - hundreds of times what an interpreted pattern costs to build, and only
    /// worth it when the same pattern goes on to serve many matches.
    /// </remarks>
    public static Regex CreateCompiledRegex(string pattern, TimeSpan matchTimeout) =>
        new(pattern, RegexOptions.Compiled | PatternOptions, matchTimeout);
}
