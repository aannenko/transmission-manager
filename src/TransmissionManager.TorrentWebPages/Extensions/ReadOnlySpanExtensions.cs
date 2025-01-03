namespace TransmissionManager.TorrentWebPages.Extensions;

internal static class ReadOnlySpanExtensions
{
    public static int IndexOfStartOf(this Span<byte> span, ReadOnlySpan<byte> value) =>
        ((ReadOnlySpan<byte>)span).IndexOfStartOf(value);

    public static int IndexOfStartOf(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> value)
    {
        var index = span.IndexOf(value);
        if (index is not -1)
            return index;

        for (var end = span[^Math.Min(span.Length, value.Length - 1)..]; end.Length > 0; end = end[1..])
        {
            index = end.IndexOf(value[0]);
            if (index is -1)
                return -1;

            end = end[index..];
            if (end.SequenceEqual(value[..end.Length]))
                return span.Length - end.Length;
        }

        return -1;
    }
}
