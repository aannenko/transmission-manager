using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Actions.FindTorrentPage;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests;

[Parallelizable(ParallelScope.Self)]
internal sealed class FindTorrentPageTests
{
    private static readonly Torrent[] _torrents = TestData.Database.CreateInitialTorrents();

    private TestWebAppliationFactory<Program> _factory = default!;
    private HttpClient _client = default!;

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

        var page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(2, page, parameters);
        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 3, _torrents[2]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenCorrectNameAndCronFilters_ReturnsMatchingTorrents()
    {
        var parameters = new FindTorrentPageParameters(NameStartsWith: "TV Show", CronExists: true);

        var page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(2, page, parameters);
        TorrentAssertions.AssertEqual(page.Torrents[0], 1, _torrents[0]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 3, _torrents[2]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenCorrectUriAndHashStringFilters_ReturnsMatchingTorrent()
    {
        var parameters = new FindTorrentPageParameters(
            WebPageUri: TestData.Database.SecondTorrentWebPageUri,
            HashString: TestData.Database.SecondTorrentHashString);

        var page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(1, page, parameters);
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

        var page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(0, page, parameters);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenInvalidTakeParameter_ReturnsValidationProblem()
    {
        var parameters = new FindTorrentPageParameters(Take: 0);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem!.Errors, Has.Count.EqualTo(1));
        Assert.That(problem!.Errors, Contains.Key(nameof(FindTorrentPageParameters.Take)));
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenTooLargeValueOfTakeParameter_ReturnsValidationProblem()
    {
        var parameters = new FindTorrentPageParameters(Take: 1001);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem!.Errors, Has.Count.EqualTo(1));
        Assert.That(problem!.Errors, Contains.Key(nameof(FindTorrentPageParameters.Take)));
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenInvalidHashStringParameter_ReturnsValidationProblem()
    {
        var parameters = new FindTorrentPageParameters(HashString: "invalid");

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem!.Errors, Has.Count.EqualTo(1));
        Assert.That(problem!.Errors, Contains.Key(nameof(FindTorrentPageParameters.HashString)));
    }

    private static void AssertTorrentPage(
        int expectedCount,
        FindTorrentPageResponse page,
        FindTorrentPageParameters parameters)
    {
        Assert.That(page, Is.Not.Default);
        Assert.Multiple(() =>
        {
            Assert.That(page.Torrents, Has.Count.EqualTo(expectedCount));
            var nextPage = parameters.ToNextPageParameters(page.Torrents)?.ToPathAndQueryString();
            Assert.That(page.NextPageAddress, Is.EqualTo(nextPage));
        });
    }
}
