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

/// <remarks>
/// Its own fixture rather than cases in <see cref="AddTorrentTests"/> and
/// <see cref="RefreshTorrentByIdTests"/>: a successful add inserts a row, and both of those fixtures
/// pin absolute torrent ids that SQLite's <c>AUTOINCREMENT</c> would shift.
/// </remarks>
[Parallelizable(ParallelScope.Self)]
internal sealed class JsonPointerSourceTests
{
    private const string _downloadDir = "/tvshows";
    private const string _refreshedTorrentName = "Refreshed via JSON";

    /// <remarks>
    /// Seeded with the hash the document already holds, so the refresh resolves to the same magnet
    /// and Transmission answers <c>Duplicate</c> - which exercises dispatch without dragging in the
    /// add-and-remove cycle a changed hash would require.
    /// </remarks>
    private static readonly Torrent[] _initialTorrents =
    [
        new()
        {
            Id = default,
            HashString = TestData.JsonApi.FirstHashString,
            Name = _refreshedTorrentName,
            SourceUri = $"{TestData.JsonApi.Address}{TestData.JsonApi.FirstPointer}",
            SourceKind = DbSourceKind.JsonPointer,
            DownloadDir = _downloadDir,
            RefreshDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Version = 1,
        },
    ];

    private static readonly TestResponse _invalidHeaderResponse = new(
        HttpStatusCode.Conflict,
        TestData.Transmission.ConflictResponseHeaders,
        TestData.Transmission.ConflictResponseBody);

    private static readonly string _getSeededTorrentBody = string.Format(
        null,
        TestData.Transmission.GetOneTorrentRequestBodyFormat,
        TestData.JsonApi.FirstHashString);

    private static readonly string _addSeededMagnetBody = string.Format(
        null,
        TestData.Transmission.AddTorrentRequestBodyFormat,
        TestData.JsonApi.FirstMagnet,
        _downloadDir);

    private static readonly string _addSecondMagnetBody = string.Format(
        null,
        TestData.Transmission.AddTorrentRequestBodyFormat,
        TestData.JsonApi.SecondMagnet,
        _downloadDir);

    private static readonly Dictionary<TestRequest, TestResponse> _transmissionRequestResponseMap = new()
    {
        [Request(TestData.Transmission.EmptyRequestHeaders, _getSeededTorrentBody)] = _invalidHeaderResponse,
        [Request(TestData.Transmission.FilledRequestHeaders, _getSeededTorrentBody)] =
            new(HttpStatusCode.OK,
                TestData.Transmission.DefaultResponseHeaders,
                string.Format(
                    null,
                    TestData.Transmission.GetOneTorrentResponseBodyFormat,
                    _downloadDir,
                    TestData.JsonApi.FirstHashString,
                    _refreshedTorrentName)),

        [Request(TestData.Transmission.EmptyRequestHeaders, _addSeededMagnetBody)] = _invalidHeaderResponse,
        [Request(TestData.Transmission.FilledRequestHeaders, _addSeededMagnetBody)] =
            new(HttpStatusCode.Created,
                TestData.Transmission.DefaultResponseHeaders,
                string.Format(
                    null,
                    TestData.Transmission.AddTorrentDuplicateResponseBodyFormat,
                    TestData.JsonApi.FirstHashString,
                    30,
                    _refreshedTorrentName)),

        [Request(TestData.Transmission.EmptyRequestHeaders, _addSecondMagnetBody)] = _invalidHeaderResponse,
        [Request(TestData.Transmission.FilledRequestHeaders, _addSecondMagnetBody)] =
            new(HttpStatusCode.Created,
                TestData.Transmission.DefaultResponseHeaders,
                string.Format(
                    null,
                    TestData.Transmission.AddTorrentAddedResponseBodyFormat,
                    TestData.JsonApi.SecondHashString,
                    31,
                    "Added via JSON")),
    };

    private TestWebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory<Program>(
            _initialTorrents,
            TestData.SourceRequestResponseMap,
            _transmissionRequestResponseMap);

        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async ValueTask TearDown()
    {
        _client?.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    /// <remarks>
    /// The first end-to-end exercise of a <c>JsonPointer</c> source. The magnet discriminates
    /// between the two clients: the JSON client synthesises a bare lower-cased magnet from the value
    /// at the pointer, while a page scrape of this address would find none at all.
    /// </remarks>
    [Test]
    public async Task AddTorrentAsync_WhenSourceKindIsJsonPointer_ResolvesThePointerAndPersistsTheKind()
    {
        var dto = new AddTorrentRequest
        {
            SourceUri = new($"{TestData.JsonApi.Address}{TestData.JsonApi.SecondPointer}"),
            SourceKind = TorrentSourceKind.JsonPointer,
            DownloadDir = _downloadDir,
        };

        var response = await _client.PostAsJsonAsync(EndpointAddresses.Torrents, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var result = await response.Content.ReadFromJsonAsync<AddTorrentResponse>().ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TransmissionResult, Is.EqualTo(TransmissionAddResult.Added));
            Assert.That(result.TorrentDto.SourceKind, Is.EqualTo(TorrentSourceKind.JsonPointer));
            Assert.That(result.TorrentDto.HashString, Is.EqualTo(TestData.JsonApi.SecondHashString));
            Assert.That(result.TorrentDto.SourceUri.OriginalString, Is.EqualTo(dto.SourceUri.OriginalString));
        }
    }

    /// <remarks>
    /// Proves the <b>stored</b> kind drives dispatch: a refresh request carries no source fields at
    /// all, so only <c>Torrent.SourceKind</c> can select the JSON client. This is the cron-driven
    /// path.
    /// </remarks>
    [Test]
    public async Task RefreshTorrentByIdAsync_WhenStoredKindIsJsonPointer_DispatchesToTheJsonClient()
    {
        var response = await _client
            .PostAsync($"{EndpointAddresses.Torrents}/1", null)
            .ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<RefreshTorrentByIdResponse>().ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TransmissionResult, Is.EqualTo(TransmissionAddResult.Duplicate));
            Assert.That(result.TorrentDto.SourceKind, Is.EqualTo(TorrentSourceKind.JsonPointer));
            Assert.That(result.TorrentDto.HashString, Is.EqualTo(TestData.JsonApi.FirstHashString));
        }
    }

    private static TestRequest Request(IReadOnlyDictionary<string, string> headers, string body) =>
        new(HttpMethod.Post, TestData.Transmission.ApiUri, headers, body);
}
