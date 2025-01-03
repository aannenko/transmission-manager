using TransmissionManager.TorrentWebPages.Extensions;

namespace TransmissionManager.TorrentWebPages.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class ReadOnlySpanExtensionsTests
{
    [Test]
    public void IndexOfStartOf_GivenNormalSpanAndFullyExistingValue_ReturnsIndexOfValue() =>
        Assert.That("testing"u8.IndexOfStartOf("ing"u8), Is.EqualTo(4));

    [Test]
    public void IndexOfStartOf_GivenNormalSpanAndValueEqualToSpan_ReturnsIndexOfValue() =>
        Assert.That("testing"u8.IndexOfStartOf("testing"u8), Is.EqualTo(0));

    [Test]
    public void IndexOfStartOf_GivenNormalSpanAndPartiallyExistingValue_ReturnsIndexOfValue() =>
        Assert.That("testin"u8.IndexOfStartOf("ing"u8), Is.EqualTo(4));

    [Test]
    public void IndexOfStartOf_GivenNormalSpanWithDuplicatesAndPartiallyExistingValue_ReturnsIndexOfValue() =>
        Assert.That("aaaab"u8.IndexOfStartOf("aabc"u8), Is.EqualTo(2));

    [Test]
    public void IndexOfStartOf_GivenNormalSpanWithRepeatingPatternAndPartiallyExistingValue_ReturnsIndexOfValue() =>
        Assert.That("ababa"u8.IndexOfStartOf("abac"u8), Is.EqualTo(2));

    [Test]
    public void IndexOfStartOf_GivenShorterSpanAndPartiallyExistingValue_ReturnsIndexOfValue() =>
        Assert.That("in"u8.IndexOfStartOf("ing"u8), Is.EqualTo(0));

    [Test]
    public void IndexOfStartOf_GivenNormalSpanAndEmptyValue_ReturnsIndexOfValue() =>
        Assert.That("testing"u8.IndexOfStartOf(""u8), Is.EqualTo(0));

    [Test]
    public void IndexOfStartOf_GivenEmptySpanAndEmptyValue_ReturnsIndexOfValue() =>
        Assert.That(""u8.IndexOfStartOf(""u8), Is.EqualTo(0));

    [Test]
    public void IndexOfStartOf_GivenNormalSpanAndNonExistingValue_ReturnsMinusOne() =>
        Assert.That("testing"u8.IndexOfStartOf("asdfg"u8), Is.EqualTo(-1));

    [Test]
    public void IndexOfStartOf_GivenEmptySpanAndNonEmptyValue_ReturnsMinusOne() =>
        Assert.That(""u8.IndexOfStartOf("asdf"u8), Is.EqualTo(-1));
}
