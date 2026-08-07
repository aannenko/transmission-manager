using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class DeleteTorrentByIdTests
{
    private static readonly Torrent[] _initialTorrents = TestData.Database.CreateInitialTorrents();

    #region Transmission Test Data

    // Common

    private static readonly TestResponse _invalidHeaderResponse = new(
        HttpStatusCode.Conflict,
        TestData.Transmission.ConflictResponseHeaders,
        TestData.Transmission.ConflictResponseBody);

    // Delete Torrent

    private static readonly string _deleteTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.DeleteTorrentRequestBodyFormat,
        _initialTorrents[1].HashString,
        "true");

    private static readonly TestRequest _deleteTorrentInvalidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.EmptyRequestHeaders,
        _deleteTorrentRequestBody);

    private static readonly TestRequest _deleteTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _deleteTorrentRequestBody);

    private static readonly TestResponse _deleteTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        TestData.Transmission.DeleteTorrentResponseBody);

    // Request-Response map

    private static readonly Dictionary<TestRequest, TestResponse> _transmissionRequestResponseMap = new()
    {
        [_deleteTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_deleteTorrentValidHeaderRequest] = _deleteTorrentValidHeaderResponse
    };

    #endregion

    private TestWebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory<Program>(_initialTorrents, null, _transmissionRequestResponseMap);
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async ValueTask TearDown()
    {
        _client?.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task DeleteTorrentByIdAsync_WhenIdExists_DeletesTorrent()
    {
        var torrentAddress = $"{EndpointAddresses.Torrents}/1";

        var response = await _client.DeleteAsync($"{torrentAddress}?version=1").ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        response = await _client.GetAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Detail, Is.EqualTo("Torrent '1' retrieval failed: 'No such torrent.'."));
    }

    [Test]
    public async Task DeleteTorrentByIdAsync_WhenIdExistsAndRemoveDataFlagUsed_DeletesTorrentAndTransmissionData()
    {
        var torrentAddress = $"{EndpointAddresses.Torrents}/2?version=1&deleteType=LocalAndTransmissionAndData";

        var response = await _client.DeleteAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        response = await _client.GetAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Detail, Is.EqualTo("Torrent '2' retrieval failed: 'No such torrent.'."));
    }

    [Test]
    public async Task DeleteTorrentByIdAsync_WhenIdDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"{EndpointAddresses.Torrents}/-1?version=1").ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(
            problemDetails.Detail,
            Is.EqualTo("Torrent '-1' deletion failed: 'No such torrent.'."));
    }

    [Test]
    public async Task DeleteTorrentByIdAsync_WhenIdDoesNotExistAndFlagToRemoveDataUsed_ReturnsNotFound()
    {
        var torrentAddress = $"{EndpointAddresses.Torrents}/-1?version=1&deleteType=LocalAndTransmission";

        var response = await _client.DeleteAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(
            problemDetails.Detail,
            Is.EqualTo("Torrent '-1' deletion failed: 'No such torrent.'."));
    }

    [Test]
    public async Task DeleteTorrentByIdAsync_WhenInvalidFlagToRemoveDataUsed_ReturnsProblemDetails()
    {
        var torrentAddress = $"{EndpointAddresses.Torrents}/1?version=1&deleteType=999";
        // deleteType=InvalidFlag returns problem details without the Errors dict

        var response = await _client.DeleteAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Errors, Has.Count.EqualTo(1));
            Assert.That(problem.Errors, Contains.Key("deleteType"));
            if (problem.Errors.TryGetValue("deleteType", out var errors))
            {
                Assert.That(errors, Has.Length.EqualTo(1));
                Assert.That(errors[0], Is.EqualTo("The field deleteType is invalid."));
            }
        }
    }

    [Test]
    public async Task DeleteTorrentByIdAsync_WhenLocalAndVersionMismatch_ReturnsConflictAndCurrentVersion()
    {
        var response = await _client
            .DeleteAsync($"{EndpointAddresses.Torrents}/3?version=999")
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Extensions, Contains.Key("currentVersion"));

        var currentVersion = ((JsonElement)problemDetails.Extensions["currentVersion"]!).GetInt64();

        Assert.That(currentVersion, Is.EqualTo(1));

        // Row should still exist after a conflict
        var get = await _client.GetAsync($"{EndpointAddresses.Torrents}/3").ConfigureAwait(false);
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task DeleteTorrentByIdAsync_WhenNonLocalAndVersionMismatch_ReturnsConflictAndDoesNotCallTransmission()
    {
        // Stale version on a non-local delete must short-circuit before the Transmission RPC.
        // Transmission mock has no mapping for id=3's hash, so any RPC would yield 424 or 5xx, never 409.
        var response = await _client
            .DeleteAsync($"{EndpointAddresses.Torrents}/3?version=999&deleteType=LocalAndTransmissionAndData")
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Extensions, Contains.Key("currentVersion"));

        var currentVersion = ((JsonElement)problemDetails.Extensions["currentVersion"]!).GetInt64();

        Assert.That(currentVersion, Is.EqualTo(1));

        // Row should still exist after a conflict.
        var get = await _client.GetAsync($"{EndpointAddresses.Torrents}/3").ConfigureAwait(false);

        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task DeleteTorrentByIdAsync_WhenVersionParameterMissing_ReturnsBadRequest()
    {
        var response = await _client
            .DeleteAsync($"{EndpointAddresses.Torrents}/3")
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
    public async Task DeleteTorrentByIdAsync_WhenVersionParameterNegative_ReturnsBadRequest()
    {
        var response = await _client
            .DeleteAsync($"{EndpointAddresses.Torrents}/3?version=-1")
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key("version"));

        var versionErrors = problem.Errors["version"];

        Assert.That(versionErrors, Is.Not.Empty);
        Assert.That(versionErrors[0], Contains.Substring("must be between"));
    }
}
