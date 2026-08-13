using TransmissionManager.TorrentSources.JsonPointer;

namespace TransmissionManager.TorrentSources.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class JsonPointerParserTests
{
    private const int _maxTokenBytes = 4096;

    private static readonly string[] _topicHashSegments = ["result", "6880555", "7"];

    private static readonly string[] _separatedSegments = ["a", "b"];

    [Test]
    public void TryParsePointer_WhenFragmentHoldsAPointer_ReturnsItsSegments()
    {
        var segments = Parse("#/result/6880555/7");

        Assert.That(segments, Is.EqualTo(_topicHashSegments));
    }

    [Test]
    public void TryParsePointer_WhenFragmentIsAbsent_Fails()
    {
        var parsed = JsonPointerParser.TryParsePointer(
            new Uri("https://source.com/doc").Fragment,
            _maxTokenBytes,
            out var segments,
            out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed, Is.False);
            Assert.That(segments, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }
    }

    [Test]
    public void TryParsePointer_WhenFragmentIsEmpty_ReturnsThePointerToTheWholeDocument()
    {
        var segments = Parse("#");

        Assert.That(segments, Is.Empty);
    }

    [Test]
    public void TryParsePointer_WhenPointerDoesNotStartWithSlash_Fails()
    {
        var parsed = JsonPointerParser.TryParsePointer(
            new Uri("https://source.com/doc#result/1").Fragment,
            _maxTokenBytes,
            out _,
            out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed, Is.False);
            Assert.That(error, Is.Not.Empty);
        }
    }

    /// <remarks>'~1' before '~0', or '~01' yields '/' instead of the literal '~1'.</remarks>
    [TestCase("#/a~1b", "a/b")]
    [TestCase("#/a~0b", "a~b")]
    [TestCase("#/~01", "~1")]
    [TestCase("#/~10", "/0")]
    [TestCase("#/~00", "~0")]
    public void TryParsePointer_WhenTokenIsEscaped_UnescapesInTheOrderTheSpecRequires(string fragment, string expected)
    {
        var segments = Parse(fragment);

        Assert.That(segments, Is.EqualTo([expected]));
    }

    /// <remarks>
    /// Several escapes in one token is what the single-escape cases cannot pin: the run between two
    /// escapes must land one character earlier per escape already made. The cases differ in the
    /// length of that run - none, one, and several - because a wrong offset can still land correctly
    /// on the shorter ones.
    /// </remarks>
    [TestCase("#/~0~1", "~/")]
    [TestCase("#/a~0b~1c", "a~b/c")]
    [TestCase("#/~0001~0", "~001~")]
    [TestCase("#/~1~1~1", "///")]
    public void TryParsePointer_WhenTokenHoldsSeveralEscapes_UnescapesEachInPlace(string fragment, string expected)
    {
        var segments = Parse(fragment);

        Assert.That(segments, Is.EqualTo([expected]));
    }

    /// <remarks>An escape at the very end leaves nothing after it to scan or copy.</remarks>
    [TestCase("#/~0", "~")]
    [TestCase("#/~1", "/")]
    [TestCase("#/ab~0", "ab~")]
    public void TryParsePointer_WhenTokenEndsWithAnEscape_UnescapesIt(string fragment, string expected)
    {
        var segments = Parse(fragment);

        Assert.That(segments, Is.EqualTo([expected]));
    }

    /// <remarks>
    /// Long runs between the escapes, so the copies around them are block copies rather than the
    /// single characters the shorter cases exercise.
    /// </remarks>
    [Test]
    public void TryParsePointer_WhenEscapedTokenIsLong_UnescapesEveryEscape()
    {
        var run = new string('a', 200);

        var segments = Parse($"#/~1{run}~0{run}~1");

        Assert.That(segments, Is.EqualTo([$"/{run}~{run}/"]));
    }

    [TestCase("#/~2")]
    [TestCase("#/a~")]
    [TestCase("#/~")]
    public void TryParsePointer_WhenEscapeIsIncomplete_Fails(string fragment)
    {
        var parsed = JsonPointerParser.TryParsePointer(
            new Uri($"https://source.com/doc{fragment}").Fragment,
            _maxTokenBytes,
            out _,
            out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed, Is.False);
            Assert.That(error, Is.Not.Empty);
        }
    }

    [TestCase("#/a%20b", "a b")]
    [TestCase("#/a b", "a b")]
    [TestCase("#/100%25", "100%")]
    [TestCase("#/%D1%82%D0%B5%D1%81%D1%82", "тест")]
    public void TryParsePointer_WhenTokenIsPercentEncoded_DecodesBeforeSplitting(string fragment, string expected)
    {
        var segments = Parse(fragment);

        Assert.That(segments, Is.EqualTo([expected]));
    }

    /// <remarks>
    /// '%2F' is a separator once decoded, so it cannot stand in for '~1'. Documented rather than
    /// corrected: the spec mandates this decoding order.
    /// </remarks>
    [Test]
    public void TryParsePointer_WhenSlashIsPercentEncoded_TreatsItAsASeparator()
    {
        var segments = Parse("#/a%2Fb");

        Assert.That(segments, Is.EqualTo(_separatedSegments));
    }

    [TestCase("0", 0)]
    [TestCase("7", 7)]
    [TestCase("10", 10)]
    [TestCase("1000000000", 1000000000)]
    [TestCase("2147483647", int.MaxValue)]
    public void TryParseAsArrayIndex_WhenTokenIsAnIndex_ReturnsIt(string segment, int expected)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(JsonPointerParser.TryParseAsArrayIndex(segment, out var index), Is.True);
            Assert.That(index, Is.EqualTo(expected));
        }
    }

    /// <remarks>
    /// The ten-digit cases pin that an index above <see cref="int.MaxValue"/> is not an index. The
    /// trailing NUL is what a fragment ending "1%00" decodes to, and is the case the digit check
    /// exists for - int.TryParse stops at a NUL and would read it as 1.
    /// </remarks>
    [TestCase("01")]
    [TestCase("007")]
    [TestCase("-1")]
    [TestCase("+1")]
    [TestCase("1e2")]
    [TestCase(" 1")]
    [TestCase("-")]
    [TestCase("")]
    [TestCase("abc")]
    [TestCase("1\u0000", TestName = "TryParseAsArrayIndex_WhenTokenIsNotAnIndex_Fails(digit then NUL)")]
    [TestCase("2147483648")]
    [TestCase("4294967295")]
    [TestCase("9999999999")]
    [TestCase("18446744073709551616")]
    public void TryParseAsArrayIndex_WhenTokenIsNotAnIndex_Fails(string segment)
    {
        Assert.That(JsonPointerParser.TryParseAsArrayIndex(segment, out _), Is.False);
    }

    private static string[] Parse(string fragment)
    {
        var parsed = JsonPointerParser.TryParsePointer(
            new Uri($"https://source.com/doc{fragment}").Fragment,
            _maxTokenBytes,
            out var segments,
            out var error);

        Assert.That(parsed, Is.True, error);
        return segments!;
    }
}
