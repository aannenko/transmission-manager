using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Actions.TransmissionInfo.Get;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Transmission.Options;

namespace TransmissionManager.Api.IntegrationTests.TransmissionInfo;

[Parallelizable(ParallelScope.All)]
internal sealed class GetTransmissionInfoTests
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
    public async Task GetTransmissionInfo_WhenCalled_ReturnsFullTransmissionApiAddress()
    {
        var options = _factory.Services.GetRequiredService<IOptions<TransmissionClientOptions>>().Value;
        var expectedAddress = new Uri(new(options.BaseAddress), options.RpcEndpointAddressSuffix);

        var response = await _client.GetAsync(TestData.Endpoints.TransmissionInfo).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadFromJsonAsync<GetTransmissionInfoResponse>().ConfigureAwait(false);

        Assert.That(content.EndpointAddress, Is.EqualTo(expectedAddress));
    }
}
