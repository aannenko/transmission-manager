using SourcesRemoteTextUtils = TransmissionManager.TorrentSources.RemoteTextUtils;
using TransmissionRemoteTextUtils = TransmissionManager.Transmission.RemoteTextUtils;

namespace TransmissionManager.Api.Tests;

/// <remarks>
/// The torrent sources and the Transmission client each quote text a server sent, and neither library
/// can reference the other, so each holds its own copy of the helper. Nothing but this test stops a fix
/// to one copy from leaving the other behind, and the copy left behind would go on letting through
/// what the fixed one now stops.
/// </remarks>
[Parallelizable(ParallelScope.All)]
internal sealed class RemoteTextUtilsParityTests
{
    private static readonly string _mixedText = string.Concat(Enumerable.Repeat("x\u001b\U0001F600", 30));

    [Test]
    public void DefaultSummaryLength_WhenComparedWithItsSourcesCopy_IsTheSame() =>
        Assert.That(
            TransmissionRemoteTextUtils.DefaultSummaryLength,
            Is.EqualTo(SourcesRemoteTextUtils.DefaultSummaryLength));

    [Test]
    public void Summarize_WhenGivenEachUtf16CodeUnit_MatchesItsSourcesCopy()
    {
        var codeUnits = Enumerable.Range(char.MinValue, char.MaxValue + 1)
            .Select(static code => ((char)code).ToString())
            .ToArray();

        Assert.That(
            codeUnits.Select(static unit => TransmissionRemoteTextUtils.Summarize(unit)),
            Is.EqualTo(codeUnits.Select(static unit => SourcesRemoteTextUtils.Summarize(unit))));
    }

    [Test]
    public void Summarize_WhenGivenEachLengthAtTheDefaultBudget_MatchesItsSourcesCopy()
    {
        var lengths = Enumerable.Range(0, _mixedText.Length + 1).ToArray();

        Assert.That(
            lengths.Select(static length =>
                $"{length}: {TransmissionRemoteTextUtils.Summarize(_mixedText.AsSpan(0, length))}"),
            Is.EqualTo(lengths.Select(static length =>
                $"{length}: {SourcesRemoteTextUtils.Summarize(_mixedText.AsSpan(0, length))}")));
    }

    [Test]
    public void Summarize_WhenGivenEachLengthAtEachBudget_MatchesItsSourcesCopy()
    {
        var cases = (
            from length in Enumerable.Range(0, _mixedText.Length + 1)
            from maxLength in Enumerable.Range(1, 100)
            select (length, maxLength))
            .ToArray();

        Assert.That(
            cases.Select(static c =>
                $"{c}: {TransmissionRemoteTextUtils.Summarize(_mixedText.AsSpan(0, c.length), c.maxLength)}"),
            Is.EqualTo(cases.Select(static c =>
                $"{c}: {SourcesRemoteTextUtils.Summarize(_mixedText.AsSpan(0, c.length), c.maxLength)}")));
    }

    [TestCase(int.MinValue)]
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(101)]
    [TestCase(int.MaxValue)]
    public void Summarize_WhenBudgetIsOutOfRange_ThrowsLikeItsSourcesCopy(int maxLength)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => TransmissionRemoteTextUtils.Summarize("served text", maxLength),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => SourcesRemoteTextUtils.Summarize("served text", maxLength),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
