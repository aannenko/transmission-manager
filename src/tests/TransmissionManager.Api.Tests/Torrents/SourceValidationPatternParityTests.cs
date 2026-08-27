using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TransmissionManager.Api.Common.Attributes;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Validation;
using TransmissionManager.TorrentSources;
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

    /// <remarks>
    /// Refusing a pattern that cannot be built is only worth anything while both sides build them
    /// the same way. <c>(a)\1</c> is what makes that a real risk: valid under the default options,
    /// a parse error under <c>ExplicitCapture</c>.
    /// </remarks>
    [TestCase(@"(a)\1", TestName = "TorrentSourceRules_WhenComparedWithTheClientsRegexBuilder_AcceptsTheSamePatterns(unnamed backreference)")]
    [TestCase(@"(?<v>a)\k<v>", TestName = "TorrentSourceRules_WhenComparedWithTheClientsRegexBuilder_AcceptsTheSamePatterns(named backreference)")]
    [TestCase("[a-z", TestName = "TorrentSourceRules_WhenComparedWithTheClientsRegexBuilder_AcceptsTheSamePatterns(unterminated set)")]
    [TestCase("a{2,1}", TestName = "TorrentSourceRules_WhenComparedWithTheClientsRegexBuilder_AcceptsTheSamePatterns(reversed quantifier)")]
    [TestCase("[a-fA-F0-9]{40}", TestName = "TorrentSourceRules_WhenComparedWithTheClientsRegexBuilder_AcceptsTheSamePatterns(hash)")]
    public void TorrentSourceRules_WhenComparedWithTheClientsRegexBuilder_AcceptsTheSamePatterns(string pattern)
    {
        var acceptedByRules = TorrentSourceRules
            .Validate(TorrentSourceKind.JsonPointer, pattern, null)
            .Length is 0;

        bool acceptedByClients;
        try
        {
            _ = RegexUtils.CreateInterpretedRegex(pattern, TimeSpan.FromMilliseconds(100));
            acceptedByClients = true;
        }
        catch (RegexParseException)
        {
            acceptedByClients = false;
        }

        Assert.That(acceptedByRules, Is.EqualTo(acceptedByClients), pattern);
    }
}
