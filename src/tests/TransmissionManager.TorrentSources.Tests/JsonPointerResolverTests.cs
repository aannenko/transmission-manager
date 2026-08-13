using System.Text;
using System.Text.Json;
using TransmissionManager.TorrentSources.JsonPointer;

namespace TransmissionManager.TorrentSources.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class JsonPointerResolverTests
{
    private const int _maxTokenBytes = 4096;

    private const string _hash = "36B04E5B0123456789ABCDEF0123456789AB46FF";

    private const string _payload = $$"""
        {
          "format": { "topic_id": ["tor_status", "seeders", "info_hash"] },
          "update_time": 1785441955,
          "result": {
            "6880554": [0, 12, "0000000000000000000000000000000000000000"],
            "6880555": [0, 50, "{{_hash}}"],
            "6880556": [0, 7, null]
          }
        }
        """;

    /// <remarks>
    /// The chunk sizes matter more than the pointer: a stream that answers in full never exercises
    /// the refill and compaction path.
    /// </remarks>
    [TestCase(1)]
    [TestCase(7)]
    [TestCase(64)]
    [TestCase(int.MaxValue)]
    public async Task ResolveAsync_WhenPointerAddressesAString_ReturnsIt(int chunkSize)
    {
        var (resolution, value, _) = await ResolveAsync(_payload, "#/result/6880555/2", chunkSize).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.Found));
            Assert.That(value, Is.EqualTo(_hash));
        }
    }

    [Test]
    public async Task ResolveAsync_WhenPointerIsEmpty_AddressesTheWholeDocument()
    {
        var (resolution, value, _) = await ResolveAsync("\"just-a-string\"", "#").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.Found));
            Assert.That(value, Is.EqualTo("just-a-string"));
        }
    }

    [TestCase("#/result/9999999/2", TestName =
        "ResolveAsync_WhenPointerDoesNotAddressAValue_ReturnsNotFound(member absent)")]
    [TestCase("#/missing", TestName =
        "ResolveAsync_WhenPointerDoesNotAddressAValue_ReturnsNotFound(member absent at the root)")]
    [TestCase("#/result/6880555/9", TestName =
        "ResolveAsync_WhenPointerDoesNotAddressAValue_ReturnsNotFound(index out of range)")]
    [TestCase("#/result/6880555/3", TestName =
        "ResolveAsync_WhenPointerDoesNotAddressAValue_ReturnsNotFound(index one past the last element)")]
    [TestCase("#/result/6880555/02", TestName =
        "ResolveAsync_WhenPointerDoesNotAddressAValue_ReturnsNotFound(index with a leading zero)")]
    [TestCase("#/result/6880555/seeders", TestName =
        "ResolveAsync_WhenPointerDoesNotAddressAValue_ReturnsNotFound(non-numeric token on an array)")]
    [TestCase("#/result/6880555/-", TestName =
        "ResolveAsync_WhenPointerDoesNotAddressAValue_ReturnsNotFound(the end-of-array token)")]
    [TestCase("#/update_time/0", TestName =
        "ResolveAsync_WhenPointerDoesNotAddressAValue_ReturnsNotFound(walks into a scalar)")]
    public async Task ResolveAsync_WhenPointerDoesNotAddressAValue_ReturnsNotFound(string fragment)
    {
        var (resolution, value, valueKind) = await ResolveAsync(_payload, fragment).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.NotFound));
            Assert.That(value, Is.Null);
            Assert.That(valueKind, Is.EqualTo(JsonValueKind.Undefined));
        }
    }

    [TestCase("#/result/6880555/1", JsonValueKind.Number, TestName =
        "ResolveAsync_WhenPointerAddressesSomethingOtherThanAString_ReturnsNotAStringWithItsKind(a number)")]
    [TestCase("#/result/6880555", JsonValueKind.Array, TestName =
        "ResolveAsync_WhenPointerAddressesSomethingOtherThanAString_ReturnsNotAStringWithItsKind(an array)")]
    [TestCase("#/result", JsonValueKind.Object, TestName =
        "ResolveAsync_WhenPointerAddressesSomethingOtherThanAString_ReturnsNotAStringWithItsKind(an object)")]
    [TestCase("#/result/6880556/2", JsonValueKind.Null, TestName =
        "ResolveAsync_WhenPointerAddressesSomethingOtherThanAString_ReturnsNotAStringWithItsKind(a null)")]
    public async Task ResolveAsync_WhenPointerAddressesSomethingOtherThanAString_ReturnsNotAStringWithItsKind(
        string fragment,
        JsonValueKind expectedKind)
    {
        var (resolution, value, valueKind) = await ResolveAsync(_payload, fragment).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.NotAString));
            Assert.That(value, Is.Null);
            Assert.That(valueKind, Is.EqualTo(expectedKind));
        }
    }

    [TestCase(1)]
    [TestCase(int.MaxValue)]
    public async Task ResolveAsync_WhenALaterSiblingRepeatsTheName_DoesNotResolveAgainstIt(int chunkSize)
    {
        const string document = """
            { "result": { "111": ["nope"] }, "backup": { "6880555": ["wrong-answer"] } }
            """;

        var (resolution, value, _) = await ResolveAsync(document, "#/result/6880555/0", chunkSize).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.NotFound));
            Assert.That(value, Is.Null);
        }
    }

    [Test]
    public async Task ResolveAsync_WhenALaterSiblingArrayCouldContinueTheCount_DoesNotResolveAgainstIt()
    {
        const string document = """
            { "wanted": [0, 1], "other": [2, 3, 4, 5, 6, 7, "wrong-answer"] }
            """;

        var (resolution, _, _) = await ResolveAsync(document, "#/wanted/6").ConfigureAwait(false);

        Assert.That(resolution, Is.EqualTo(JsonPointerResolution.NotFound));
    }

    /// <remarks>
    /// A skipped value's depth is counted as it is read, not compared against one recorded when the
    /// skip began, so the small chunk sizes carry that count across refills.
    /// </remarks>
    [TestCase(1)]
    [TestCase(7)]
    [TestCase(int.MaxValue)]
    public async Task ResolveAsync_WhenSkippedValueNestsContainers_StepsOverAllOfThem(int chunkSize)
    {
        const string document = """
            { "skipped": [[1, 2, [3, { "z": [9, 9] }]], { "a": [] }], "wanted": "found-it" }
            """;

        var (resolution, value, _) = await ResolveAsync(document, "#/wanted", chunkSize).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.Found));
            Assert.That(value, Is.EqualTo("found-it"));
        }
    }

    /// <remarks>Chunk sizes 1 and 2 split the mark itself across a refill.</remarks>
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(int.MaxValue)]
    public async Task ResolveAsync_WhenDocumentStartsWithAByteOrderMark_ReadsItAnyway(int chunkSize)
    {
        var bytes = new List<byte>([0xEF, 0xBB, 0xBF]);
        bytes.AddRange(Encoding.UTF8.GetBytes(_payload));

        var (resolution, value, _) = await ResolveAsync([.. bytes], "#/result/6880555/2", chunkSize)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.Found));
            Assert.That(value, Is.EqualTo(_hash));
        }
    }

    /// <remarks>
    /// A body this short cannot hold a mark, so it has to reach the reader as it is rather than be
    /// held back for bytes that will never arrive. Every case here is one or two bytes.
    /// </remarks>
    [TestCase("1", JsonPointerResolution.NotAString, null)]
    [TestCase("{}", JsonPointerResolution.NotAString, null)]
    [TestCase("[]", JsonPointerResolution.NotAString, null)]
    [TestCase("\"\"", JsonPointerResolution.Found, "", TestName =
        "ResolveAsync_WhenDocumentIsShorterThanAByteOrderMark_StillReadsIt(an empty JSON string)")]
    public async Task ResolveAsync_WhenDocumentIsShorterThanAByteOrderMark_StillReadsIt(
        string document,
        JsonPointerResolution expectedResolution,
        string? expectedValue)
    {
        var (resolution, value, _) = await ResolveAsync(document, "#").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(expectedResolution));
            Assert.That(value, Is.EqualTo(expectedValue));
        }
    }

    /// <remarks>
    /// The mark-only body strips to nothing but leaves the mark in the pooled buffer, so the empty
    /// body that follows rents it back with those bytes still in place. Comparing more bytes than
    /// were read would see them, take the empty body for a marked one, and slice a negative length -
    /// an <see cref="ArgumentOutOfRangeException"/> in place of the <see cref="JsonException"/> an
    /// empty body owes. Both run through one test so they share the pool bucket, as two requests do.
    /// </remarks>
    [Test]
    public void ResolveAsync_WhenAMarkOnlyDocumentPrecedesAnEmptyOne_ReportsBothAsMalformedJson()
    {
        Assert.That(
            async () => await ResolveAsync([0xEF, 0xBB, 0xBF], "#/result").ConfigureAwait(false),
            Throws.InstanceOf<JsonException>());

        Assert.That(
            async () => await ResolveAsync([], "#/result").ConfigureAwait(false),
            Throws.InstanceOf<JsonException>());
    }

    /// <remarks>
    /// <see cref="Utf8JsonReader.Read"/> admits these documents; only decoding the text rejects
    /// them, and it does so with an <see cref="InvalidOperationException"/> that would escape the
    /// whole search. Reported as malformed JSON because the source, not the pointer, is at fault.
    /// </remarks>
    [TestCase("""{ "a": "\uD800" }""", TestName =
        "ResolveAsync_WhenAddressedStringCannotBeDecoded_Throws(unpaired surrogate escape)")]
    public void ResolveAsync_WhenAddressedStringCannotBeDecoded_Throws(string document)
    {
        Assert.That(
            async () => await ResolveAsync(document, "#/a").ConfigureAwait(false),
            Throws.TypeOf<JsonException>());
    }

    /// <remarks>
    /// The name cannot be decoded, so it cannot be the segment, which is a valid string. Comparing
    /// it throws rather than answering, and the walk has to read that as 'not a match' and carry on
    /// to the member that does match.
    /// </remarks>
    [Test]
    public async Task ResolveAsync_WhenAnEarlierMemberNameCannotBeDecoded_StillMatchesALaterOne()
    {
        const string document = """{ "\uD800": "skip me", "a": "found-it" }""";

        var (resolution, value, _) = await ResolveAsync(document, "#/a").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.Found));
            Assert.That(value, Is.EqualTo("found-it"));
        }
    }

    /// <remarks>
    /// A name may be written with JSON escapes, so it has to be compared decoded rather than as
    /// the raw bytes between the quotes. The small chunk size splits the escape itself across a
    /// refill, where only the reader's retained state can complete it.
    /// </remarks>
    [TestCase(1)]
    [TestCase(3)]
    [TestCase(int.MaxValue)]
    public async Task ResolveAsync_WhenMemberNameIsJsonEscaped_StillMatchesIt(int chunkSize)
    {
        const string document = """{ "\u0072esult": { "1": ["found-it"] } }""";

        var (resolution, value, _) = await ResolveAsync(document, "#/result/1/0", chunkSize).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.Found));
            Assert.That(value, Is.EqualTo("found-it"));
        }
    }

    /// <remarks>Chunk size 1 splits every multi-byte character across a refill.</remarks>
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(int.MaxValue)]
    public async Task ResolveAsync_WhenNamesAndValuesAreNotAscii_ResolvesAcrossRefills(int chunkSize)
    {
        const string document = """{ "имя": { "ключ": ["значение-é-😀"] } }""";

        var (resolution, value, _) = await ResolveAsync(document, "#/имя/ключ/0", chunkSize).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.Found));
            Assert.That(value, Is.EqualTo("значение-é-😀"));
        }
    }

    /// <remarks>RFC 6901 leaves this open; unlike JsonDocument, which keeps the last.</remarks>
    [Test]
    public async Task ResolveAsync_WhenMemberIsDuplicated_TakesTheFirst()
    {
        const string document = """{ "a": "first", "a": "second" }""";

        var (_, value, _) = await ResolveAsync(document, "#/a").ConfigureAwait(false);

        Assert.That(value, Is.EqualTo("first"));
    }

    [TestCase("{ \"a\": ", TestName = "ResolveAsync_WhenDocumentIsNotUsableJson_Throws(truncated)")]
    [TestCase("", TestName = "ResolveAsync_WhenDocumentIsNotUsableJson_Throws(empty)")]
    [TestCase("<html>not json</html>", TestName = "ResolveAsync_WhenDocumentIsNotUsableJson_Throws(not json at all)")]
    [TestCase("{ \"b\": 1, }", TestName = "ResolveAsync_WhenDocumentIsNotUsableJson_Throws(trailing comma)")]
    [TestCase("{ /* comment */ \"b\": 1 }", TestName = "ResolveAsync_WhenDocumentIsNotUsableJson_Throws(comment)")]
    public void ResolveAsync_WhenDocumentIsNotUsableJson_Throws(string document)
    {
        Assert.That(
            async () => await ResolveAsync(document, "#/a").ConfigureAwait(false),
            Throws.InstanceOf<JsonException>());
    }

    /// <remarks>Safe because the value is validated as an info hash regardless.</remarks>
    [Test]
    public async Task ResolveAsync_WhenDocumentIsMalformedAfterTheValue_StillReturnsTheValue()
    {
        const string document = """{ "a": "found-it", } and then some nonsense""";

        var (resolution, value, _) = await ResolveAsync(document, "#/a").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.Found));
            Assert.That(value, Is.EqualTo("found-it"));
        }
    }

    /// <remarks>
    /// The limit is the whole memory a search may occupy, so a token that fills it exactly must
    /// still be read - both when it is stepped over and when it is the answer. Two of the limit's
    /// bytes go to the quotes around the value.
    /// </remarks>
    [TestCase(1)]
    [TestCase(512)]
    [TestCase(int.MaxValue)]
    public async Task ResolveAsync_WhenTokenFillsTheLimitExactly_ReadsIt(int chunkSize)
    {
        var large = new string('a', _maxTokenBytes - 2);
        var document = $$"""{"padding":"{{large}}","wanted":"{{large}}"}""";

        var (resolution, value, _) = await ResolveAsync(document, "#/wanted", chunkSize).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.Found));
            Assert.That(value, Is.EqualTo(large));
        }
    }

    /// <remarks>
    /// The pool rounds a rent up to a power-of-two bucket, so 5000 bytes is served by an 8192-byte
    /// array; reading into that surplus would quietly raise the configured limit. The message is
    /// asserted because the reader would fail on the incomplete token either way - only the
    /// explicit check names the limit that stopped it.
    /// </remarks>
    [Test]
    public void ResolveAsync_WhenTokenExceedsALimitThePoolRoundsUp_ThrowsNamingTheLimit()
    {
        const int maxTokenBytes = 5000;
        var document = $$"""{"wanted":"{{new string('a', maxTokenBytes)}}"}""";

        Assert.That(
            async () => await ResolveAsync(document, "#/wanted", int.MaxValue, maxTokenBytes).ConfigureAwait(false),
            Throws.InstanceOf<JsonException>().With.Message.Contains($"{maxTokenBytes} byte limit"));
    }

    /// <remarks>
    /// A token above the limit ends the search even when the pointer does not address it: the
    /// reader cannot step over a value it cannot hold. The deliberate price of a known memory bound.
    /// </remarks>
    [TestCase("#/wanted", TestName = "ResolveAsync_WhenTokenExceedsTheLimit_Throws(the addressed value)")]
    [TestCase("#/other", TestName =
        "ResolveAsync_WhenTokenExceedsTheLimit_Throws(a value the pointer does not address)")]
    public void ResolveAsync_WhenTokenExceedsTheLimit_Throws(string fragment)
    {
        var document = $$"""{"wanted":"{{new string('a', _maxTokenBytes - 1)}}","other":"x"}""";

        Assert.That(
            async () => await ResolveAsync(document, fragment).ConfigureAwait(false),
            Throws.InstanceOf<JsonException>().With.Message.Contains($"{_maxTokenBytes} byte limit"));
    }

    /// <remarks>
    /// An element that is itself a container has its opening token consumed while being counted,
    /// so entering it cannot wait for the pass that ordinarily recognises a container.
    /// </remarks>
    [TestCase("{ \"a\": [[\"1\",\"2\"],[\"x\",\"y\"]] }", "#/a/1/0", "x", TestName =
        "ResolveAsync_WhenContainersAreNested_ResolvesThroughThem(array inside an array)")]
    [TestCase("{ \"a\": [{\"b\":\"x\"}] }", "#/a/0/b", "x", TestName =
        "ResolveAsync_WhenContainersAreNested_ResolvesThroughThem(object inside an array)")]
    [TestCase("{ \"a\": [[[\"deep\"]]] }", "#/a/0/0/0", "deep", TestName =
        "ResolveAsync_WhenContainersAreNested_ResolvesThroughThem(three arrays deep)")]
    [TestCase("[\"zero\",\"one\"]", "#/1", "one", TestName =
        "ResolveAsync_WhenContainersAreNested_ResolvesThroughThem(array at the root)")]
    [TestCase("{ \"\": { \"x\": \"found\" } }", "#//x", "found", TestName =
        "ResolveAsync_WhenContainersAreNested_ResolvesThroughThem(empty member name)")]
    [TestCase("{ \"a\": { \"\": \"found\" } }", "#/a/", "found", TestName =
        "ResolveAsync_WhenContainersAreNested_ResolvesThroughThem(empty member name last)")]
    public async Task ResolveAsync_WhenContainersAreNested_ResolvesThroughThem(
        string document,
        string fragment,
        string expected)
    {
        var (resolution, value, _) = await ResolveAsync(document, fragment, 1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.Found));
            Assert.That(value, Is.EqualTo(expected));
        }
    }

    [TestCase("{ \"a\": [\"only\"] }", "#/a/1/0", TestName =
        "ResolveAsync_WhenNestedContainerDoesNotHoldTheIndex_ReturnsNotFound(index out of range mid-path)")]
    [TestCase("{ \"a\": [[\"x\"]] }", "#/a/0/9", TestName =
        "ResolveAsync_WhenNestedContainerDoesNotHoldTheIndex_ReturnsNotFound(index out of range in a nested array)")]
    public async Task ResolveAsync_WhenNestedContainerDoesNotHoldTheIndex_ReturnsNotFound(
        string document,
        string fragment)
    {
        var (resolution, _, _) = await ResolveAsync(document, fragment, 1).ConfigureAwait(false);

        Assert.That(resolution, Is.EqualTo(JsonPointerResolution.NotFound));
    }

    /// <remarks>The failure guarded against is a confident wrong answer from a later container.</remarks>
    [TestCase("{ \"a\": 0, \"other\": { \"b\": \"wrong\" } }", "#/a/b", TestName =
        "ResolveAsync_WhenPointerContinuesPastAValueWithNoChildren_ReturnsNotFound(past a number)")]
    [TestCase("{ \"a\": null, \"other\": { \"b\": \"wrong\" } }", "#/a/b", TestName =
        "ResolveAsync_WhenPointerContinuesPastAValueWithNoChildren_ReturnsNotFound(past a null)")]
    [TestCase("{ \"a\": \"s\", \"other\": { \"b\": \"wrong\" } }", "#/a/b", TestName =
        "ResolveAsync_WhenPointerContinuesPastAValueWithNoChildren_ReturnsNotFound(past a string)")]
    [TestCase("{ \"a\": [0, { \"b\": \"wrong\" }] }", "#/a/0/b", TestName =
        "ResolveAsync_WhenPointerContinuesPastAValueWithNoChildren_ReturnsNotFound(past a number in an array)")]
    [TestCase("{ \"a\": true, \"other\": { \"b\": \"wrong\" } }", "#/a/b", TestName =
        "ResolveAsync_WhenPointerContinuesPastAValueWithNoChildren_ReturnsNotFound(past a boolean)")]
    public async Task ResolveAsync_WhenPointerContinuesPastAValueWithNoChildren_ReturnsNotFound(
        string document,
        string fragment)
    {
        var (resolution, value, _) = await ResolveAsync(document, fragment, 1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.NotFound));
            Assert.That(value, Is.Null);
        }
    }

    /// <remarks>
    /// The depth limit covers the whole document, not only the path the pointer follows, so a
    /// value that is merely stepped over can exhaust it. The pair pins where that boundary sits.
    /// </remarks>
    [Test]
    public async Task ResolveAsync_WhenSkippedValueIsAtTheDeepestAllowedNesting_StillResolves()
    {
        var document = BuildNestedSkipDocument(63);

        var (resolution, value, _) = await ResolveAsync(document, "#/wanted", 1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution, Is.EqualTo(JsonPointerResolution.Found));
            Assert.That(value, Is.EqualTo("found-it"));
        }
    }

    [Test]
    public void ResolveAsync_WhenSkippedValueIsNestedTooDeeply_Throws()
    {
        var document = BuildNestedSkipDocument(64);

        Assert.That(
            async () => await ResolveAsync(document, "#/wanted").ConfigureAwait(false),
            Throws.InstanceOf<JsonException>());
    }

    /// <returns>
    /// A document whose first member nests <paramref name="nesting"/> arrays, none of which the
    /// pointer enters, followed by the member it does address.
    /// </returns>
    private static string BuildNestedSkipDocument(int nesting) =>
        $$"""{"junk":{{new string('[', nesting)}}1{{new string(']', nesting)}},"wanted":"found-it"}""";

    private static Task<(JsonPointerResolution Resolution, string? Value, JsonValueKind ValueKind)> ResolveAsync(
        string document,
        string fragment,
        int chunkSize = int.MaxValue,
        int maxTokenBytes = _maxTokenBytes) =>
        ResolveAsync(Encoding.UTF8.GetBytes(document), fragment, chunkSize, maxTokenBytes);

    private static async Task<(JsonPointerResolution Resolution, string? Value, JsonValueKind ValueKind)> ResolveAsync(
        byte[] document,
        string fragment,
        int chunkSize = int.MaxValue,
        int maxTokenBytes = _maxTokenBytes)
    {
        var parsed = JsonPointerParser.TryParsePointer(
            new Uri($"https://source.com/doc{fragment}").Fragment,
            _maxTokenBytes,
            out var segments,
            out var error);

        Assert.That(parsed, Is.True, error);

        using var stream = new ChunkedStream(document, chunkSize);
        return await JsonPointerResolver.ResolveAsync(stream, segments!, maxTokenBytes).ConfigureAwait(false);
    }

    /// <remarks>
    /// Answers at most <paramref name="chunkSize"/> bytes per read, so that the refill and
    /// compaction path is exercised rather than skipped by a stream that answers in full.
    /// </remarks>
    private sealed class ChunkedStream(byte[] data, int chunkSize) : Stream
    {
        private int _position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => data.Length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var count = Math.Min(Math.Min(chunkSize, data.Length - _position), buffer.Length);
            data.AsSpan(_position, count).CopyTo(buffer.Span);
            _position += count;
            return ValueTask.FromResult(count);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
