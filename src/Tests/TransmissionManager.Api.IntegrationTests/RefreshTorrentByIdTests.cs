using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Actions.RefreshTorrentById;
using TransmissionManager.Api.Common.Transmission;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests;

[Parallelizable(ParallelScope.Self)]
internal sealed class RefreshTorrentByIdTests
{
    private static readonly Torrent[] _initialTorrents = TestData.Database.CreateInitialTorrents();

    #region Transmission Test Data

    // Common

    private static readonly TestResponse _invalidHeaderResponse = new(
        HttpStatusCode.Conflict,
        TestData.Transmission.ConflictResponseHeaders,
        TestData.Transmission.ConflictResponseBody);

    // Get Duplicate Torrent

    private static readonly string _getDuplicateTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.GetOneTorrentRequestBodyFormat,
        _initialTorrents[0].HashString);

    private static readonly TestRequest _getDuplicateTorrentInvalidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.EmptyRequestHeaders,
        _getDuplicateTorrentRequestBody);

    private static readonly TestRequest _getDuplicateTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _getDuplicateTorrentRequestBody);

    private static readonly string _getDuplicateTorrentResponseBody = string.Format(
        null,
        TestData.Transmission.GetOneTorrentResponseBodyFormat,
        _initialTorrents[0].DownloadDir,
        _initialTorrents[0].HashString,
        _initialTorrents[0].Name);

    private static readonly TestResponse _getDuplicateTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        _getDuplicateTorrentResponseBody);

    // Add Duplicate Torrent

    private static readonly string _addDuplicateTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.AddTorrentRequestBodyFormat,
        TestData.WebPages.FirstPageMagnetExisting,
        _initialTorrents[0].DownloadDir);

    private static readonly TestRequest _addDuplicateTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _addDuplicateTorrentRequestBody);

    private static readonly string _addDuplicateTorrentResponseBody = string.Format(
        null,
        TestData.Transmission.AddTorrentDuplicateResponseBodyFormat,
        _initialTorrents[0].HashString,
        25,
        _initialTorrents[0].Name);

    private static readonly TestResponse _addDuplicateTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        _addDuplicateTorrentResponseBody);

    // Get Outdated Torrent

    private static readonly string _getOutdatedTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.GetOneTorrentRequestBodyFormat,
        _initialTorrents[1].HashString);

    private static readonly TestRequest _getOutdatedTorrentInvalidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.EmptyRequestHeaders,
        _getOutdatedTorrentRequestBody);

    private static readonly TestRequest _getOutdatedTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _getOutdatedTorrentRequestBody);

    private static readonly string _getOutdatedTorrentResponseBody = string.Format(
        null,
        TestData.Transmission.GetOneTorrentResponseBodyFormat,
        _initialTorrents[1].DownloadDir,
        _initialTorrents[1].HashString,
        _initialTorrents[1].Name);

    private static readonly TestResponse _getOutdatedTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        _getOutdatedTorrentResponseBody);

    // Add Updated Torrent

    private static readonly string _addUpdatedTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.AddTorrentRequestBodyFormat,
        TestData.WebPages.SecondPageMagnetUpdated,
        _initialTorrents[1].DownloadDir);

    private static readonly TestRequest _addUpdatedTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _addUpdatedTorrentRequestBody);

    private static readonly string _addUpdatedTorrentResponseBody = string.Format(
        null,
        TestData.Transmission.AddTorrentAddedResponseBodyFormat,
        _initialTorrents[1].HashString,
        26,
        _initialTorrents[1].Name);

    private static readonly TestResponse _addOutdatedTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        _addUpdatedTorrentResponseBody);

    // Remove Outdated Torrent

    private static readonly string _removeOutdatedTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.DeleteTorrentRequestBodyFormat,
        _initialTorrents[1].HashString,
        "false");

    private static readonly TestRequest _removeOutdatedTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _removeOutdatedTorrentRequestBody);

    private static readonly TestResponse _removeOutdatedTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        TestData.Transmission.DeleteTorrentResponseBody);

    // Get Removed Torrent

    private static readonly string _getRemovedTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.GetOneTorrentRequestBodyFormat,
        _initialTorrents[2].HashString);

    private static readonly TestRequest _getRemovedTorrentInvalidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.EmptyRequestHeaders,
        _getRemovedTorrentRequestBody);

    private static readonly TestRequest _getRemovedTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _getRemovedTorrentRequestBody);

    private static readonly TestResponse _getRemovedTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        TestData.Transmission.GetOneTorrentNotFoundResponseBody);

    // Request-Response map

    private static readonly Dictionary<TestRequest, TestResponse> _transmissionRequestResponseMap = new()
    {
        [_getDuplicateTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_getDuplicateTorrentValidHeaderRequest] = _getDuplicateTorrentValidHeaderResponse,
        [_addDuplicateTorrentValidHeaderRequest] = _addDuplicateTorrentValidHeaderResponse,
        [_getOutdatedTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_getOutdatedTorrentValidHeaderRequest] = _getOutdatedTorrentValidHeaderResponse,
        [_addUpdatedTorrentValidHeaderRequest] = _addOutdatedTorrentValidHeaderResponse,
        [_removeOutdatedTorrentValidHeaderRequest] = _removeOutdatedTorrentValidHeaderResponse,
        [_getRemovedTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_getRemovedTorrentValidHeaderRequest] = _getRemovedTorrentValidHeaderResponse,
    };

    #endregion

    private TestWebAppliationFactory<Program> _factory = default!;
    private HttpClient _client = default!;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestWebAppliationFactory<Program>(
            _initialTorrents,
            TestData.WebPages.RequestResponseMap,
            _transmissionRequestResponseMap);

        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async ValueTask TearDown()
    {
        _client?.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task RefreshTorrentByIdAsync_WhenGivenExistingTorrentIdWithCurrentHash_RefreshesTorrentAndReturnsDuplicate()
    {
        var response = await _client.PostAsync($"{TestData.Endpoints.Torrents}/1", null).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<RefreshTorrentByIdResponse>().ConfigureAwait(false);

        Assert.That(result.TransmissionResult, Is.EqualTo(TransmissionAddResult.Duplicate));
    }

    [Test]
    public async Task RefreshTorrentByIdAsync_WhenGivenExistingTorrentIdWithOutdatedHash_RefreshesTorrentAndReturnsAdded()
    {
        var response = await _client.PostAsync($"{TestData.Endpoints.Torrents}/2", null).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<RefreshTorrentByIdResponse>().ConfigureAwait(false);

        Assert.That(result.TransmissionResult, Is.EqualTo(TransmissionAddResult.Added));
    }

    [Test]
    public async Task RefreshTorrentByIdAsync_WhenGivenExistingTorrentIdWithNonExistentHash_Returns422UnprocessableEntity()
    {
        var response = await _client.PostAsync($"{TestData.Endpoints.Torrents}/3", null).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
    }
}
