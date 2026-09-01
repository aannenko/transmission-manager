using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class GetTorrentByIdTests
{
    private static readonly Torrent[] _torrents = [TestData.Database.CreateInitialTorrents()[0]];

    private TestWebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory<Program>(_torrents, null, null);
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async ValueTask TearDown()
    {
        _client?.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task GetTorrentByIdAsync_WhenIdExists_ReturnsTorrent()
    {
        var response = await _client.GetAsync($"{EndpointAddresses.Torrents}/1").ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var torrent = await response.Content.ReadFromJsonAsync<TorrentDto>().ConfigureAwait(false);
        var expected = _torrents[0];

        Assert.That(torrent, Is.Not.Null);
        TorrentAssertions.AssertEqual(torrent, expected);
    }

    [Test]
    public async Task GetTorrentByIdAsync_WhenIdDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"{EndpointAddresses.Torrents}/999").ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problem = await response.Content
            .ReadFromJsonAsync<HttpValidationProblemDetails>()
            .ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Errors["id"], Is.EqualTo(["No such torrent."]));

            // The point of keying the messages is that there is nothing else to read.
            Assert.That(problem.Detail, Is.Null);
        }
    }
}
