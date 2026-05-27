using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class UpdateTorrentByIdTests
{
    private static readonly Torrent[] _torrents = TestData.Database.CreateInitialTorrents();

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
    public async Task UpdateTorrentByIdAsync_WhenIdExistsAndRequestIsValid_UpdatesTorrent()
    {
        var dto = new UpdateTorrentByIdRequest
        {
            DownloadDir = "/videos",
            MagnetRegexPattern = "magnet:\\?xt=urn:[^\"]",
            Cron = "30 7,19 * * 3"
        };

        var torrentAddress = $"{EndpointAddresses.Torrents}/1";
        var patchAddress = $"{torrentAddress}?version=1";

        var response = await _client.PatchAsJsonAsync(patchAddress, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        response = await _client.GetAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var torrent = await response.Content.ReadFromJsonAsync<TorrentDto>().ConfigureAwait(false);

        Assert.That(torrent, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(torrent.DownloadDir, Is.EqualTo(dto.DownloadDir));
            Assert.That(torrent.MagnetRegexPattern, Is.EqualTo(dto.MagnetRegexPattern));
            Assert.That(torrent.Cron, Is.EqualTo(dto.Cron));
            Assert.That(torrent.Version, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenMagnetRegexPatternOrCronAreEmptyStrings_SetsTheirValuesToNull()
    {
        var dto = new UpdateTorrentByIdRequest
        {
            DownloadDir = "/videos",
            MagnetRegexPattern = "",
            Cron = ""
        };

        var torrentAddress = $"{EndpointAddresses.Torrents}/3";
        var patchAddress = $"{torrentAddress}?version=1";

        var response = await _client.PatchAsJsonAsync(patchAddress, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        response = await _client.GetAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var torrent = await response.Content.ReadFromJsonAsync<TorrentDto>().ConfigureAwait(false);

        Assert.That(torrent, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(torrent.DownloadDir, Is.EqualTo(dto.DownloadDir));
            Assert.That(torrent.MagnetRegexPattern, Is.Null);
            Assert.That(torrent.Cron, Is.Null);
        }
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenIdDoesNotExist_ReturnsNotFound()
    {
        var dto = new UpdateTorrentByIdRequest { DownloadDir = "/videos" };

        var response = await _client
            .PatchAsJsonAsync($"{EndpointAddresses.Torrents}/-1?version=1", dto)
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Detail, Is.EqualTo("Torrent '-1' update failed: 'No such torrent.'."));
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenVersionMismatches_ReturnsConflictWithCurrentVersion()
    {
        var dto = new UpdateTorrentByIdRequest { DownloadDir = "/videos" };

        var response = await _client
            .PatchAsJsonAsync($"{EndpointAddresses.Torrents}/2?version=999", dto)
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Extensions, Contains.Key("currentVersion"));

        var currentVersion = ((JsonElement)problemDetails.Extensions["currentVersion"]!).GetInt64();

        Assert.That(currentVersion, Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenVersionParameterMissing_ReturnsBadRequest()
    {
        var dto = new UpdateTorrentByIdRequest { DownloadDir = "/videos" };

        var response = await _client
            .PatchAsJsonAsync($"{EndpointAddresses.Torrents}/1", dto)
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key("version"));

        var versionErrors = problem.Errors["version"];

        Assert.That(versionErrors, Is.Not.Empty);
        Assert.That(versionErrors[0], Contains.Substring("must be between"));
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenVersionIsZero_ReturnsBadRequest()
    {
        var dto = new UpdateTorrentByIdRequest { DownloadDir = "/videos" };

        var response = await _client
            .PatchAsJsonAsync($"{EndpointAddresses.Torrents}/1?version=0", dto)
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key("version"));

        var versionErrors = problem.Errors["version"];

        Assert.That(versionErrors, Is.Not.Empty);
        Assert.That(versionErrors[0], Contains.Substring("must be between"));
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenAllFieldsAreNull_ReturnsBadRequest()
    {
        var dto = new UpdateTorrentByIdRequest();

        var response = await _client
            .PatchAsJsonAsync($"{EndpointAddresses.Torrents}/1?version=1", dto)
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key("DownloadDir"));

        var errors = problem.Errors["DownloadDir"];

        Assert.That(errors, Is.Not.Empty);
        Assert.That(errors[0], Contains.Substring("At least one field must be provided."));
    }
}
