using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Actions.AppVersion.Get;
using TransmissionManager.Api.IntegrationTests.Helpers;

namespace TransmissionManager.Api.IntegrationTests.AppVersion;

[Parallelizable(ParallelScope.All)]
internal sealed class GetAppVersionTests
{
    private TestWebAppliationFactory<Program> _factory = default!;
    private HttpClient _client = default!;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestWebAppliationFactory<Program>([], null, null);
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async ValueTask TearDown()
    {
        _client?.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task GetAppVersion_WhenCalled_ReturnsAppVersion()
    {
        var expectedVersion = typeof(Program).Assembly.GetName().Version;

        var response = await _client.GetAsync(TestData.Endpoints.AppVersion).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadFromJsonAsync<GetAppVersionResponse>().ConfigureAwait(false);

        Assert.That(content, Is.Not.Default);
        Assert.That(content.Version, Is.EqualTo(expectedVersion));
    }
}
