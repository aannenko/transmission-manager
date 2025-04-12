using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class FindTorrentByIdTests
{
    private static readonly Torrent[] _torrents = [TestData.Database.CreateInitialTorrents()[0]];

    private TestWebAppliationFactory<Program> _factory = default!;
    private HttpClient _client = default!;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestWebAppliationFactory<Program>(_torrents, null, null);
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async ValueTask TearDown()
    {
        _client?.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task FindTorrentByIdAsync_WhenIdExists_ReturnsTorrent()
    {
        var response = await _client.GetAsync($"{EndpointAddresses.Torrents}/1").ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var torrent = await response.Content.ReadFromJsonAsync<TorrentDto>().ConfigureAwait(false);
        var expected = _torrents[0];

        Assert.That(torrent, Is.Not.Null);
        TorrentAssertions.AssertEqual(torrent, 1, expected);
    }

    [Test]
    public async Task FindTorrentByIdAsync_WhenIdDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"{EndpointAddresses.Torrents}/999").ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem!.Detail, Is.EqualTo("Torrent with id 999 was not found."));
    }
}
