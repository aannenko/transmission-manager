using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Actions.AddTorrent;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests;

[Parallelizable(ParallelScope.Self)]
public sealed class AddTorrentTests
{
    private static readonly Torrent[] _initialTorrents = [TestData.Database.CreateInitialTorrents()[0]];

    #region Transmission Test Data

    // Common

    private static readonly TestResponse _invalidHeaderResponse = new(
        HttpStatusCode.Conflict,
        TestData.Transmission.ConflictResponseHeaders,
        TestData.Transmission.ConflictResponseBody);

    // Add New Torrent

    private static readonly string _addNewTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.AddTorrentRequestBodyFormat,
        TestData.WebPages.FourthPageMagnetNew,
        _initialTorrents[0].DownloadDir);

    private static readonly TestRequest _addNewTorrentInvalidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.EmptyRequestHeaders,
        _addNewTorrentRequestBody);

    private static readonly TestRequest _addNewTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _addNewTorrentRequestBody);

    private static readonly string _addNewTorrentResponseBody = string.Format(
        null,
        TestData.Transmission.AddTorrentAddedResponseBodyFormat,
        "3A81AAA70E75439D332C146ABDE899E546356BE2",
        26,
        "TV Show 4");

    private static readonly TestResponse _addNewTorrentValidHeaderResponse = new(
        HttpStatusCode.Created,
        TestData.Transmission.DefaultResponseHeaders,
        _addNewTorrentResponseBody);

    // Add Existing Torrent

    private static readonly string _addExistingTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.AddTorrentRequestBodyFormat,
        TestData.WebPages.FirstPageMagnetExisting,
        _initialTorrents[0].DownloadDir);

    private static readonly string _addExistingTorrentResponseBody = string.Format(
        null,
        TestData.Transmission.AddTorrentDuplicateResponseBodyFormat,
        _initialTorrents[0].HashString,
        25,
        _initialTorrents[0].Name);

    private static readonly TestRequest _addExistingTorrentInvalidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.EmptyRequestHeaders,
        _addExistingTorrentRequestBody);

    private static readonly TestRequest _addExistingTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _addExistingTorrentRequestBody);

    private static readonly TestResponse _addExistingTorrentValidHeaderResponse = new(
        HttpStatusCode.Created,
        TestData.Transmission.DefaultResponseHeaders,
        _addExistingTorrentResponseBody);

    // Request-Response map

    private static readonly Dictionary<TestRequest, TestResponse> _transmissionRequestResponseMap = new()
    {
        [_addNewTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_addNewTorrentValidHeaderRequest] = _addNewTorrentValidHeaderResponse,
        [_addExistingTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_addExistingTorrentValidHeaderRequest] = _addExistingTorrentValidHeaderResponse,
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
    public async Task AddTorrentAsync_WhenGivenNewTorrentUri_AddsTorrentToTransmissionAndDb()
    {
        var dto = new AddTorrentRequest
        {
            WebPageUri = new("https://torrenttracker.com/forum/viewtopic.php?t=1234570"),
            DownloadDir = "/tvshows",
            Cron = "0 9,17 * * *"
        };

        var response = await _client.PostAsJsonAsync(TestData.Endpoints.Torrents, dto).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            var location = $"{TestData.Endpoints.Torrents}/2";
            Assert.That(response.Headers.Location?.OriginalString, Is.EqualTo(location));
        });
    }

    [Test]
    public async Task AddTorrentAsync_WhenGivenExistingTorrentUri_ReturnsConflictResponse()
    {
        var dto = new AddTorrentRequest
        {
            WebPageUri = _initialTorrents[0].WebPageUri,
            DownloadDir = _initialTorrents[0].DownloadDir,
            Cron = _initialTorrents[0].Cron,
        };

        var response = await _client.PostAsJsonAsync(TestData.Endpoints.Torrents, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.Multiple(() =>
        {
            const string error = "Addition of a torrent from the web page '{0}' has failed: 'Torrent already exists.'.";
            Assert.That(problemDetails.Detail, Is.EqualTo(string.Format(CultureInfo.InvariantCulture, error, dto.WebPageUri)));
            Assert.That(problemDetails.Extensions.TryGetValue("transmissionResult", out var transmissionResult));
            Assert.That(transmissionResult?.ToString(), Is.EqualTo("Duplicate"));
        });
    }
}
