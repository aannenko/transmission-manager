using System.Buffers;

namespace TransmissionManager.TorrentSources;

/// <remarks>
/// Whatever a source served is untrusted, and an error message quoting it reaches two places that
/// treat it as structure rather than data: a log line, where a newline forges a record and an escape
/// sequence reaches the operator's terminal raw on Linux, and an HTTP response body, whose size is
/// otherwise bounded only by the read buffer. Text that came from a request, from configuration or
/// from this project's own vocabulary needs none of this.
/// </remarks>
internal static class RemoteTextUtils
{
    public const int DefaultSummaryLength = 80;
    private const int _maxSummaryLength = 100;
    private const string _ellipsis = "...";
    private const char _placeholder = '_';

    private static readonly SearchValues<char> _controlChars = SearchValues.Create(
        '\0', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007',
        '\u0008', '\u0009', '\n', '\u000B', '\u000C', '\r', '\u000E', '\u000F',
        '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016', '\u0017',
        '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D', '\u001E', '\u001F',
        '\u007F', '\u0080', '\u0081', '\u0082', '\u0083', '\u0084', '\u0085', '\u0086',
        '\u0087', '\u0088', '\u0089', '\u008A', '\u008B', '\u008C', '\u008D', '\u008E',
        '\u008F', '\u0090', '\u0091', '\u0092', '\u0093', '\u0094', '\u0095', '\u0096',
        '\u0097', '\u0098', '\u0099', '\u009A', '\u009B', '\u009C', '\u009D', '\u009E',
        '\u009F');

    /// <summary>
    /// Summarizes text a source served, for quoting in an error message.
    /// </summary>
    /// <param name="value">The text as the source served it.</param>
    /// <param name="maxLength">The maximum number of characters to include in the summary.</param>
    /// <returns>
    /// The first <paramref name="maxLength"/> characters of <paramref name="value"/> at most - one
    /// fewer when the cut would strand the high half of a surrogate pair - each control character
    /// among them replaced by <c>_</c>, followed by an ellipsis if <paramref name="value"/> was
    /// longer than that.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxLength"/> is not between 1 and <see cref="_maxSummaryLength"/>.
    /// The upper bound is not decoration: the summary is built in a buffer of that size on the
    /// stack, and a stack overflow can be neither caught nor logged.
    /// </exception>
    /// <remarks>
    /// The ellipsis is present at the end of the returned summary only if the value was truncated.
    /// </remarks>
    public static string Summarize(ReadOnlySpan<char> value, int maxLength = DefaultSummaryLength)
    {
        if (maxLength is < 1 or > _maxSummaryLength)
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, $"Must be between 1 and {_maxSummaryLength}.");

        var slice = value.Length <= maxLength ? value : value[..maxLength];

        if (slice.IsEmpty)
            return string.Empty;

        if (char.IsHighSurrogate(slice[^1]))
            slice = slice[..^1];

        Span<char> buffer = stackalloc char[maxLength];
        slice.ReplaceAny(buffer, _controlChars, _placeholder);
        var summary = buffer[..slice.Length];

        return value.Length <= maxLength
            ? summary.ToString()
            : string.Concat(summary, _ellipsis);
    }
}
