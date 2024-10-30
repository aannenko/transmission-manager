using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests;

[Parallelizable(ParallelScope.Self)]
public sealed class FindTorrentByIdTests
{
    private static readonly Torrent[] _torrents = [TestData.Database.CreateInitialTorrents()[0]];

    private TestWebAppliationFactory<Program> _factory;
    private HttpClient _client;

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
    public async Task FindTorrentByIdAsync_WhenGivenExistingTorrentId_ReturnsTorrent()
    {
        var response = await _client.GetAsync($"{TestData.Endpoints.Torrents}/1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var torrent = await response.Content.ReadFromJsonAsync<Torrent>();
        var expected = _torrents[0];

        Assert.That(torrent, Is.Not.Null);
        TorrentAssertions.AssertEqual(torrent, 1, expected);
    }

    [Test]
    public async Task FindTorrentByIdAsync_WhenGivenNonExistingTorrentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"{TestData.Endpoints.Torrents}/999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Detail, Is.EqualTo(string.Format(TestData.EndpointMessages.IdNotFound, 999)));
    }
}
