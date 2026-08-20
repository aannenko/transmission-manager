using System.Globalization;
using System.Text;
using TransmissionManager.TorrentSources.JsonPointer;

namespace TransmissionManager.TorrentSources.Tests;

/// <remarks>
/// <c>TorrentJsonPointerClient</c> hands a format straight to <see cref="CompositeFormat"/> with a
/// single argument, having checked nothing but
/// <see cref="JsonValueRegex.IsJsonValueFormat"/> first. That is only safe while the pattern refuses
/// every format composite formatting could choke on or amplify, so these tests pin the pattern
/// itself rather than the client: the sweep fails if anyone widens it, whatever they widen it to,
/// and the cases name the specific inputs that made the pattern necessary.
/// </remarks>
internal sealed class JsonValueFormatSafetyTests
{
    private const string _hash = "3a8151e8fd4ff37cd2acbcfd6e5f7d1c1ba1e00c";

    private static bool IsAccepted(string format) => JsonValueRegex.IsJsonValueFormatRegex().IsMatch(format);

    [TestCase("magnet:?xt=urn:btih:{1}")]
    [TestCase("magnet:?xt=urn:btih:{0}&x={1}")]
    public void IsJsonValueFormat_WhenFormatAsksForASecondArgument_RejectsIt(string format)
    {
        // Were it accepted, CompositeFormat.Parse would succeed and only string.Format would fail -
        // at fetch time, on every refresh, for as long as the format stays stored.
        var parsed = CompositeFormat.Parse(format);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.MinimumArgumentCount, Is.GreaterThan(1));
            Assert.That(() => string.Format(CultureInfo.InvariantCulture, parsed, _hash), Throws.TypeOf<FormatException>());
            Assert.That(IsAccepted(format), Is.False);
        }
    }

    [TestCase("{0,1000000}")]
    [TestCase("magnet:?xt=urn:btih:{0,-1000000}")]
    public void IsJsonValueFormat_WhenFormatAlignsTheValue_RejectsIt(string format)
    {
        // Alignment is the amplifier: forty characters of hash become a megabyte of padding, per
        // refresh, on a machine chosen for being small.
        var formatted = string.Format(CultureInfo.InvariantCulture, CompositeFormat.Parse(format), _hash);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(formatted, Has.Length.GreaterThanOrEqualTo(1_000_000));
            Assert.That(IsAccepted(format), Is.False);
        }
    }

    [TestCase("magnet:?xt=urn:btih:{bad}")]
    [TestCase("magnet:?xt=urn:btih:{0}&y={bad}")]
    [TestCase("magnet:?xt=urn:btih:{")]
    [TestCase("magnet:?xt=urn:btih:}")]
    public void IsJsonValueFormat_WhenFormatHoldsAnUnusableBrace_RejectsIt(string format)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => CompositeFormat.Parse(format), Throws.TypeOf<FormatException>());
            Assert.That(IsAccepted(format), Is.False);
        }
    }

    [TestCase("magnet:?xt=urn:btih:")]
    [TestCase("")]
    public void IsJsonValueFormat_WhenFormatNeverUsesTheValue_RejectsIt(string format)
    {
        // A format that drops the value builds the same magnet for every torrent, which the caller
        // would see as one source hijacking another rather than as a bad format.
        Assert.That(IsAccepted(format), Is.False);
    }

    [TestCase("{0}")]
    [TestCase("magnet:?xt=urn:btih:{0}")]
    [TestCase("magnet:?xt=urn:btih:{0}&tr=udp://tracker.example:1337")]
    [TestCase("{0}{0}")]
    public void IsJsonValueFormat_WhenFormatOnlySubstitutesTheValue_AcceptsIt(string format)
    {
        Assert.That(IsAccepted(format), Is.True);
    }

    /// <remarks>
    /// The invariant the client depends on, asserted over everything the pattern accepts instead of
    /// over a list someone could widen alongside it: the format takes exactly one argument, and
    /// substituting it adds only the value's own length.
    /// </remarks>
    [Test]
    public void IsJsonValueFormat_WhenSweptWithBraceCombinations_AcceptsOnlyPlainSubstitutions()
    {
        string[] fragments = ["", "x", "{", "}", "{0}", "{1}", "{{", "}}", "{0,9}", "{0:X}", "{a}", "0", ","];
        var accepted = 0;

        using (Assert.EnterMultipleScope())
        {
            foreach (var candidate in Combinations(fragments, 3))
            {
                if (!IsAccepted(candidate))
                    continue;

                accepted++;
                var parsed = CompositeFormat.Parse(candidate); // must not throw
                var formatted = string.Format(CultureInfo.InvariantCulture, parsed, _hash);
                var placeholders = candidate.Split("{0}").Length - 1;

                Assert.That(parsed.MinimumArgumentCount, Is.EqualTo(1), candidate);
                Assert.That(
                    formatted,
                    Has.Length.EqualTo(candidate.Length - (placeholders * 3) + (placeholders * _hash.Length)),
                    candidate);
            }
        }

        Assert.That(accepted, Is.GreaterThan(50), "the sweep must actually exercise the accepting branch");
    }

    private static IEnumerable<string> Combinations(string[] fragments, int length)
    {
        if (length is 0)
        {
            yield return string.Empty;
            yield break;
        }

        foreach (var head in fragments)
            foreach (var tail in Combinations(fragments, length - 1))
                yield return head + tail;
    }
}
