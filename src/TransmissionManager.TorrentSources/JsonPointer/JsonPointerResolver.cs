using System.Buffers;
using System.Text.Json;
using Outcome = (TransmissionManager.TorrentSources.JsonPointer.JsonPointerResolution Resolution, string? Value,
    System.Text.Json.JsonValueKind ValueKind);

namespace TransmissionManager.TorrentSources.JsonPointer;

/// <summary>
/// Resolves the segments of a JSON Pointer against a JSON stream without holding the document.
/// </summary>
/// <remarks>
/// Memory is bounded by the token limit the caller sets rather than by the document, so an
/// arbitrarily large - or deliberately inflated - response cannot exhaust it. Callers are expected
/// to bound the elapsed time separately.
/// </remarks>
internal static class JsonPointerResolver
{
    /// <remarks>An info hash is forty characters, so nothing below this could ever hold one.</remarks>
    private const int _minTokenBytes = 64;

    private static readonly Outcome _notFound = (JsonPointerResolution.NotFound, null, JsonValueKind.Undefined);

    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    /// <remarks>
    /// The depth limit covers the whole document, not just the pointer's path: the reader sees every
    /// token, including those inside values that are stepped over, so a document nested deeper than
    /// this is rejected even when the addressed value is shallow. The same deliberate trade as
    /// <see cref="TorrentJsonPointerClientOptions.MaxJsonTokenBytes"/>.
    /// </remarks>
    private static readonly JsonReaderOptions _readerOptions = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 64,
    };

    /// <summary>
    /// Reads <paramref name="stream"/> until the value addressed by <paramref name="segments"/> is
    /// decided.
    /// </summary>
    /// <param name="stream">The UTF-8 JSON document to read.</param>
    /// <param name="segments">
    /// The reference tokens of the pointer, outermost first, as
    /// <see cref="JsonPointerParser.TryParsePointer"/> returns them. Empty addresses the whole
    /// document.
    /// </param>
    /// <param name="maxTokenBytes">
    /// The size of the read buffer, and so the largest JSON token the document may hold.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// How the resolution ended, the string value when it is <see cref="JsonPointerResolution.Found"/>,
    /// and the kind of the addressed value when it is <see cref="JsonPointerResolution.NotAString"/>.
    /// </returns>
    /// <exception cref="JsonException">
    /// Thrown if the document is not valid JSON, or holds a token larger than
    /// <paramref name="maxTokenBytes"/>.
    /// </exception>
    /// <remarks>
    /// Where a member occurs more than once, the first is taken; RFC 6901 leaves the choice open,
    /// and reading forwards cannot revise an answer it has already found.
    /// <para>
    /// Reading stops at the addressed value, so a document malformed only after it is still
    /// accepted: reading to the end would cost the whole document and could not improve the answer.
    /// </para>
    /// </remarks>
    public static async ValueTask<Outcome> ResolveAsync(
        Stream stream,
        string[] segments,
        int maxTokenBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTokenBytes, _minTokenBytes);

        // The rented array will likely be larger (up to the next power-of-two);
        // reading into that surplus would overflow the maxTokenBytes limit.
        var buffer = ArrayPool<byte>.Shared.Rent(maxTokenBytes);
        var walk = new PointerWalk();
        var dataLength = 0;
        var isBomChecked = false;
        try
        {
            while (true)
            {
                var read = await stream
                    .ReadAsync(buffer.AsMemory(dataLength, maxTokenBytes - dataLength), cancellationToken)
                    .ConfigureAwait(false);

                var isFinalBlock = read is 0;
                dataLength += read;

                if (!isBomChecked) // Strip BOM.
                {
                    if (dataLength >= Utf8Bom.Length) // If we've read enough to check for a BOM.
                    {
                        isBomChecked = true;
                        if (buffer.AsSpan(0, Utf8Bom.Length).SequenceEqual(Utf8Bom))
                        {
                            buffer.AsSpan(Utf8Bom.Length, dataLength - Utf8Bom.Length).CopyTo(buffer);
                            dataLength -= Utf8Bom.Length;
                        }
                    }
                    else if (isFinalBlock) // If the stream is shorter than a BOM.
                    {
                        isBomChecked = true;
                    }
                    else // Read more data to check the BOM.
                    {
                        continue;
                    }
                }

                // Utf8JsonReader cannot cross an await, so it lives inside this call and walk
                // carries its position between chunks.
                if (AdvanceJsonWalk(buffer.AsSpan(0, dataLength), isFinalBlock, segments, ref walk, out var outcome))
                    return outcome;

                // Only reached by a document the reader accepts and the walk cannot answer.
                if (isFinalBlock)
                    return _notFound;

                // A full buffer that yielded nothing holds one token too large to complete.
                if (walk.BytesConsumed is 0 && dataLength == maxTokenBytes)
                {
                    throw new JsonException(
                        $"A single JSON token exceeds the {maxTokenBytes} byte limit for reading a torrent source.");
                }

                dataLength -= walk.BytesConsumed;
                buffer.AsSpan(walk.BytesConsumed, dataLength).CopyTo(buffer);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Reads as much of <paramref name="data"/> as the reader can, moving the walk through it.
    /// </summary>
    /// <returns>Whether the walk reached a verdict, which is then in <paramref name="outcome"/>.</returns>
    private static bool AdvanceJsonWalk(
        ReadOnlySpan<byte> data,
        bool isFinalBlock,
        ReadOnlySpan<string> segments,
        ref PointerWalk walk,
        out Outcome outcome)
    {
        var reader = new Utf8JsonReader(data, isFinalBlock, walk.State);

        // Paths below that return true without setting a verdict leave the pointer unresolved.
        outcome = _notFound;

        while (reader.Read())
        {
            switch (walk.Phase)
            {
                case Phase.TakeValue:
                    if (TakeValue(ref reader, ref walk, segments, out outcome))
                        return true;

                    break;

                case Phase.ScanContainer:
                    // The segment is not in this container. Reading past its end would match inside
                    // a sibling and answer from the wrong part of the document.
                    if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
                        return true;

                    // A name only appears in an object, an element only opens in an array, so the
                    // token says which container this is - no need to remember it across a skip.
                    if (reader.TokenType is JsonTokenType.PropertyName)
                    {
                        if (NameEquals(ref reader, segments[walk.SegmentsMatched]))
                        {
                            walk.SegmentsMatched++;
                            walk.Phase = Phase.TakeValue;
                        }
                        else
                        {
                            walk.Phase = Phase.SkipValue;
                        }
                    }
                    else if (walk.ElementIndex++ != walk.WantedElementIndex)
                    {
                        BeginSkip(ref reader, ref walk);
                    }
                    else
                    {
                        // TakeValue must see this very token, so call it now - the next Read moves
                        // past it.
                        walk.SegmentsMatched++;
                        if (TakeValue(ref reader, ref walk, segments, out outcome))
                            return true;
                    }

                    break;

                case Phase.SkipValue:
                    BeginSkip(ref reader, ref walk);
                    break;

                default: // Phase.Skipping
                    ContinueSkip(ref reader, ref walk);
                    break;
            }
        }

        walk.State = reader.CurrentState;
        walk.BytesConsumed = (int)reader.BytesConsumed;
        return false;
    }

    /// <returns>Whether the walk reached a verdict, which is then in <paramref name="outcome"/>.</returns>
    /// <remarks>
    /// A value the pointer cannot descend into ends the walk. Reading past it would drift into
    /// whatever follows, where a later container can carry the remaining segments and answer in its
    /// place.
    /// </remarks>
    private static bool TakeValue(
        ref Utf8JsonReader reader,
        ref PointerWalk walk,
        ReadOnlySpan<string> segments,
        out Outcome outcome)
    {
        outcome = _notFound;

        if (walk.SegmentsMatched == segments.Length)
        {
            outcome = reader.TokenType is JsonTokenType.String
                ? (JsonPointerResolution.Found, ReadString(ref reader), JsonValueKind.String)
                : (JsonPointerResolution.NotAString, null, ToValueKind(reader.TokenType));

            return true;
        }

        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                walk.Phase = Phase.ScanContainer;
                return false;

            case JsonTokenType.StartArray:
                // A segment that cannot address an element matches nothing this array holds, so the
                // whole array can be left unread rather than counted through to its end.
                if (!JsonPointerParser.TryParseAsArrayIndex(segments[walk.SegmentsMatched], out walk.WantedElementIndex))
                    return true;

                walk.Phase = Phase.ScanContainer;
                walk.ElementIndex = 0;
                return false;

            default:
                return true;
        }
    }

    /// <remarks>
    /// A name that cannot be transcoded - an unpaired surrogate escape, for one - is not the segment
    /// being sought, because that segment is a valid string. <see cref="Utf8JsonReader.Read"/> admits
    /// such a name and <see cref="Utf8JsonReader.ValueTextEquals(string)"/> throws on it rather than
    /// answering, so the throw is the answer.
    /// </remarks>
    private static bool NameEquals(ref Utf8JsonReader reader, string segment)
    {
        try
        {
            return reader.ValueTextEquals(segment);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <exception cref="JsonException">
    /// Thrown if the string cannot be transcoded to UTF-16. An unpaired surrogate escape or invalid
    /// UTF-8 both pass <see cref="Utf8JsonReader.Read"/> and fail only here, as an
    /// <see cref="InvalidOperationException"/> that would otherwise escape the whole search. The
    /// source, not the pointer, is at fault, so it is reported as malformed JSON.
    /// </exception>
    private static string ReadString(ref Utf8JsonReader reader)
    {
        try
        {
            return reader.GetString()!;
        }
        catch (InvalidOperationException e)
        {
            throw new JsonException("A string in the torrent source could not be read as text.", e);
        }
    }

    /// <remarks>
    /// Depth is counted here rather than left to <see cref="Utf8JsonReader.TrySkip"/>, which fails
    /// whenever the value outgrows the buffer - exactly the case worth stepping over.
    /// </remarks>
    private static void BeginSkip(ref Utf8JsonReader reader, ref PointerWalk walk)
    {
        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            walk.SkipDepth = 1;
            walk.Phase = Phase.Skipping;
        }
        else
        {
            walk.Phase = Phase.ScanContainer;
        }
    }

    private static void ContinueSkip(ref Utf8JsonReader reader, ref PointerWalk walk)
    {
        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            walk.SkipDepth++;
        }
        else if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray && --walk.SkipDepth is 0)
        {
            walk.Phase = Phase.ScanContainer;
        }
    }

    private static JsonValueKind ToValueKind(JsonTokenType tokenType) => tokenType switch
    {
        JsonTokenType.String => JsonValueKind.String,
        JsonTokenType.Number => JsonValueKind.Number,
        JsonTokenType.True => JsonValueKind.True,
        JsonTokenType.False => JsonValueKind.False,
        JsonTokenType.Null => JsonValueKind.Null,
        JsonTokenType.StartObject => JsonValueKind.Object,
        JsonTokenType.StartArray => JsonValueKind.Array,
        _ => JsonValueKind.Undefined,
    };

    /// <summary>
    /// What the token the reader is positioned on means to the walk.
    /// </summary>
    /// <remarks>
    /// <see cref="Phase.TakeValue"/> is first so that a walk starts there, on the root value.
    /// </remarks>
    private enum Phase
    {
        /// <summary>Opens the addressed value, or the one the next segment is looked for inside.</summary>
        TakeValue,

        /// <summary>
        /// Names a member of, or opens an element of, the container a segment named - or ends it.
        /// </summary>
        ScanContainer,

        /// <summary>Opens a value the pointer does not enter.</summary>
        SkipValue,

        /// <summary>Lies within a value the pointer does not enter.</summary>
        Skipping,
    }

    /// <summary>
    /// The position within the document that has to survive a buffer refill.
    /// </summary>
    private struct PointerWalk()
    {
        public JsonReaderState State = new(_readerOptions);

        public int BytesConsumed;

        public Phase Phase;

        /// <summary>
        /// How many leading segments of the pointer have been matched.
        /// Also the index of the segment being looked for.
        /// </summary>
        public int SegmentsMatched;

        /// <summary>Which element of the array being counted comes next.</summary>
        public int ElementIndex;

        public int WantedElementIndex;

        /// <summary>How many containers deep the value being stepped over is still open.</summary>
        public int SkipDepth;
    }
}
