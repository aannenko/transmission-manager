using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace TransmissionManager.TorrentSources.JsonPointer;

/// <summary>
/// Reads the syntax of an RFC 6901 JSON Pointer.
/// </summary>
internal static class JsonPointerParser
{
    /// <summary>
    /// What a token needs beyond its own payload before the JSON reader can complete it: two quotes
    /// for a string, and one more byte for the delimiter that ends a property name or a number.
    /// </summary>
    private const int _tokenFramingBytes = 3;

    /// <summary>
    /// Reads the RFC 6901 JSON Pointer carried by <paramref name="fragment"/> as its unescaped
    /// reference tokens, outermost first. They are empty when the pointer addresses the whole
    /// document.
    /// </summary>
    /// <param name="fragment">
    /// A URI fragment as <see cref="Uri.Fragment"/> gives it, leading '#' included.
    /// </param>
    /// <param name="maxTokenBytes">
    /// The token limit <see cref="JsonPointerResolver.ResolveAsync"/> will be given, which also
    /// bounds how long a segment may be. Both must be given the same number.
    /// </param>
    /// <param name="error">Why the fragment is not a usable pointer.</param>
    /// <remarks>
    /// Percent-decoding must run over the whole fragment before it is split: that is what makes
    /// <c>%2F</c> a separator, as RFC 6901 requires - splitting first would leave it a literal
    /// slash inside a token.
    /// </remarks>
    internal static bool TryParsePointer(
        ReadOnlySpan<char> fragment,
        int maxTokenBytes,
        [NotNullWhen(true)] out string[]? segments,
        [NotNullWhen(false)] out string? error)
    {
        segments = null;

        // Uri.Fragment is "" for "http://host/doc" but "#" for "http://host/doc#", and only the
        // latter is a pointer - to the whole document. Stripping '#' first would merge the two.
        if (fragment.Length is 0)
        {
            error = "The URI must end with a JSON Pointer in its fragment, for example '#/result/1234567/7'.";
            return false;
        }

        var unescapedFragment = Uri.UnescapeDataString(fragment[1..]);
        if (unescapedFragment.Length is 0)
        {
            segments = [];
            error = null;
            return true;
        }

        if (unescapedFragment[0] is not '/')
        {
            error = $"The JSON Pointer '{unescapedFragment}' must start with '/'.";
            return false;
        }

        var maxSegmentBytes = maxTokenBytes - _tokenFramingBytes;

        segments = new string[unescapedFragment.Count('/')];
        var segmentsAdded = 0;
        var unescapedSpan = unescapedFragment.AsSpan(1); // skip leading '/'
        foreach (var range in unescapedSpan.Split('/'))
        {
            if (!TryUnescapeToken(unescapedSpan[range], out var segment, out error))
                return false;

            var segmentBytes = Encoding.UTF8.GetByteCount(segment);
            if (segmentBytes > maxSegmentBytes)
            {
                // We'll not be able match or even hold a value this long - failing early.
                error = $"Segment {segmentsAdded + 1} of the JSON Pointer is {segmentBytes} bytes, which exceeds the " +
                    $"{maxSegmentBytes} bytes allowed by a " +
                    $"{nameof(TorrentJsonPointerClientOptions.MaxJsonTokenBytes)} of {maxTokenBytes}.";

                return false;
            }

            segments[segmentsAdded++] = segment;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Reads <paramref name="segment"/> as an array index.
    /// </summary>
    /// <remarks>
    /// RFC 6901 allows 0 or positive integers without leading zeros as array indexes.
    /// <c>int.TryParse</c> alone is not enough: it reads "01" as 1, and stops at a trailing NUL that a
    /// fragment can carry as "1%00".
    /// <para>
    /// An index greater than <see cref="int.MaxValue"/> is legal but larger than the walk can count
    /// to, so it is left unresolved rather than reported as invalid.
    /// </para>
    /// </remarks>
    internal static bool TryParseAsArrayIndex(string segment, out int index)
    {
        ArgumentNullException.ThrowIfNull(segment);

        index = 0;
        if (segment.Length is 0 || (segment.Length > 1 && segment[0] is '0'))
            return false;

        return !segment.AsSpan().ContainsAnyExceptInRange('0', '9')
            && int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out index);
    }

    /// <remarks>
    /// Each escape is decoded where it stands, so <c>~01</c> yields the literal <c>~1</c>; replacing
    /// every <c>~0</c> and then every <c>~1</c> across the whole token would yield <c>/</c> instead.
    /// <para>
    /// The writer trusts the validating pass: it reads the character after every '~' without
    /// checking that one is there, and treats anything that is not '0' as '1'.
    /// </para>
    /// </remarks>
    private static bool TryUnescapeToken(
        ReadOnlySpan<char> token,
        [NotNullWhen(true)] out string? segment,
        [NotNullWhen(false)] out string? error)
    {
        var tildeCount = 0;
        for (var rest = token; ;)
        {
            var index = rest.IndexOf('~');
            if (index < 0)
                break;

            tildeCount++;
            if (index + 1 == rest.Length || rest[index + 1] is not ('0' or '1'))
            {
                segment = null;
                error = $"'~' must be followed by '0' or '1' in the JSON Pointer token '{token}'.";
                return false;
            }

            rest = rest[(index + 2)..];
        }

        if (tildeCount is 0)
        {
            segment = token.ToString();
            error = null;
            return true;
        }

        // Each escape collapses two characters into one, so the length is known before the write.
        segment = string.Create(token.Length - tildeCount, token, static (unescaped, token) =>
        {
            var written = 0;
            var remainder = token;
            while (true)
            {
                var index = remainder.IndexOf('~');
                if (index < 0)
                    break;

                remainder[..index].CopyTo(unescaped[written..]);
                written += index;
                unescaped[written++] = remainder[index + 1] is '0' ? '~' : '/';
                remainder = remainder[(index + 2)..];
            }

            remainder.CopyTo(unescaped[written..]);
        });

        error = null;
        return true;
    }
}
