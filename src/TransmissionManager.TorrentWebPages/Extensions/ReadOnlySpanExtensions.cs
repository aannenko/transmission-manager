﻿namespace TransmissionManager.TorrentWebPages.Extensions;

public static class ReadOnlySpanExtensions
{
    public static int IndexOfStartOf(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> value)
    {
        var index = span.IndexOf(value);
        if (index is not -1)
            return index;

        for (int valueLength = Math.Min(span.Length, value.Length - 1); valueLength > 0; valueLength--)
            if (span.EndsWith(value[..valueLength]))
                return span.Length - valueLength;

        return -1;
    }
}