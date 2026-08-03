using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        });

        Assert.That(() => provider.GetRequiredService<IStartupValidator>().Validate(), Throws.Nothing);
    }

    /// <remarks>
    /// A missing key binds to <see cref="TimeSpan.Zero"/>, which satisfies <c>[Required]</c> - only
    /// the range check rejects it, and only if validation actually runs at startup.
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
