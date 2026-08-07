using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TransmissionManager.TorrentSources.Options;

namespace TransmissionManager.TorrentSources.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentSourcesServiceCollectionExtensionsTests
{
    private const string _defaultMagnetRegexPattern = @"magnet:\?xt=urn:btih:[^""]+";

    [Test]
    public void AddTorrentSourcesServices_WhenOptionsAreValid_ValidatesOnStartWithoutThrowing()
    {
        using var provider = CreateProvider(new()
        {
            ["TorrentSources:DefaultMagnetRegexPattern"] = _defaultMagnetRegexPattern,
            ["TorrentSources:RegexMatchTimeout"] = "00:00:00.1",
            ["TorrentSources:MagnetSearchTimeout"] = "00:00:30",
            ["TorrentSources:MaxJsonTokenBytes"] = "4096",
        });

        Assert.That(() => provider.GetRequiredService<IStartupValidator>().Validate(), Throws.Nothing);
    }

    /// <remarks>
    /// A missing key binds to <see cref="TimeSpan.Zero"/>, which satisfies <c>[Required]</c> - only
    /// the range check rejects it, and only if validation actually runs at startup.
    /// <para>
    /// Asserted on the message rather than the exception type: every source's options inherit this
    /// setting, so each one reports it and the host aggregates the failures. How many sources exist
    /// is not what this test is about.
    /// </para>
    /// </remarks>
    [TestCase(null)]
    [TestCase("00:00:00.5")]
    [TestCase("00:20:00")]
    public void AddTorrentSourcesServices_WhenMagnetSearchTimeoutIsUnusable_ValidationOnStartThrows(
        string? magnetSearchTimeout)
    {
        using var provider = CreateProvider(new()
        {
            ["TorrentSources:DefaultMagnetRegexPattern"] = _defaultMagnetRegexPattern,
            ["TorrentSources:RegexMatchTimeout"] = "00:00:00.1",
            ["TorrentSources:MagnetSearchTimeout"] = magnetSearchTimeout,
            ["TorrentSources:MaxJsonTokenBytes"] = "4096",
        });

        Assert.That(
            () => provider.GetRequiredService<IStartupValidator>().Validate(),
            Throws.Exception.With.Message.Contains(nameof(TorrentSourcesOptions.MagnetSearchTimeout)));
    }

    /// <remarks>
    /// A missing key binds to zero, which the range check rejects, so the limit cannot be left
    /// unset and silently default to something that would not hold an info hash.
    /// </remarks>
    [TestCase(null, TestName =
        "AddTorrentSourcesServices_WhenMaxJsonTokenBytesIsUnusable_ValidationOnStartThrows(absent)")]
    [TestCase("1023", TestName =
        "AddTorrentSourcesServices_WhenMaxJsonTokenBytesIsUnusable_ValidationOnStartThrows(below the minimum)")]
    [TestCase("65537", TestName =
        "AddTorrentSourcesServices_WhenMaxJsonTokenBytesIsUnusable_ValidationOnStartThrows(above the maximum)")]
    public void AddTorrentSourcesServices_WhenMaxJsonTokenBytesIsUnusable_ValidationOnStartThrows(
        string? maxJsonTokenBytes)
    {
        using var provider = CreateProvider(new()
        {
            ["TorrentSources:DefaultMagnetRegexPattern"] = _defaultMagnetRegexPattern,
            ["TorrentSources:RegexMatchTimeout"] = "00:00:00.1",
            ["TorrentSources:MagnetSearchTimeout"] = "00:00:30",
            ["TorrentSources:MaxJsonTokenBytes"] = maxJsonTokenBytes,
        });

        Assert.That(
            () => provider.GetRequiredService<IStartupValidator>().Validate(),
            Throws.TypeOf<OptionsValidationException>());
    }

    private static ServiceProvider CreateProvider(Dictionary<string, string?> settings) =>
        new ServiceCollection()
            .AddTorrentSourcesServices(new ConfigurationBuilder().AddInMemoryCollection(settings).Build())
            .BuildServiceProvider();
}
