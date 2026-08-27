using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Validation;

namespace TransmissionManager.Api.Common.Tests;

[Parallelizable(ParallelScope.All)]
internal sealed class TorrentSourceRulesTests
{
    private const string _magnetPattern = @"magnet:\?xt=urn:btih:[^""]+";
    private const string _valuePattern = "[a-fA-F0-9]{40}";
    private const string _magnetFormat = "magnet:?xt=urn:btih:{0}";
    private const string _notAMagnetSearch = "Invalid regex for magnet link search.";
    private const string _formatUnused = "A magnet format is only used by a JsonPointer source.";

    [TestCase(null, null)]
    [TestCase("", "")]
    [TestCase(_magnetPattern, null)]
    [TestCase(_magnetPattern, "")]
    public void Validate_WhenWebPageSettingsAreAcceptable_YieldsNothing(string? pattern, string? format)
    {
        var failures = TorrentSourceRules.Validate(TorrentSourceKind.WebPage, pattern, format);

        Assert.That(failures, Is.Empty);
    }

    [Test]
    public void Validate_WhenWebPagePatternDoesNotLookForAMagnet_NamesThePattern()
    {
        var failures = TorrentSourceRules.Validate(TorrentSourceKind.WebPage, _valuePattern, null);

        Assert.That(failures, Has.Length.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(failures[0].Key, Is.EqualTo(nameof(AddTorrentRequest.MagnetRegexPattern)));
            Assert.That(failures[0].Value, Is.EqualTo([_notAMagnetSearch]));
        }
    }

    /// <remarks>
    /// A web page source never reads a format, and the kind cannot change afterwards, so one stored
    /// here could never come into effect.
    /// </remarks>
    [Test]
    public void Validate_WhenWebPageCarriesAMagnetFormat_NamesTheFormat()
    {
        var failures = TorrentSourceRules.Validate(TorrentSourceKind.WebPage, null, _magnetFormat);

        Assert.That(failures, Has.Length.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(failures[0].Key, Is.EqualTo(nameof(AddTorrentRequest.JsonValueFormat)));
            Assert.That(failures[0].Value, Is.EqualTo([_formatUnused]));
        }
    }

    [Test]
    public void Validate_WhenWebPageBreaksBothRules_NamesBothSettings()
    {
        var failures = TorrentSourceRules.Validate(TorrentSourceKind.WebPage, _valuePattern, _magnetFormat);

        Assert.That(failures.Select(static failure => failure.Key), Is.EqualTo(
            [nameof(AddTorrentRequest.MagnetRegexPattern), nameof(AddTorrentRequest.JsonValueFormat)]));
    }

    /// <remarks>
    /// This pattern would be refused on a web page torrent, which is the point: a JSON pattern
    /// matches a value inside a string and has no reason to mention a magnet link.
    /// </remarks>
    [TestCase(_valuePattern, _magnetFormat)]
    [TestCase(null, null)]
    [TestCase("", "")]
    public void Validate_WhenSourceIsJsonPointer_YieldsNothing(string? pattern, string? format)
    {
        var failures = TorrentSourceRules.Validate(TorrentSourceKind.JsonPointer, pattern, format);

        Assert.That(failures, Is.Empty);
    }

