using TransmissionManager.TorrentSources.WebPage;

namespace TransmissionManager.TorrentSources.Tests;

/// <remarks>
/// One case per setting the validator is responsible for, because nothing else enumerates them: a
/// property added to the options class and forgotten here is a property nothing checks.
/// </remarks>
[Parallelizable(ParallelScope.All)]
internal sealed class ValidateTorrentWebPageClientOptionsTests
{
    private static readonly ValidateTorrentWebPageClientOptions _validator = new();

    [Test]
    public void Validate_WhenOptionsAreValid_Succeeds()
    {
        var result = _validator.Validate(null, CreateOptions());

        Assert.That(result.Succeeded, Is.True, result.FailureMessage);
    }

    [TestCase("00:00:00", TestName = "Validate_WhenResponseReadTimeoutIsOutOfRange_Fails(absent)")]
    [TestCase("00:00:00.999")]
    [TestCase("00:02:00.001")]
    public void Validate_WhenResponseReadTimeoutIsOutOfRange_Fails(string responseReadTimeout)
    {
        var options = CreateOptions();
        options.ResponseReadTimeout = TimeSpan.Parse(responseReadTimeout, null);

        AssertFailsNaming(options, nameof(options.ResponseReadTimeout));
    }

    [TestCase("00:00:00", TestName = "Validate_WhenRegexMatchTimeoutIsOutOfRange_Fails(absent)")]
    [TestCase("00:00:00.009")]
    [TestCase("00:00:00.501")]
    public void Validate_WhenRegexMatchTimeoutIsOutOfRange_Fails(string regexMatchTimeout)
    {
        var options = CreateOptions();
        options.RegexMatchTimeout = TimeSpan.Parse(regexMatchTimeout, null);

        AssertFailsNaming(options, nameof(options.RegexMatchTimeout));
    }

    [TestCase(null, TestName = "Validate_WhenDefaultMagnetRegexPatternIsUnusable_Fails(absent)")]
    [TestCase("", TestName = "Validate_WhenDefaultMagnetRegexPatternIsUnusable_Fails(empty)")]
    [TestCase("   ", TestName = "Validate_WhenDefaultMagnetRegexPatternIsUnusable_Fails(whitespace)")]
    [TestCase(@"xt=urn:btih:[a-fA-F0-9]{40}")] // does not look for a magnet link
    [TestCase(@"magnet:\?xt=urn:btih:[")] // looks for one, but does not compile
    public void Validate_WhenDefaultMagnetRegexPatternIsUnusable_Fails(string? defaultMagnetRegexPattern)
    {
        var options = CreateOptions();
        options.DefaultMagnetRegexPattern = defaultMagnetRegexPattern!;

        AssertFailsNaming(options, nameof(options.DefaultMagnetRegexPattern));
    }

    /// <remarks>
    /// Both ends of the limit, because only the pair of them catches an off-by-one. The pattern is
    /// padded rather than replaced so that length is the one thing left that can fail.
    /// </remarks>
    [TestCase(0, true, TestName = "Validate_WhenDefaultMagnetRegexPatternIsAtTheLengthLimit_Succeeds")]
    [TestCase(1, false, TestName = "Validate_WhenDefaultMagnetRegexPatternIsOverTheLengthLimit_Fails")]
    public void Validate_WhenDefaultMagnetRegexPatternIsAroundTheLengthLimit_SucceedsOnlyWithinIt(
        int overshoot,
        bool expectedToSucceed)
    {
        const string prefix = @"magnet:\?";

        var options = CreateOptions();
        options.DefaultMagnetRegexPattern =
            prefix + new string('a', RegexUtils.MaxPatternLength - prefix.Length + overshoot);

        Assert.That(options.DefaultMagnetRegexPattern, Has.Length.EqualTo(RegexUtils.MaxPatternLength + overshoot));

        if (expectedToSucceed)
        {
            var result = _validator.Validate(null, options);
            Assert.That(result.Succeeded, Is.True, result.FailureMessage);
        }
        else
        {
            AssertFailsNaming(options, nameof(options.DefaultMagnetRegexPattern));
        }
    }

    /// <remarks>
    /// The reason the checks are ordered. A missing setting binds to zero, which the regular
    /// expression constructor rejects outright, replacing every reported failure with one that names
    /// no setting at all.
    /// </remarks>
    [Test]
    public void Validate_WhenRegexMatchTimeoutIsMissing_DoesNotCompileThePattern()
    {
        var options = CreateOptions();
        options.RegexMatchTimeout = TimeSpan.Zero;

        Assert.That(() => _validator.Validate(null, options), Throws.Nothing);
    }

    private static void AssertFailsNaming(TorrentWebPageClientOptions options, string settingName)
    {
        var result = _validator.Validate(null, options);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.FailureMessage, Does.Contain(settingName));
        }
    }

    private static TorrentWebPageClientOptions CreateOptions() => new()
    {
        ResponseReadTimeout = TimeSpan.FromSeconds(30),
        RegexMatchTimeout = TimeSpan.FromMilliseconds(100),
        DefaultMagnetRegexPattern = @"magnet:\?xt=urn:btih:[^""]+",
    };
}
