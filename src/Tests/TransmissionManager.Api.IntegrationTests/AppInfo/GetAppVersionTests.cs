using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.IntegrationTests.Helpers;

namespace TransmissionManager.Api.IntegrationTests.AppVersion;

[Parallelizable(ParallelScope.All)]
internal sealed class GetAppVersionTests
{
    private TestWebApplicationFactory<Program> _factory = default!;
    private HttpClient _client = default!;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory<Program>([], null, null);
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async ValueTask TearDown()
    {
        _client?.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task GetAppVersion_WhenCalled_ReturnsExpectedAppVersion()
    {
        var expectedVersion = typeof(Program).Assembly.GetName().Version;
        var before = DateTimeOffset.Now;

        var response = await _client.GetAsync(EndpointAddresses.AppVersion).ConfigureAwait(false);

        var after = DateTimeOffset.Now;

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var version = await response.Content.ReadFromJsonAsync<Version>().ConfigureAwait(false);

        Assert.That(version, Is.Not.Default);
        Assert.That(version, Is.EqualTo(expectedVersion));
    }
}