    /// <remarks>
    /// An undefined kind is a bad kind, not bad settings, and naming the settings too would send the
    /// caller after fields that are not the problem. The pattern still has to parse.
    /// </remarks>
    [Test]
    public void Validate_WhenSourceKindIsNotADefinedMember_JudgesOnlyThePattern()
    {
        var accepted = TorrentSourceRules.Validate((TorrentSourceKind)999, _valuePattern, _magnetFormat);
        var refused = TorrentSourceRules.Validate((TorrentSourceKind)999, "[a-z", _magnetFormat);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(accepted, Is.Empty);
            Assert.That(refused, Has.Length.EqualTo(1));
            Assert.That(refused[0].Key, Is.EqualTo(nameof(AddTorrentRequest.MagnetRegexPattern)));
        }
    }

    /// <remarks>
    /// Closes the update path: nothing else builds a stored pattern before the refresh that needs
    /// it, so without this a torrent accepts a pattern that can only fail later.
    /// </remarks>
    [TestCase("[a-z", TestName = "Validate_WhenPatternCannotParse_NamesThePattern(unterminated set)")]
    [TestCase(@"magnet:\?xt=(", TestName = "Validate_WhenPatternCannotParse_NamesThePattern(unbalanced parenthesis)")]
    [TestCase("a{2,1}", TestName = "Validate_WhenPatternCannotParse_NamesThePattern(reversed quantifier)")]
    public void Validate_WhenPatternCannotParse_NamesThePattern(string pattern)
    {
        foreach (var sourceKind in new[] { TorrentSourceKind.WebPage, TorrentSourceKind.JsonPointer })
        {
            var failures = TorrentSourceRules.Validate(sourceKind, pattern, null);

            Assert.That(failures, Has.Length.EqualTo(1), sourceKind.ToString());
            Assert.That(
                failures[0].Key,
                Is.EqualTo(nameof(AddTorrentRequest.MagnetRegexPattern)),
                sourceKind.ToString());
        }
    }

    /// <remarks>
    /// Both faults are real; only the parse error is reported.
    /// </remarks>
    [Test]
    public void Validate_WhenPatternNeitherParsesNorFindsAMagnet_ReportsOnlyTheParseError()
    {
        var failures = TorrentSourceRules.Validate(TorrentSourceKind.WebPage, "[a-z", null);

        Assert.That(failures, Has.Length.EqualTo(1));
        Assert.That(failures[0].Value, Does.Not.Contain(_notAMagnetSearch));
    }

    /// <remarks>
    /// What makes the options load-bearing: under <c>ExplicitCapture</c> an unnamed group captures
    /// nothing, so a backreference to one does not parse, while a named one still does. Default
    /// options would accept the first and leave it to fail at the next refresh.
    /// </remarks>
    [TestCase(@"(a)\1", false, TestName = "Validate_WhenPatternDependsOnCaptureOptions_FollowsTheClients(unnamed backreference)")]
    [TestCase(@"(?<v>a)\k<v>", true, TestName = "Validate_WhenPatternDependsOnCaptureOptions_FollowsTheClients(named backreference)")]
    public void Validate_WhenPatternDependsOnCaptureOptions_FollowsTheClients(string pattern, bool expectedValid)
    {
        var failures = TorrentSourceRules.Validate(TorrentSourceKind.JsonPointer, pattern, null);

        Assert.That(failures, expectedValid ? Is.Empty : Is.Not.Empty);
    }

    /// <remarks>
    /// The adapter the request DTOs and the details page validate through: it has to carry every
    /// message across, against the member it was reported for.
    /// </remarks>
    [Test]
    public void ToValidationResults_WhenBothRulesAreBroken_CarriesEveryMessageAndMember()
    {
        var results = TorrentSourceRules
            .GetValidationResults(TorrentSourceKind.WebPage, _valuePattern, _magnetFormat)
            .ToArray();

        Assert.That(results, Has.Length.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(results[0].ErrorMessage, Is.EqualTo(_notAMagnetSearch));
            Assert.That(results[0].MemberNames, Is.EqualTo([nameof(AddTorrentRequest.MagnetRegexPattern)]));
            Assert.That(results[1].ErrorMessage, Is.EqualTo(_formatUnused));
            Assert.That(results[1].MemberNames, Is.EqualTo([nameof(AddTorrentRequest.JsonValueFormat)]));
        }
    }

    [Test]
    public void ToValidationResults_WhenEveryRuleHolds_YieldsNothing()
    {
        var results = TorrentSourceRules.GetValidationResults(TorrentSourceKind.WebPage, _magnetPattern, null);

        Assert.That(results, Is.Empty);
    }
}
