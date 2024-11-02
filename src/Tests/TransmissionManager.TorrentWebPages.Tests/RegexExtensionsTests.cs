using System.Text.RegularExpressions;
using TransmissionManager.TorrentWebPages.Extensions;

namespace TransmissionManager.TorrentWebPages.Tests;

public sealed partial class RegexExtensionsTests
{
    [Test]
    public void TryGetFirstMatch_WhenGivenMatchingSpan_ReturnsTrueAndNonEmptyMatchRange()
    {
        var result = CdeRegex().TryGetFirstMatch("abcdefg", out var range);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(range, Is.EqualTo(new Range(2, 5)));
        });
    }

    [Test]
    public void TryGetFirstMatch_WhenGivenNonMatchingSpan_ReturnsFalseAndEmptyMatchRange()
    {
        var result = CdfRegex().TryGetFirstMatch("abcdefg", out var range);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(range, Is.EqualTo(Range.EndAt(0)));
        });
    }

    [GeneratedRegex("cde")]
    private static partial Regex CdeRegex();

    [GeneratedRegex("cdf")]
    private static partial Regex CdfRegex();
}
