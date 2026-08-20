using System.Text.RegularExpressions;

namespace TransmissionManager.TorrentSources;

/// <remarks>
/// <see cref="RegexOptions.ExplicitCapture"/> is for performance: neither source reads a capture, so
/// a plain <c>(…)</c> in an operator's pattern groups without the engine collecting anything for it.
/// </remarks>
internal static class RegexUtils
{
    /// <summary>
    /// Builds a pattern that will be matched once and thrown away.
    /// </summary>
    public static Regex CreateInterpretedRegex(string pattern, TimeSpan matchTimeout) =>
        new(pattern, RegexOptions.ExplicitCapture, matchTimeout);

    /// <summary>
    /// Builds a pattern that will be held for the lifetime of the process and matched repeatedly.
    /// </summary>
    /// <remarks>
    /// Compiling emits code, which costs on the order of a millisecond and tens of kilobytes that
    /// are never reclaimed - hundreds of times what an interpreted pattern costs to build, and only
    /// worth it when the same pattern goes on to serve many matches.
    /// </remarks>
    public static Regex CreateCompiledRegex(string pattern, TimeSpan matchTimeout) =>
        new(pattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeout);
}
