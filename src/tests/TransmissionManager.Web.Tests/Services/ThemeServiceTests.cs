using TransmissionManager.Web.Constants;
using TransmissionManager.Web.Services;
using TransmissionManager.Web.Tests.Helpers;

namespace TransmissionManager.Web.Tests.Services;

[Parallelizable(ParallelScope.Self)]
internal sealed class ThemeServiceTests
{
    private const string _storageKey = "theme";

    [Test]
    public void Theme_BeforeAnythingIsLoaded_IsLight()
    {
        Assert.That(CreateService(new()).Theme, Is.EqualTo(Theme.Light));
    }

    [TestCase("dark", Theme.Dark)]
    [TestCase("DARK", Theme.Dark)]
    [TestCase("light", Theme.Light)]
    public async Task LoadAsync_WhenStorageHoldsAThemeName_AdoptsIt(string stored, Theme expected)
    {
        var runtime = new FakeJSRuntime();
        runtime.Storage[_storageKey] = stored;
        var service = CreateService(runtime);

        await service.LoadAsync().ConfigureAwait(false);

        Assert.That(service.Theme, Is.EqualTo(expected));
    }

    /// <remarks>
    /// The numbers are the ones a name check alone lets through: every one of them parses.
    /// </remarks>
    [TestCase(null, TestName = "LoadAsync_WhenStorageHoldsNoThemeName_FallsBackToLight(nothing stored)")]
    [TestCase("", TestName = "LoadAsync_WhenStorageHoldsNoThemeName_FallsBackToLight(empty string)")]
    [TestCase("sepia")]
    [TestCase("2")]
    [TestCase("99")]
    [TestCase("-1")]
    [TestCase("-2")]
    public async Task LoadAsync_WhenStorageHoldsNoThemeName_FallsBackToLight(string? stored)
    {
        var runtime = new FakeJSRuntime();
        if (stored is not null)
            runtime.Storage[_storageKey] = stored;

        var service = CreateService(runtime);

        await service.LoadAsync().ConfigureAwait(false);

        Assert.That(service.Theme, Is.EqualTo(Theme.Light));
    }

    /// <remarks>
    /// Asserts the round trip rather than the stored text: nothing outside this service reads the
    /// value, so only surviving a reload is a promise to anyone.
    /// </remarks>
    [Test]
    public async Task SetThemeAsync_WhenGivenATheme_AdoptsItAndSurvivesAReload()
    {
        var runtime = new FakeJSRuntime();
        var service = CreateService(runtime);

        await service.SetThemeAsync(Theme.Dark).ConfigureAwait(false);

        Assert.That(service.Theme, Is.EqualTo(Theme.Dark));

        var reloaded = CreateService(runtime);
        await reloaded.LoadAsync().ConfigureAwait(false);

        Assert.That(reloaded.Theme, Is.EqualTo(Theme.Dark));
    }

    private static ThemeService CreateService(FakeJSRuntime runtime)
    {
        return new(new LocalStorageService(runtime));
    }
}
