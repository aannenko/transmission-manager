using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.IntegrationTests.Helpers;

namespace TransmissionManager.Api.IntegrationTests.LocalTime;

[Parallelizable(ParallelScope.All)]
internal sealed class GetLocalTimeTests
{
    private readonly record struct GetLocalTimeStringResponse(string LocalTime);

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
    public async Task GetLocalTime_WhenCalled_ReturnsDateTimeOffsetInIso86012019Format()
    {
        var before = DateTimeOffset.Now;

        var response = await _client.GetAsync(TestData.Endpoints.LocalTime).ConfigureAwait(false);

        var after = DateTimeOffset.Now;

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadFromJsonAsync<GetLocalTimeStringResponse>().ConfigureAwait(false);

        Assert.That(content, Is.Not.Default);

        var isParsed = DateTimeOffset.TryParseExact(content.LocalTime, "O", null, DateTimeStyles.None, out var time);

        Assert.That(isParsed, Is.True);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(time.Offset, Is.EqualTo(before.Offset));
            Assert.That(time, Is.GreaterThan(before));
            Assert.That(time, Is.LessThan(after));
        }
    }
}
