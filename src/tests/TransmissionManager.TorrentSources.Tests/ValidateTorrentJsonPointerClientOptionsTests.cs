using TransmissionManager.TorrentSources.JsonPointer;

namespace TransmissionManager.TorrentSources.Tests;

/// <remarks>
/// One case per setting the validator is responsible for, because nothing else enumerates them: a
/// property added to the options class and forgotten here is a property nothing checks.
/// </remarks>
[Parallelizable(ParallelScope.All)]
internal sealed class ValidateTorrentJsonPointerClientOptionsTests
{
    private static readonly ValidateTorrentJsonPointerClientOptions _validator = new();

    [Test]
    public void Validate_WhenOptionsAreValid_Succeeds()
    {
        var result = _validator.Validate(null, CreateOptions());

        Assert.That(result.Succeeded, Is.True, result.FailureMessage);
    }

    /// <remarks>
    /// Both extraction settings are optional: a source whose JSON already holds whole magnet links
    /// needs neither.
    /// </remarks>
    [TestCase(null, TestName = "Validate_WhenExtractionSettingsAreAbsent_Succeeds(null)")]
    [TestCase("", TestName = "Validate_WhenExtractionSettingsAreAbsent_Succeeds(empty)")]
    public void Validate_WhenExtractionSettingsAreAbsent_Succeeds(string? absent)
    {
        var options = CreateOptions();
        options.DefaultJsonValueRegexPattern = absent;
        options.DefaultJsonValueFormat = absent;

        var result = _validator.Validate(null, options);

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

    [TestCase(0, TestName = "Validate_WhenMaxJsonTokenBytesIsOutOfRange_Fails(absent)")]
    [TestCase(1023)]
    [TestCase(65537)]
    public void Validate_WhenMaxJsonTokenBytesIsOutOfRange_Fails(int maxJsonTokenBytes)
    {
        var options = CreateOptions();
        options.MaxJsonTokenBytes = maxJsonTokenBytes;

        AssertFailsNaming(options, nameof(options.MaxJsonTokenBytes));
    }

    [TestCase(@"(?<value>[a-f]{40})[")] // does not compile
    public void Validate_WhenDefaultJsonValueRegexPatternIsUnusable_Fails(string defaultJsonValueRegexPattern)
    {
        var options = CreateOptions();
        options.DefaultJsonValueRegexPattern = defaultJsonValueRegexPattern;

        AssertFailsNaming(options, nameof(options.DefaultJsonValueRegexPattern));
    }

    [TestCase("magnet:?xt=urn:btih:{1}")] // asks for an argument the substitution never supplies
    [TestCase("magnet:?xt=urn:btih:{0,1000000}")] // an alignment, honoured by allocating a megabyte
    [TestCase("magnet:?xt=urn:btih:")] // no placeholder, so every torrent would get the same link
    public void Validate_WhenDefaultJsonValueFormatIsUnusable_Fails(string defaultJsonValueFormat)
    {
        var options = CreateOptions();
        options.DefaultJsonValueFormat = defaultJsonValueFormat;

        AssertFailsNaming(options, nameof(options.DefaultJsonValueFormat));
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

    private static void AssertFailsNaming(TorrentJsonPointerClientOptions options, string settingName)
    {
        var result = _validator.Validate(null, options);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.FailureMessage, Does.Contain(settingName));
        }
    }

    private static TorrentJsonPointerClientOptions CreateOptions() => new()
    {
        ResponseReadTimeout = TimeSpan.FromSeconds(30),
        RegexMatchTimeout = TimeSpan.FromMilliseconds(100),
        MaxJsonTokenBytes = 4096,
        DefaultJsonValueRegexPattern = "[a-fA-F0-9]{40}",
        DefaultJsonValueFormat = "magnet:?xt=urn:btih:{0}",
    };
}
