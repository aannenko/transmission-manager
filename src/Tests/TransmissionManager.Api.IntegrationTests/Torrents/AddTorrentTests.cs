using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Dto.Transmission;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class AddTorrentTests
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

    private const string _addNewTorrentResponseHashString = "3A81AAA70E75439D332C146ABDE899E546356BE2";
    private const int _addNewTorrentResponseId = 26;
    private const string _addNewTorrentResponseName = "TV Show 4";
    private static readonly string _addNewTorrentResponseBody = string.Format(
        null,
        TestData.Transmission.AddTorrentAddedResponseBodyFormat,
        _addNewTorrentResponseHashString,
        _addNewTorrentResponseId,
        _addNewTorrentResponseName);

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

    private TestWebApplicationFactory<Program> _factory = default!;
    private HttpClient _client = default!;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory<Program>(
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
    public async Task AddTorrentAsync_WhenWebPageUriIsNew_AddsTorrentToTransmissionAndDb()
    {
        var dto = new AddTorrentRequest
        {
            WebPageUri = new("https://torrenttracker.com/forum/viewtopic.php?t=1234570"),
            DownloadDir = "/tvshows",
            Cron = "0 9,17 * * *"
        };

        var response = await _client.PostAsJsonAsync(EndpointAddresses.Torrents, dto).ConfigureAwait(false);

        const long expectedId = 2;
        var expectedLocation = $"{EndpointAddresses.Torrents}/{expectedId}";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers.Location?.OriginalString, Is.EqualTo(expectedLocation));
        }

        var addTorrentResponse = await response.Content.ReadFromJsonAsync<AddTorrentResponse>().ConfigureAwait(false);

        Assert.That(addTorrentResponse, Is.Not.Null);
        Assert.That(addTorrentResponse.TransmissionResult, Is.EqualTo(TransmissionAddResult.Added));

        var expectedTorrent = new Torrent
        {
            Id = expectedId,
            HashString = _addNewTorrentResponseHashString,
            RefreshDate = DateTime.UtcNow,
            Name = _addNewTorrentResponseName,
            WebPageUri = dto.WebPageUri.OriginalString,
            DownloadDir = dto.DownloadDir,
            Cron = dto.Cron,
        };

        TorrentAssertions.AssertEqual(addTorrentResponse.TorrentDto, expectedTorrent, TimeSpan.FromSeconds(1));

        var getTorrentResponse = await _client.GetAsync(expectedLocation).ConfigureAwait(false);

        Assert.That(getTorrentResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var newTorrent = await getTorrentResponse.Content.ReadFromJsonAsync<TorrentDto>().ConfigureAwait(false);

        TorrentAssertions.AssertEqual(newTorrent, expectedTorrent, TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task AddTorrentAsync_WhenWebPageUriExists_ReturnsConflictResponse()
    {
        var dto = new AddTorrentRequest
        {
            WebPageUri = new(_initialTorrents[0].WebPageUri),
            DownloadDir = _initialTorrents[0].DownloadDir,
            Cron = _initialTorrents[0].Cron,
        };

        var response = await _client.PostAsJsonAsync(EndpointAddresses.Torrents, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            var error =
                $"Addition of a torrent from the web page '{dto.WebPageUri}' has failed: 'Torrent already exists.'.";

            Assert.That(problemDetails.Detail, Is.EqualTo(error));
            Assert.That(problemDetails.Extensions.TryGetValue("transmissionResult", out var transmissionResult));
            Assert.That(transmissionResult?.ToString(), Is.EqualTo("Duplicate"));
        }
    }

    [Test]
    public async Task AddTorrentAsync_WhenMagnetRegexPatternAndCronAreInvalid_ReturnsValidationErrors()
    {
        var dto = new AddTorrentRequest
        {
            WebPageUri = new("https://torrenttracker.com/forum/viewtopic.php?t=1234570"),
            DownloadDir = "/tvshows",
            MagnetRegexPattern = "",
            Cron = " "
        };

        var response = await _client.PostAsJsonAsync(EndpointAddresses.Torrents, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            const string title = "One or more validation errors occurred.";

            Assert.That(problemDetails.Title, Is.EqualTo(title));
            Assert.That(problemDetails.Extensions.TryGetValue("errors", out var errorObject));

            var errorString = errorObject?.ToString();

            Assert.That(errorString, Contains.Substring(nameof(AddTorrentRequest.MagnetRegexPattern)));
            Assert.That(errorString, Contains.Substring(nameof(AddTorrentRequest.Cron)));
        }
    }
}
