using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TransmissionManager.Transmission.Options;

namespace TransmissionManager.Transmission.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TransmissionServiceCollectionExtensionsTests
{
    [Test]
    public void AddTransmissionServices_WhenOptionsAreValid_ValidatesOnStartWithoutThrowing()
    {
        using var provider = CreateProvider(Settings());

        Assert.That(() => provider.GetRequiredService<IStartupValidator>().Validate(), Throws.Nothing);
    }

    /// <remarks>
    /// One case per setting: these are validated by annotation, so a property that loses its
    /// attribute is caught by nothing else. Registering a validator alone would defer all of them
    /// to the first resolution of the options - the first request reaching Transmission, not boot.
    /// </remarks>
    [TestCase("Transmission:BaseAddress", null)]
    [TestCase("Transmission:BaseAddress", "http://transmission")]
    [TestCase("Transmission:BaseAddress", "ftp://transmission:9091")]
    [TestCase("Transmission:RpcEndpointAddressSuffix", null)]
    [TestCase("Transmission:SessionHeaderName", null)]
    public void AddTransmissionServices_WhenASettingIsUnusable_ValidationOnStartThrowsNamingIt(
        string key,
        string? value)
    {
        var settings = Settings();
        settings[key] = value;

        using var provider = CreateProvider(settings);

        Assert.That(
            () => provider.GetRequiredService<IStartupValidator>().Validate(),
            Throws.TypeOf<OptionsValidationException>()
                .With.Message.Contains(key.Split(':')[^1]));
    }

    [Test]
    public void AddTransmissionServices_WhenSectionIsBound_FillsBothOptionsFromIt()
    {
        using var provider = CreateProvider(Settings());

        var client = provider.GetRequiredService<IOptionsMonitor<TransmissionClientOptions>>().CurrentValue;
        var header = provider.GetRequiredService<IOptionsMonitor<SessionHeaderProviderOptions>>().CurrentValue;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(client.BaseAddress, Is.EqualTo("http://transmission:9091"));
            Assert.That(client.RpcEndpointAddressSuffix, Is.EqualTo("/transmission/rpc"));
            Assert.That(header.SessionHeaderName, Is.EqualTo("X-Transmission-Session-Id"));
        }
    }

    [Test]
    public void AddTransmissionServices_WhenTheSectionIsMissing_ThrowsNamingIt() =>
        Assert.That(
            () => CreateProvider([]),
            Throws.InvalidOperationException.With.Message.Contains("Transmission"));

    private static Dictionary<string, string?> Settings() => new()
    {
        ["Transmission:BaseAddress"] = "http://transmission:9091",
        ["Transmission:RpcEndpointAddressSuffix"] = "/transmission/rpc",
        ["Transmission:SessionHeaderName"] = "X-Transmission-Session-Id",
    };

    private static ServiceProvider CreateProvider(Dictionary<string, string?> settings) =>
        new ServiceCollection()
            .AddTransmissionServices(new ConfigurationBuilder().AddInMemoryCollection(settings).Build())
            .BuildServiceProvider();
}
