using System.Buffers;

namespace TransmissionManager.Transmission;

/// <remarks>
/// Text a server served is untrusted: a control character in it forges structure in whatever the
/// summary is embedded in, and its length is whatever the server chose to send. Both are
/// neutralised here, so a summary holds no control character and costs a bounded amount of message.
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
