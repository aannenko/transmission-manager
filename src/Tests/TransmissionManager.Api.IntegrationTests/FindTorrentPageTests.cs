using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.FindTorrentPage;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests;

[Parallelizable(ParallelScope.Self)]
public sealed class FindTorrentPageTests
{
    private static readonly Torrent[] _torrents = TestData.Database.CreateInitialTorrents();

    private TestWebAppliationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestWebAppliationFactory<Program>(_torrents, null, null);
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async ValueTask TearDown()
    {
        _client?.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenCorrectPageDescriptor_ReturnsMatchingTorrents()
    {
        const string query = $"{TestData.Endpoints.Torrents}?take=5&afterId=1";
        var response = await _client.GetAsync(query);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<FindTorrentPageResponse>();

        Assert.That(page, Is.Not.Default);
        Assert.Multiple(() =>
        {
            Assert.That(page.Torrents, Has.Length.EqualTo(2));
            const string nextPageQuery = $"{TestData.Endpoints.Torrents}?take=5&afterId=3";
            Assert.That(page.NextPageAddress, Is.EqualTo(nextPageQuery));
        });

        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 3, _torrents[2]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenCorrectNameAndCronFilters_ReturnsMatchingTorrents()
    {
        const string query = $"{TestData.Endpoints.Torrents}?nameStartsWith=TV+Show&cronExists=True";
        var response = await _client.GetAsync(query);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<FindTorrentPageResponse>();

        Assert.That(page, Is.Not.Default);
        Assert.Multiple(() =>
        {
            Assert.That(page.Torrents, Has.Length.EqualTo(2));
            const string nextPageQuery = TestData.Endpoints.Torrents +
                "?take=20&afterId=3&nameStartsWith=TV+Show&cronExists=True";

            Assert.That(page.NextPageAddress, Is.EqualTo(nextPageQuery));
        });

        TorrentAssertions.AssertEqual(page.Torrents[0], 1, _torrents[0]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 3, _torrents[2]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenCorrectUriAndHashStringFilters_ReturnsMatchingTorrent()
    {
        var query = TestData.Endpoints.Torrents +
            $"?webPageUri={WebUtility.UrlEncode(TestData.Database.SecondTorrentWebPageUri)}" +
            $"&hashString={TestData.Database.SecondTorrentHashString}";

        var response = await _client.GetAsync(query);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<FindTorrentPageResponse>();

        Assert.That(page, Is.Not.Default);
        Assert.Multiple(() =>
        {
            Assert.That(page.Torrents, Has.Length.EqualTo(1));
            Assert.That(page.NextPageAddress, Is.EqualTo(null));
        });

        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenNonExistingFilterValues_ReturnsEmptyTorrentPage()
    {
        const string query = TestData.Endpoints.Torrents +
            "?take=5" +
            "&afterId=100" +
            "&nameStartsWith=NoSuchName" +
            "&webPageUri=https%3A%2F%2FtorrentTracker.com%2Fi-do-not-exist" +
            "&hashString=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" +
            "&cronExists=True";

        var response = await _client.GetAsync(query);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<FindTorrentPageResponse>();

        Assert.That(page, Is.Not.Default);
        Assert.Multiple(() =>
        {
            Assert.That(page.Torrents, Has.Length.EqualTo(0));
            Assert.That(page.NextPageAddress, Is.EqualTo(null));
        });
    }
}
