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
        var parameters = new FindTorrentPageParameters(Take: 5, AfterId: 1);
        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<FindTorrentPageResponse>().ConfigureAwait(false);

        Assert.That(page, Is.Not.Default);
        Assert.Multiple(() =>
        {
            Assert.That(page.Torrents, Has.Count.EqualTo(2));
            var nextPage = parameters.ToNextPageParameters(page.Torrents)?.ToPathAndQueryString();
            Assert.That(page.NextPageAddress, Is.EqualTo(nextPage));
        });

        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 3, _torrents[2]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenCorrectNameAndCronFilters_ReturnsMatchingTorrents()
    {
        var parameters = new FindTorrentPageParameters(NameStartsWith: "TV Show", CronExists: true);
        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<FindTorrentPageResponse>().ConfigureAwait(false);

        Assert.That(page, Is.Not.Default);
        Assert.Multiple(() =>
        {
            Assert.That(page.Torrents, Has.Count.EqualTo(2));
            var nextPage = parameters.ToNextPageParameters(page.Torrents)?.ToPathAndQueryString();
            Assert.That(page.NextPageAddress, Is.EqualTo(nextPage));
        });

        TorrentAssertions.AssertEqual(page.Torrents[0], 1, _torrents[0]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 3, _torrents[2]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenCorrectUriAndHashStringFilters_ReturnsMatchingTorrent()
    {
        var parameters = new FindTorrentPageParameters(
            WebPageUri: new(TestData.Database.SecondTorrentWebPageUri),
            HashString: TestData.Database.SecondTorrentHashString);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<FindTorrentPageResponse>().ConfigureAwait(false);

        Assert.That(page, Is.Not.Default);
        Assert.Multiple(() =>
        {
            Assert.That(page.Torrents, Has.Count.EqualTo(1));
            var nextPage = parameters.ToNextPageParameters(page.Torrents)?.ToPathAndQueryString();
            Assert.That(page.NextPageAddress, Is.EqualTo(nextPage));
        });

        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenNonExistingFilterValues_ReturnsEmptyTorrentPage()
    {
        var parameters = new FindTorrentPageParameters(
            Take: 5,
            AfterId: 100,
            NameStartsWith: "NoSuchName",
            WebPageUri: new("https://torrentTracker.com/i-do-not-exist"),
            HashString: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            CronExists: true);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<FindTorrentPageResponse>().ConfigureAwait(false);

        Assert.That(page, Is.Not.Default);
        Assert.Multiple(() =>
        {
            Assert.That(page.Torrents, Has.Count.EqualTo(0));
            var nextPage = parameters.ToNextPageParameters(page.Torrents)?.ToPathAndQueryString();
            Assert.That(page.NextPageAddress, Is.EqualTo(nextPage));
        });
    }
}
