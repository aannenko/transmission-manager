using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TransmissionManager.TorrentSources.JsonPointer;
using TransmissionManager.TorrentSources.WebPage;

namespace TransmissionManager.TorrentSources.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentSourcesServiceCollectionExtensionsTests
{
    private const string _defaultMagnetRegexPattern = @"magnet:\?xt=urn:btih:[^""]+";

    [Test]
    public void AddTorrentSourcesServices_WhenOptionsAreValid_ValidatesOnStartWithoutThrowing()
    {
        using var provider = CreateProvider(Settings());

        Assert.That(() => provider.GetRequiredService<IStartupValidator>().Validate(), Throws.Nothing);
    }

    /// <remarks>
    /// A missing key binds to <see cref="TimeSpan.Zero"/>, which satisfies <c>[Required]</c> - only
    /// the range check rejects it, and only if validation actually runs at startup.
    /// </remarks>
    [TestCase(null)]
    [TestCase("00:00:00.5")]
    [TestCase("00:20:00")]
    public void AddTorrentSourcesServices_WhenResponseReadTimeoutIsUnusable_ValidationOnStartThrows(
        string? responseReadTimeout)
    {
        var settings = Settings();
        settings["TorrentSources:WebPage:ResponseReadTimeout"] = responseReadTimeout;

        using var provider = CreateProvider(settings);

        Assert.That(
            () => provider.GetRequiredService<IStartupValidator>().Validate(),
            Throws.TypeOf<OptionsValidationException>());
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
        var settings = Settings();
        settings["TorrentSources:JsonPointer:MaxJsonTokenBytes"] = maxJsonTokenBytes;

        using var provider = CreateProvider(settings);

        Assert.That(
            () => provider.GetRequiredService<IStartupValidator>().Validate(),
            Throws.TypeOf<OptionsValidationException>());
    }

    /// <remarks>
    /// The point of binding a section per source: one source's settings are its own. Both halves
    /// need asserting - that the bad value is reported, and that the other source still resolves -
    /// because the startup validator rethrows a lone failure as it is, so asserting only that it
    /// throws would pass just as well if both sources had broken.
    /// </remarks>
    [Test]
    public void AddTorrentSourcesServices_WhenOneSourceIsMisconfigured_LeavesTheOtherUsable()
    {
        var settings = Settings();
        settings["TorrentSources:WebPage:RegexMatchTimeout"] = "00:00:10";

        using var provider = CreateProvider(settings);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => provider.GetRequiredService<IOptionsMonitor<TorrentWebPageClientOptions>>().CurrentValue,
                Throws.TypeOf<OptionsValidationException>());

            Assert.That(
                () => provider.GetRequiredService<IOptionsMonitor<TorrentJsonPointerClientOptions>>().CurrentValue,
                Throws.Nothing);
        }
    }

    /// <remarks>
    /// Guards the two sections against being crossed or collapsed back into one, which the identical
    /// values the application ships with would hide.
    /// </remarks>
    [Test]
    public void AddTorrentSourcesServices_WhenSourcesAreTunedDifferently_BindsEachToItsOwnSection()
    {
        var settings = Settings();
        settings["TorrentSources:WebPage:ResponseReadTimeout"] = "00:00:45";
        settings["TorrentSources:JsonPointer:ResponseReadTimeout"] = "00:00:05";

        using var provider = CreateProvider(settings);

        var webPage = provider.GetRequiredService<IOptionsMonitor<TorrentWebPageClientOptions>>().CurrentValue;
        var jsonPointer = provider.GetRequiredService<IOptionsMonitor<TorrentJsonPointerClientOptions>>().CurrentValue;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(webPage.ResponseReadTimeout, Is.EqualTo(TimeSpan.FromSeconds(45)));
            Assert.That(jsonPointer.ResponseReadTimeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }
    }

    /// <remarks>
    /// A slice's whole section going missing is reported as such, rather than as the several
    /// property failures binding an empty section would produce. That is why the children are taken
    /// with <c>GetRequiredSection</c>: the message names what an operator has to add, and the
    /// realistic way to get here is a configuration file written against the older flat layout.
    /// </remarks>
    [TestCase("WebPage")]
    [TestCase("JsonPointer")]
    public void AddTorrentSourcesServices_WhenASourceSectionIsMissing_ThrowsNamingThatSection(string sectionName)
    {
        var settings = Settings();
        var prefix = $"TorrentSources:{sectionName}:";
        foreach (var key in settings.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            _ = settings.Remove(key);

        Assert.That(
            () => CreateProvider(settings),
            Throws.InvalidOperationException.With.Message.Contains(sectionName));
    }

    /// <remarks>
    /// Discovery by interface rather than a hardcoded list, so a client added later fails this until
    /// it is registered.
    /// </remarks>
    [Test]
    public void AddTorrentSourcesServices_WhenOptionsAreValid_RegistersEverySourceClient()
    {
        var clientTypes = typeof(ITorrentSourceClient).Assembly
            .GetTypes()
            .Where(static type =>
                type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(ITorrentSourceClient)))
            .ToArray();

        Assert.That(clientTypes, Is.Not.Empty, $"No {nameof(ITorrentSourceClient)} implementations were discovered.");

        using var provider = CreateProvider(Settings());

        using (Assert.EnterMultipleScope())
        {
            foreach (var clientType in clientTypes)
                Assert.That(provider.GetService(clientType), Is.Not.Null, $"{clientType.Name} is not registered.");
        }
    }

    private static Dictionary<string, string?> Settings() => new()
    {
        ["TorrentSources:WebPage:DefaultMagnetRegexPattern"] = _defaultMagnetRegexPattern,
        ["TorrentSources:WebPage:RegexMatchTimeout"] = "00:00:00.1",
        ["TorrentSources:WebPage:ResponseReadTimeout"] = "00:00:30",
        ["TorrentSources:JsonPointer:RegexMatchTimeout"] = "00:00:00.1",
        ["TorrentSources:JsonPointer:ResponseReadTimeout"] = "00:00:30",
        ["TorrentSources:JsonPointer:MaxJsonTokenBytes"] = "4096",
    };

    private static ServiceProvider CreateProvider(Dictionary<string, string?> settings) =>
        new ServiceCollection()
            .AddTorrentSourcesServices(new ConfigurationBuilder().AddInMemoryCollection(settings).Build())
            .BuildServiceProvider();
}
