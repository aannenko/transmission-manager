using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class UpdateTorrentByIdTests
{
    private static readonly Torrent[] _torrents = TestData.Database.CreateInitialTorrents();

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
    public async Task UpdateTorrentByIdAsync_WhenIdExistsAndRequestIsValid_UpdatesTorrent()
    {
        var dto = new UpdateTorrentByIdRequest
        {
            DownloadDir = "/videos",
            MagnetRegexPattern = "magnet:\\?xt=urn:[^\"]",
            Cron = "30 7,19 * * 3"
        };

        var torrentAddress = $"{EndpointAddresses.Torrents}/1";

        var response = await _client.PatchAsJsonAsync(torrentAddress, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        response = await _client.GetAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var torrent = await response.Content.ReadFromJsonAsync<Torrent>().ConfigureAwait(false);

        Assert.That(torrent, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(torrent!.DownloadDir, Is.EqualTo(dto.DownloadDir));
            Assert.That(torrent.MagnetRegexPattern, Is.EqualTo(dto.MagnetRegexPattern));
            Assert.That(torrent.Cron, Is.EqualTo(dto.Cron));
        }
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenIdDoesNotExist_ReturnsNotFound()
    {
        var dto = new UpdateTorrentByIdRequest { DownloadDir = "/videos" };

        var response = await _client.PatchAsJsonAsync($"{EndpointAddresses.Torrents}/-1", dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails!.Detail, Is.EqualTo("Torrent with id -1 was not found."));
    }
}
