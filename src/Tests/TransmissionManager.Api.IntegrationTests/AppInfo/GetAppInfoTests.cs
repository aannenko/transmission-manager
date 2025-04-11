using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Api.Common.Dto.AppInfo;

namespace TransmissionManager.Api.IntegrationTests.AppInfo;

[Parallelizable(ParallelScope.All)]
internal sealed class GetAppInfoTests
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
    public async Task GetAppInfo_WhenCalled_ReturnsExpectedAppInfo()
    {
        var expectedVersion = typeof(Program).Assembly.GetName().Version;
        var before = DateTimeOffset.Now;

        var response = await _client.GetAsync(TestData.Endpoints.AppInfo).ConfigureAwait(false);

        var after = DateTimeOffset.Now;

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadFromJsonAsync<GetAppInfoResponse>().ConfigureAwait(false);

        Assert.That(content, Is.Not.Default);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(content.Version, Is.EqualTo(expectedVersion));
            Assert.That(content.LocalTime, Is.GreaterThan(before));
            Assert.That(content.LocalTime, Is.LessThan(after));
            Assert.That(content.LocalTime.Offset, Is.EqualTo(before.Offset));
        }
    }
}
