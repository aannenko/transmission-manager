using TransmissionManager.TorrentWebPages.Extensions;

namespace TransmissionManager.TorrentWebPages.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class ReadOnlySpanExtensionsTests
{
    [Test]
    public void IndexOfStartOf_WhenGivenNormalSpanAndFullyExistingValue_ReturnsIndexOfValue() =>
        Assert.That("testing"u8.IndexOfStartOf("ing"u8), Is.EqualTo(4));

    [Test]
    public void IndexOfStartOf_WhenGivenNormalSpanAndValueEqualToSpan_ReturnsIndexOfValue() =>
        Assert.That("testing"u8.IndexOfStartOf("testing"u8), Is.EqualTo(0));

    [Test]
    public void IndexOfStartOf_WhenGivenNormalSpanAndPartiallyExistingValue_ReturnsIndexOfValue() =>
        Assert.That("testin"u8.IndexOfStartOf("ing"u8), Is.EqualTo(4));

    [Test]
    public void IndexOfStartOf_WhenGivenNormalSpanWithDuplicatesAndPartiallyExistingValue_ReturnsIndexOfValue() =>
        Assert.That("aaaab"u8.IndexOfStartOf("aabc"u8), Is.EqualTo(2));

    [Test]
    public void IndexOfStartOf_WhenGivenNormalSpanWithRepeatingPatternAndPartiallyExistingValue_ReturnsIndexOfValue() =>
        Assert.That("ababa"u8.IndexOfStartOf("abac"u8), Is.EqualTo(2));

    [Test]
    public void IndexOfStartOf_WhenGivenShorterSpanAndPartiallyExistingValue_ReturnsIndexOfValue() =>
        Assert.That("in"u8.IndexOfStartOf("ing"u8), Is.EqualTo(0));

    [Test]
    public void IndexOfStartOf_WhenGivenNormalSpanAndEmptyValue_ReturnsIndexOfValue() =>
        Assert.That("testing"u8.IndexOfStartOf(""u8), Is.EqualTo(0));

    [Test]
    public void IndexOfStartOf_WhenGivenEmptySpanAndEmptyValue_ReturnsIndexOfValue() =>
        Assert.That(""u8.IndexOfStartOf(""u8), Is.EqualTo(0));

    [Test]
    public void IndexOfStartOf_WhenGivenNormalSpanAndNonExistingValue_ReturnsMinusOne() =>
        Assert.That("testing"u8.IndexOfStartOf("asdfg"u8), Is.EqualTo(-1));

    [Test]
    public void IndexOfStartOf_WhenGivenEmptySpanAndNonEmptyValue_ReturnsMinusOne() =>
        Assert.That(""u8.IndexOfStartOf("asdf"u8), Is.EqualTo(-1));
}
