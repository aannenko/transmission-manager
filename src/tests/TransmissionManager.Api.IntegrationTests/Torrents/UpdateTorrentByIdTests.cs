using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Validation;
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

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key("id"));
        Assert.That(problem.Errors["id"], Is.EqualTo(["No such torrent."]));
    }

    /// <remarks>
    /// The conflict is reported against <c>version</c>, the parameter that has to change - the same
    /// key the framework itself uses when it rejects that parameter.
    /// </remarks>
    [Test]
    public async Task UpdateTorrentByIdAsync_WhenVersionMismatches_ReturnsConflictWithCurrentVersion()
    {
        var dto = new UpdateTorrentByIdRequest { DownloadDir = "/videos" };

        var response = await _client
            .PatchAsJsonAsync($"{EndpointAddresses.Torrents}/2?version=999", dto)
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Errors, Contains.Key("version"));
            Assert.That(problem.Extensions, Contains.Key("currentVersion"));
        }

        Assert.That(problem.Errors["version"], Is.EqualTo(["The torrent has been modified by another client."]));

        var currentVersion = ((JsonElement)problem.Extensions["currentVersion"]!).GetInt64();

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

    /// <remarks>
    /// The request carries no source kind, so only the stored torrent - a web page one here - says
    /// which rules apply. Without this the pattern would be stored as written and fail at the next
    /// refresh instead.
    /// <para>
    /// These cases all use torrent 2: a rejected request never reaches the write, so nothing is
    /// mutated and no version is consumed whatever order the fixture runs in.
    /// </para>
    /// </remarks>
    [Test]
    public async Task UpdateTorrentByIdAsync_WhenPatternDoesNotLookForAMagnetOnAWebPageTorrent_ReturnsBadRequest()
    {
        var dto = new UpdateTorrentByIdRequest { MagnetRegexPattern = "[a-fA-F0-9]{40}" };

        var response = await _client
            .PatchAsJsonAsync($"{EndpointAddresses.Torrents}/2?version=1", dto)
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key(nameof(UpdateTorrentByIdRequest.MagnetRegexPattern)));
        Assert.That(
            problem.Errors[nameof(UpdateTorrentByIdRequest.MagnetRegexPattern)],
            Is.EqualTo(["Invalid regex for magnet link search."]));
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenMagnetFormatIsSetOnAWebPageTorrent_ReturnsBadRequest()
    {
        var dto = new UpdateTorrentByIdRequest { JsonValueFormat = "magnet:?xt=urn:btih:{0}" };

        var response = await _client
            .PatchAsJsonAsync($"{EndpointAddresses.Torrents}/2?version=1", dto)
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key(nameof(UpdateTorrentByIdRequest.JsonValueFormat)));
        Assert.That(
            problem.Errors[nameof(UpdateTorrentByIdRequest.JsonValueFormat)],
            Is.EqualTo(["A magnet format is only used by a JsonPointer source."]));
    }

    /// <remarks>
    /// Each fault is reported against the field that carries it, so a client can show both without
    /// taking either apart.
    /// </remarks>
    [Test]
    public async Task UpdateTorrentByIdAsync_WhenBothSourceSettingsAreWrongForTheKind_ReportsBoth()
    {
        var dto = new UpdateTorrentByIdRequest
        {
            MagnetRegexPattern = "[a-fA-F0-9]{40}",
            JsonValueFormat = "magnet:?xt=urn:btih:{0}"
        };

        var response = await _client
            .PatchAsJsonAsync($"{EndpointAddresses.Torrents}/2?version=1", dto)
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Errors, Contains.Key(nameof(UpdateTorrentByIdRequest.MagnetRegexPattern)));
            Assert.That(problem.Errors, Contains.Key(nameof(UpdateTorrentByIdRequest.JsonValueFormat)));
        }
    }

    /// <remarks>
    /// A pattern in the body is what makes the handler read the torrent first, so a missing torrent
    /// must still come back as one.
    /// </remarks>
    [Test]
    public async Task UpdateTorrentByIdAsync_WhenIdDoesNotExistAndPatternIsPresent_ReturnsNotFound()
    {
        var dto = new UpdateTorrentByIdRequest { MagnetRegexPattern = "[a-fA-F0-9]{40}" };

        var response = await _client
            .PatchAsJsonAsync($"{EndpointAddresses.Torrents}/-1?version=1", dto)
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key("id"));
        Assert.That(problem.Errors["id"], Is.EqualTo(["No such torrent."]));
    }

    /// <remarks>
    /// The pattern is shaped like a magnet search and passes that rule, but cannot be built at all.
    /// The update writes a pattern without building it, so before this the torrent stored it and
    /// failed at the next refresh.
    /// </remarks>
    [Test]
    public async Task UpdateTorrentByIdAsync_WhenPatternCannotParse_ReturnsBadRequestAndDoesNotStoreIt()
    {
        const string brokenPattern = @"magnet:\?xt=(";
        var torrentAddress = $"{EndpointAddresses.Torrents}/2";

        var response = await _client
            .PatchAsJsonAsync($"{torrentAddress}?version=1", new UpdateTorrentByIdRequest
            {
                MagnetRegexPattern = brokenPattern
            })
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key(nameof(UpdateTorrentByIdRequest.MagnetRegexPattern)));

        // Asserts on the pattern the message quotes back, not the framework's wording around it,
        // which is localisable and has been reworded between releases.
        Assert.That(
            problem.Errors[nameof(UpdateTorrentByIdRequest.MagnetRegexPattern)],
            Has.One.Contains(brokenPattern));

        response = await _client.GetAsync(torrentAddress).ConfigureAwait(false);
        var torrent = await response.Content.ReadFromJsonAsync<TorrentDto>().ConfigureAwait(false);

        Assert.That(torrent, Is.Not.Null);
        Assert.That(torrent.MagnetRegexPattern, Is.Not.EqualTo(brokenPattern));
    }

    /// <remarks>
    /// The cap bounds what validating a pattern costs, so what matters is that an attribute enforces
    /// it - stopping the request before anything builds one.
    /// </remarks>
    [Test]
    public async Task UpdateTorrentByIdAsync_WhenPatternIsLongerThanTheCap_ReturnsBadRequest()
    {
        var dto = new UpdateTorrentByIdRequest
        {
            MagnetRegexPattern = new string('a', TorrentSourceRules.MaxPatternLength + 1)
        };

        var response = await _client
            .PatchAsJsonAsync($"{EndpointAddresses.Torrents}/2?version=1", dto)
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key(nameof(UpdateTorrentByIdRequest.MagnetRegexPattern)));
    }
}
