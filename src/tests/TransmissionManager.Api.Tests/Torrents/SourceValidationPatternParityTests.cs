using System.ComponentModel.DataAnnotations;
using TransmissionManager.Api.Common.Attributes;
using TransmissionManager.TorrentSources.JsonPointer;
using TransmissionManager.TorrentSources.WebPage;

namespace TransmissionManager.Api.Tests.Torrents;

/// <remarks>
/// The request DTOs live in a project that cannot reference the torrent sources, so each validating
/// pattern exists twice. Nothing but this test stops a change to one from leaving the other behind,
/// and a drift would let the API accept a value the source client then rejects at fetch time - or,
/// worse, refuse one that would have worked.
/// </remarks>
[Parallelizable(ParallelScope.All)]
internal sealed class SourceValidationPatternParityTests
{
    private static IEnumerable<TestCaseData<ValidationAttribute, string>> PatternTestCases()
    {
        yield return new(new MagnetRegexAttribute(), TorrentRegex.IsFindMagnet)
        { TestName = "ValidationAttribute_WhenComparedWithItsSourcesCopy_UsesTheSamePattern(MagnetRegex)" };

        yield return new(new JsonValueFormatAttribute(), JsonValueRegex.IsJsonValueFormat)
        { TestName = "ValidationAttribute_WhenComparedWithItsSourcesCopy_UsesTheSamePattern(JsonValueFormat)" };
    }

    [TestCaseSource(nameof(PatternTestCases))]
    public void ValidationAttribute_WhenComparedWithItsSourcesCopy_UsesTheSamePattern(
        ValidationAttribute attribute,
        string sourcesPattern)
    {
        Assert.That(((RegularExpressionAttribute)attribute).Pattern, Is.EqualTo(sourcesPattern));
    }

    // Both copies must also agree on what they accept, not merely on their text: the API's is
    // reached first, and anything it lets through the client is then free to refuse.
    [TestCase("magnet:?xt=urn:btih:{0}", true)]
    [TestCase("{0}", true)]
    [TestCase("magnet:?xt=urn:btih:", false)]
    public void JsonValueFormatAttribute_WhenComparedWithItsSourcesCopy_AcceptsTheSameValues(
        string format,
        bool expected)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(new JsonValueFormatAttribute().IsValid(format), Is.EqualTo(expected));
            Assert.That(JsonValueRegex.IsJsonValueFormatRegex().IsMatch(format), Is.EqualTo(expected));
        }
    }

    // The formats that made the pattern necessary; what each of them would do if it ever got past
    // the pattern is pinned by JsonValueFormatSafetyTests.
    [Test]
    public void JsonValueFormatAttribute_WhenFormatAsksForMoreThanTheValue_IsRejectedByBothCopies()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var format in new[] { "magnet:?xt=urn:btih:{0}&y={bad}", "magnet:?xt=urn:btih:{0}&x={1}", "{0,1000000}" })
            {
                Assert.That(new JsonValueFormatAttribute().IsValid(format), Is.False, format);
                Assert.That(JsonValueRegex.IsJsonValueFormatRegex().IsMatch(format), Is.False, format);
            }
        }
    }
}
