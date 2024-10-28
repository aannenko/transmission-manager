using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Api.RefreshTorrentById;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests;

[Parallelizable(ParallelScope.Self)]
public sealed class RefreshTorrentByIdTests
{
    private static readonly Torrent[] _initialTorrents = TestData.Database.CreateInitialTorrents();

    #region Transmission Test Data

    // Common

    private static readonly TestResponse _invalidHeaderResponse = new(
        HttpStatusCode.Conflict,
        TestData.Transmission.ConflictResponseHeaders,
        TestData.Transmission.ConflictResponseBody);

    // Get Existing Torrent

    private static readonly string _getExistingTorrentRequestBody = string.Format(
        TestData.Transmission.GetOneTorrentRequestBody,
        _initialTorrents[0].HashString);

    private static readonly TestRequest _getExistingTorrentInvalidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.EmptyRequestHeaders,
        _getExistingTorrentRequestBody);

    private static readonly TestRequest _getExistingTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _getExistingTorrentRequestBody);

    private static readonly string _getExistingTorrentResponseBody = string.Format(
        TestData.Transmission.GetOneTorrentResponseBody,
        _initialTorrents[0].DownloadDir,
        _initialTorrents[0].HashString,
        _initialTorrents[0].Name);

    private static readonly TestResponse _getExistingTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        _getExistingTorrentResponseBody);

    // Add Existing Torrent

    private static readonly string _addExistingTorrentRequestBody = string.Format(
        TestData.Transmission.AddTorrentRequestBody,
        TestData.WebPages.FirstPageMagnetExisting,
        _initialTorrents[0].DownloadDir);

    private static readonly TestRequest _addExistingTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _addExistingTorrentRequestBody);

    private static readonly string _addExistingTorrentResponseBody = string.Format(
        TestData.Transmission.AddTorrentDuplicateResponseBody,
        _initialTorrents[0].HashString,
        25,
        _initialTorrents[0].Name);

    private static readonly TestResponse _addExistingTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        _addExistingTorrentResponseBody);

    // Get Outdated Torrent

    private static readonly string _getOutdatedTorrentRequestBody = string.Format(
        TestData.Transmission.GetOneTorrentRequestBody,
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
        TestData.Transmission.GetOneTorrentResponseBody,
        _initialTorrents[1].DownloadDir,
        _initialTorrents[1].HashString,
        _initialTorrents[1].Name);

    private static readonly TestResponse _getOutdatedTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        _getOutdatedTorrentResponseBody);

    // Add Outdated Torrent

    private static readonly string _addOutdatedTorrentRequestBody = string.Format(
        TestData.Transmission.AddTorrentRequestBody,
        TestData.WebPages.SecondPageMagnetUpdated,
        _initialTorrents[1].DownloadDir);

    private static readonly TestRequest _addOutdatedTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _addOutdatedTorrentRequestBody);

    private static readonly string _addOutdatedTorrentResponseBody = string.Format(
        TestData.Transmission.AddTorrentAddedResponseBody,
        _initialTorrents[1].HashString,
        26,
        _initialTorrents[1].Name);

    private static readonly TestResponse _addOutdatedTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        _addOutdatedTorrentResponseBody);

    // Get Removed Torrent

    private static readonly string _getRemovedTorrentRequestBody = string.Format(
        TestData.Transmission.GetOneTorrentRequestBody,
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
        [_getExistingTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_getExistingTorrentValidHeaderRequest] = _getExistingTorrentValidHeaderResponse,
        [_addExistingTorrentValidHeaderRequest] = _addExistingTorrentValidHeaderResponse,
        [_getOutdatedTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_getOutdatedTorrentValidHeaderRequest] = _getOutdatedTorrentValidHeaderResponse,
        [_addOutdatedTorrentValidHeaderRequest] = _addOutdatedTorrentValidHeaderResponse,
        [_getRemovedTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_getRemovedTorrentValidHeaderRequest] = _getRemovedTorrentValidHeaderResponse,
    };

    #endregion

    private TestWebAppliationFactory<Program> _factory;
    private HttpClient _client;

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
        var response = await _client.PostAsync($"{TestData.Endpoints.Torrents}/1", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<RefreshTorrentByIdResponse?>();

        Assert.That(result?.TransmissionResult.ToString(), Is.EqualTo("Duplicate"));
    }

    [Test]
    public async Task RefreshTorrentByIdAsync_WhenGivenExistingTorrentIdWithOutdatedHash_RefreshesTorrentAndReturnsAdded()
    {
        var response = await _client.PostAsync($"{TestData.Endpoints.Torrents}/2", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<RefreshTorrentByIdResponse?>();

        Assert.That(result?.TransmissionResult.ToString(), Is.EqualTo("Added"));
    }

    [Test]
    public async Task RefreshTorrentByIdAsync_WhenGivenExistingTorrentIdWithNonExistingHash_RefreshesTorrentAndReturns422()
    {
        var response = await _client.PostAsync($"{TestData.Endpoints.Torrents}/3", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
    }
}
