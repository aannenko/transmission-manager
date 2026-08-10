using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Dto.Transmission;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Database.Models;
using DbSourceKind = TransmissionManager.Database.Dto.TorrentSourceKind;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class RefreshTorrentByIdTests
{
    private const string CleanupWarningTorrentHashString = "4f0334f9eac58c76d128cdb4ba7a62c269e98559";
    private const string CleanupWarningTorrentUpdatedHashString = "3a81aaa70e75439d332c146abde899e546356be2";
    private const string CleanupWarningTorrentName = "TV Show 4";
    private const string CleanupWarningTorrentUpdatedName = "TV Show 4 Updated";
    private const string CleanupWarningTorrentWebPageAddress = "https://torrentTracker.com/forum/viewtopic.php?t=1234570";
    private const string CleanupWarningTorrentDownloadDir = "/tvshows";

    private const string NoMagnetTorrentHashString = "7c9e6679742d4f3e9a1b0c5d8e2f4a6b3d5c7e91";
    private const string NoMagnetTorrentName = "TV Show 5";
    private const string NoMagnetTorrentDownloadDir = "/tvshows";

    private static readonly DateTime _noMagnetTorrentRefreshDate =
        new(2021, 8, 3, 12, 34, 56, 789, DateTimeKind.Utc);

    private static readonly DateTime _cleanupWarningTorrentRefreshDate =
        new(2021, 9, 4, 13, 45, 57, 888, DateTimeKind.Utc);

    private static readonly Torrent[] _initialTorrents =
    [
        .. TestData.Database.CreateInitialTorrents(),
        new()
        {
            Id = default,
            HashString = CleanupWarningTorrentHashString,
            Name = CleanupWarningTorrentName,
            SourceUri = CleanupWarningTorrentWebPageAddress,
            SourceKind = DbSourceKind.WebPage,
            DownloadDir = CleanupWarningTorrentDownloadDir,
            RefreshDate = _cleanupWarningTorrentRefreshDate,
            Version = 1,
        },
        new()
        {
            Id = default,
            HashString = NoMagnetTorrentHashString,
            Name = NoMagnetTorrentName,
            SourceUri = TestData.WebPages.NoMagnetPageAddress,
            SourceKind = DbSourceKind.WebPage,
            DownloadDir = NoMagnetTorrentDownloadDir,
            RefreshDate = _noMagnetTorrentRefreshDate,
            Version = 1,
        },
    ];

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

    // Get Cleanup-Warning Torrent

    private static readonly string _getCleanupWarningTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.GetOneTorrentRequestBodyFormat,
        _initialTorrents[3].HashString);

    private static readonly TestRequest _getCleanupWarningTorrentInvalidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.EmptyRequestHeaders,
        _getCleanupWarningTorrentRequestBody);

    private static readonly TestRequest _getCleanupWarningTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _getCleanupWarningTorrentRequestBody);

    private static readonly string _getCleanupWarningTorrentResponseBody = string.Format(
        null,
        TestData.Transmission.GetOneTorrentResponseBodyFormat,
        _initialTorrents[3].DownloadDir,
        _initialTorrents[3].HashString,
        _initialTorrents[3].Name);

    private static readonly TestResponse _getCleanupWarningTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        _getCleanupWarningTorrentResponseBody);

    // Add Cleanup-Warning Torrent

    private static readonly string _addCleanupWarningTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.AddTorrentRequestBodyFormat,
        TestData.WebPages.FourthPageMagnetNew,
        _initialTorrents[3].DownloadDir);

    private static readonly TestRequest _addCleanupWarningTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _addCleanupWarningTorrentRequestBody);

    private static readonly string _addCleanupWarningTorrentResponseBody = string.Format(
        null,
        TestData.Transmission.AddTorrentAddedResponseBodyFormat,
        CleanupWarningTorrentUpdatedHashString,
        27,
        CleanupWarningTorrentUpdatedName);

    private static readonly TestResponse _addCleanupWarningTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        _addCleanupWarningTorrentResponseBody);

    // Remove Cleanup-Warning Torrent

    private static readonly string _removeCleanupWarningTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.DeleteTorrentRequestBodyFormat,
        _initialTorrents[3].HashString,
        "false");

    private static readonly TestRequest _removeCleanupWarningTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _removeCleanupWarningTorrentRequestBody);

    private static readonly TestResponse _removeCleanupWarningTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        """{"arguments":{},"result":"remove failed"}""");

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

    // Get No-Magnet Torrent

    private static readonly string _getNoMagnetTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.GetOneTorrentRequestBodyFormat,
        NoMagnetTorrentHashString);

    private static readonly TestRequest _getNoMagnetTorrentInvalidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.EmptyRequestHeaders,
        _getNoMagnetTorrentRequestBody);

    private static readonly TestRequest _getNoMagnetTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _getNoMagnetTorrentRequestBody);

    private static readonly TestResponse _getNoMagnetTorrentValidHeaderResponse = new(
        HttpStatusCode.OK,
        TestData.Transmission.DefaultResponseHeaders,
        string.Format(
            null,
            TestData.Transmission.GetOneTorrentResponseBodyFormat,
            NoMagnetTorrentDownloadDir,
            NoMagnetTorrentHashString,
            NoMagnetTorrentName));

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
        [_getCleanupWarningTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_getCleanupWarningTorrentValidHeaderRequest] = _getCleanupWarningTorrentValidHeaderResponse,
        [_addCleanupWarningTorrentValidHeaderRequest] = _addCleanupWarningTorrentValidHeaderResponse,
        [_removeCleanupWarningTorrentValidHeaderRequest] = _removeCleanupWarningTorrentValidHeaderResponse,
        [_getRemovedTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_getRemovedTorrentValidHeaderRequest] = _getRemovedTorrentValidHeaderResponse,
        [_getNoMagnetTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_getNoMagnetTorrentValidHeaderRequest] = _getNoMagnetTorrentValidHeaderResponse,
    };

    #endregion

    private TestWebApplicationFactory<Program> _factory;
    private HttpClient _client;

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
    public async Task RefreshTorrentByIdAsync_WhenIdExistsAndHashStringExistsInTransmission_RefreshesTorrentAndReturnsDuplicate()
    {
        var response = await _client.PostAsync($"{EndpointAddresses.Torrents}/1", null).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<RefreshTorrentByIdResponse>().ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.TransmissionResult, Is.EqualTo(TransmissionAddResult.Duplicate));
        TorrentAssertions.AssertEqual(result.TorrentDto, _initialTorrents[0]);
    }

    [Test]
    public async Task RefreshTorrentByIdAsync_WhenIdExistsAndHashStringIsOutdatedInTransmission_RefreshesTorrentAndReturnsAdded()
    {
        var response = await _client.PostAsync($"{EndpointAddresses.Torrents}/2", null).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<RefreshTorrentByIdResponse>().ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.TransmissionResult, Is.EqualTo(TransmissionAddResult.Added));

        // Successful refresh that calls torrent-add increments Version by 1.
        // Build a local Torrent so we don't mutate the static `_initialTorrents` shared by other tests.
        var source = _initialTorrents[1];
        var expected = new Torrent
        {
            Id = source.Id,
            HashString = source.HashString,
            Name = source.Name,
            SourceUri = source.SourceUri,
            SourceKind = source.SourceKind,
            DownloadDir = source.DownloadDir,
            MagnetRegexPattern = source.MagnetRegexPattern,
            Cron = source.Cron,
            RefreshDate = DateTime.Now,
            Version = source.Version + 1,
        };

        TorrentAssertions.AssertEqual(result.TorrentDto, expected, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task RefreshTorrentByIdAsync_WhenOldTorrentCleanupFails_ReturnsRefreshedTorrentWithWarning()
    {
        var response = await _client.PostAsync($"{EndpointAddresses.Torrents}/4", null).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<RefreshTorrentByIdResponse>().ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TransmissionResult, Is.EqualTo(TransmissionAddResult.Added));
            Assert.That(result.Message, Is.Not.Null.And.Not.Empty);
        }

        var source = _initialTorrents[3];
        var expected = new Torrent
        {
            Id = source.Id,
            HashString = CleanupWarningTorrentUpdatedHashString,
            Name = CleanupWarningTorrentUpdatedName,
            SourceUri = source.SourceUri,
            SourceKind = source.SourceKind,
            DownloadDir = source.DownloadDir,
            MagnetRegexPattern = source.MagnetRegexPattern,
            Cron = source.Cron,
            RefreshDate = DateTime.Now,
            Version = source.Version + 1,
        };

        TorrentAssertions.AssertEqual(result.TorrentDto, expected, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task RefreshTorrentByIdAsync_WhenIdExistsAndHashStringDoesNotExistInTransmission_Returns422UnprocessableEntity()
    {
        var response = await _client.PostAsync($"{EndpointAddresses.Torrents}/3", null).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
    }

    /// <remarks>
    /// The stored source is fetched successfully and simply holds no magnet, so the dependency did
    /// its job and only the extraction failed - the stored configuration is what must change, hence
    /// 422 rather than 424. This is the cron-driven path, so it is the one that fails unattended.
    /// </remarks>
    [Test]
    public async Task RefreshTorrentByIdAsync_WhenStoredSourceHoldsNoMagnet_Returns422UnprocessableEntity()
    {
        var response = await _client.PostAsync($"{EndpointAddresses.Torrents}/5", null).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Detail, Contains.Substring("No magnet link was found"));
    }
}
