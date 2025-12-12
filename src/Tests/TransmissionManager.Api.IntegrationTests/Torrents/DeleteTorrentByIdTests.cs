using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class DeleteTorrentByIdTests
{
    private static readonly Torrent[] _initialTorrents = TestData.Database.CreateInitialTorrents()[0..2];

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

    private TestWebApplicationFactory<Program> _factory = default!;
    private HttpClient _client = default!;

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

        var response = await _client.DeleteAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        response = await _client.GetAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Detail, Is.EqualTo("Torrent with id 1 was not found."));
    }

    [Test]
    public async Task DeleteTorrentByIdAsync_WhenIdExistsAndRemoveDataFlagUsed_DeletesTorrentAndTransmissionData()
    {
        var torrentAddress = $"{EndpointAddresses.Torrents}/2?deleteType=LocalAndTransmissionAndData";

        var response = await _client.DeleteAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        response = await _client.GetAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Detail, Is.EqualTo("Torrent with id 2 was not found."));
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenIdDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"{EndpointAddresses.Torrents}/-1").ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(
            problemDetails.Detail,
            Is.EqualTo("Removal of the torrent with id -1 has failed: 'No such torrent.'."));
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenIdDoesNotExistAndFlagToRemoveDataUsed_ReturnsNotFound()
    {
        var torrentAddress = $"{EndpointAddresses.Torrents}/-1?deleteType=LocalAndTransmission";

        var response = await _client.DeleteAsync(torrentAddress).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(
            problemDetails.Detail,
            Is.EqualTo("Removal of the torrent with id -1 has failed: 'No such torrent.'."));
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenInvalidFlagToRemoveDataUsed_ReturnsProblemDetails()
    {
        var torrentAddress = $"{EndpointAddresses.Torrents}/1?deleteType=999";
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
}
