using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Actions.FindTorrentPage;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Dto;
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
        var parameters = new FindTorrentPageParameters(Take: 2, AfterId: 1);
        const string expectedNextPage = EndpointAddresses.TorrentsApi + "?take=2&afterId=3";

        var page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(page, 2, expectedNextPage);
        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 3, _torrents[2]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenParameterTakeLargerThanReturnedPage_ReturnsNullNextPageAddress()
    {
        var parameters = new FindTorrentPageParameters(Take: 5, AfterId: 1);

        var page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(page, 2, null);
        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 3, _torrents[2]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenCorrectNameAndCronFilters_ReturnsMatchingTorrents()
    {
        var parameters = new FindTorrentPageParameters(Take: 2, PropertyStartsWith: "TV Show", CronExists: true);
        const string expectedNextPage = EndpointAddresses.TorrentsApi +
            "?take=2&afterId=3&propertyStartsWith=TV+Show&cronExists=True";

        var page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(page, 2, expectedNextPage);
        TorrentAssertions.AssertEqual(page.Torrents[0], 1, _torrents[0]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 3, _torrents[2]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenCorrectUriFilter_ReturnsMatchingTorrent()
    {
        var parameters = new FindTorrentPageParameters(
            Take: 1,
            PropertyStartsWith: TestData.Database.SecondTorrentWebPageAddress);

        const string expectedNextPage = EndpointAddresses.TorrentsApi +
            "?take=1&afterId=2" +
            "&propertyStartsWith=https%3A%2F%2FtorrentTracker.com%2Fforum%2Fviewtopic.php%3Ft%3D1234568";

        var page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(page, 1, expectedNextPage);
        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenCorrectHashStringFilter_ReturnsMatchingTorrent()
    {
        var parameters = new FindTorrentPageParameters(
            Take: 1,
            PropertyStartsWith: TestData.Database.SecondTorrentHashString);

        const string expectedNextPage = EndpointAddresses.TorrentsApi +
            "?take=1&afterId=2" +
            $"&propertyStartsWith={TestData.Database.SecondTorrentHashString}";

        var page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(page, 1, expectedNextPage);
        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenNonExistentPaginationValues_ReturnsEmptyTorrentPage()
    {
        var parameters = new FindTorrentPageParameters(Take: 5, AfterId: 100);

        var page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(page, 0, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenNonExistentFilterValues_ReturnsEmptyTorrentPage()
    {
        var parameters = new FindTorrentPageParameters(PropertyStartsWith: "NoSuchTextAnywhere");

        var page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(page, 0, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenGivenCorrectNamePaginationParameters_ReturnsMatchingTorrents()
    {
        var parameters = new FindTorrentPageParameters(
            OrderBy: TorrentOrder.NameDesc,
            Take: 2);

        const string expectedNextPage = EndpointAddresses.TorrentsApi +
            "?take=2&afterId=2&after=TV+Show+2&orderBy=NameDesc";

        var page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(page, 2, expectedNextPage);
        TorrentAssertions.AssertEqual(page.Torrents[0], 3, _torrents[2]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 2, _torrents[1]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedNextPage).ConfigureAwait(false);

        AssertTorrentPage(page, 1, null);
        TorrentAssertions.AssertEqual(page.Torrents[0], 1, _torrents[0]);
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
    public async Task FindTorrentPageAsync_WhenGivenInvalidOrderByParameter_ReturnsValidationProblem()
    {
        var parameters = new FindTorrentPageParameters(OrderBy: (TorrentOrder)999);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem!.Errors, Has.Count.EqualTo(1));
        Assert.That(problem!.Errors, Contains.Key(nameof(FindTorrentPageParameters.OrderBy)));
    }

    private static void AssertTorrentPage(
        FindTorrentPageResponse page,
        int expectedCount,
        string? expectedNextPage)
    {
        Assert.That(page, Is.Not.Default);
        Assert.Multiple(() =>
        {
            Assert.That(page.Torrents, Has.Count.EqualTo(expectedCount));
            Assert.That(page.NextPageAddress, Is.EqualTo(expectedNextPage));
        });
    }
}
