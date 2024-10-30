using System.Net.Http.Json;
using System.Net;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Models;
using TransmissionManager.Api.UpdateTorrentById;
using Microsoft.AspNetCore.Mvc;

namespace TransmissionManager.Api.IntegrationTests;

[Parallelizable(ParallelScope.Self)]
public sealed class UpdateTorrentByIdTests
{
    private static readonly Torrent[] _torrents = TestData.Database.CreateInitialTorrents();

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
    public async Task UpdateTorrentByIdAsync_WhenGivenExistingIdAndValidData_UpdatesTorrent()
    {
        var dto = new UpdateTorrentByIdRequest
        {
            DownloadDir = "/videos",
            MagnetRegexPattern = "magnet:\\?xt=urn:[^\"]",
            Cron = "30 7,19 * * 3"
        };

        var torrentAddress = $"{TestData.Endpoints.Torrents}/1";

        var response = await _client.PatchAsJsonAsync(torrentAddress, dto);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        response = await _client.GetAsync(torrentAddress);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var torrent = await response.Content.ReadFromJsonAsync<Torrent>();

        Assert.That(torrent, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(torrent.DownloadDir, Is.EqualTo(dto.DownloadDir));
            Assert.That(torrent.MagnetRegexPattern, Is.EqualTo(dto.MagnetRegexPattern));
            Assert.That(torrent.Cron, Is.EqualTo(dto.Cron));
        });
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenGivenNonExistingId_ReturnsNotFound()
    {
        var dto = new UpdateTorrentByIdRequest { DownloadDir = "/videos" };

        var response = await _client.PatchAsJsonAsync($"{TestData.Endpoints.Torrents}/-1", dto);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Detail, Is.EqualTo("Torrent with id -1 was not found."));
    }
}
