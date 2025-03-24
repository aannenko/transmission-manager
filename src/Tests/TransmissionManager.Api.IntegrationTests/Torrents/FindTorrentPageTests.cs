using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Actions.Torrents.FindPage;
using TransmissionManager.Api.Constants;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

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
    public async Task FindTorrentPageAsync_WhenAnchorIdAndTakePointToExistingPage_ReturnsMatchingTorrentPage()
    {
        var parameters = new FindTorrentPageParameters(AnchorId: 1, Take: 3);

        var page = await _client
            .GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        const string expectedNextPage = EndpointAddresses.TorrentsApi + "?take=3&anchorId=3";
        const string expectedPreviousPage = EndpointAddresses.TorrentsApi + "?take=3&anchorId=2&direction=Backward";

        AssertTorrentPage(page, 2, expectedNextPage, expectedPreviousPage);
        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 3, _torrents[2]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedNextPage).ConfigureAwait(false);

        AssertTorrentPage(page, 0, null, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenPropertyStartsWithAndCronExistsPointToExistingNameAndCron_ReturnsMatchingTorrents()
    {
        var parameters = new FindTorrentPageParameters(Take: 2, PropertyStartsWith: "TV Show", CronExists: true);

        var page = await _client
            .GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        const string expectedNextPage = EndpointAddresses.TorrentsApi +
            "?take=2&anchorId=3&propertyStartsWith=TV+Show&cronExists=True";

        const string expectedPreviousPage = EndpointAddresses.TorrentsApi +
            "?take=2&anchorId=1&direction=Backward&propertyStartsWith=TV+Show&cronExists=True";

        AssertTorrentPage(page, 2, expectedNextPage, expectedPreviousPage);
        TorrentAssertions.AssertEqual(page.Torrents[0], 1, _torrents[0]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 3, _torrents[2]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedNextPage).ConfigureAwait(false);

        AssertTorrentPage(page, 0, null, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenPropertyStartsWithPointsToExistingWebPageUri_ReturnsMatchingTorrent()
    {
        var parameters = new FindTorrentPageParameters(
            Take: 1,
            PropertyStartsWith: TestData.Database.SecondTorrentWebPageAddress);

        var page = await _client
            .GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        const string expectedNextPage = $"{EndpointAddresses.TorrentsApi}?take=1&anchorId=2" +
            "&propertyStartsWith=https%3A%2F%2FtorrentTracker.com%2Fforum%2Fviewtopic.php%3Ft%3D1234568";

        const string expectedPreviousPage = $"{EndpointAddresses.TorrentsApi}?take=1&anchorId=2&direction=Backward" +
            "&propertyStartsWith=https%3A%2F%2FtorrentTracker.com%2Fforum%2Fviewtopic.php%3Ft%3D1234568";

        AssertTorrentPage(page, 1, expectedNextPage, expectedPreviousPage);
        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedNextPage).ConfigureAwait(false);

        AssertTorrentPage(page, 0, null, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenPropertyStartsWithPointsToExistingHashString_ReturnsMatchingTorrent()
    {
        var parameters = new FindTorrentPageParameters(
            Take: 1,
            PropertyStartsWith: TestData.Database.SecondTorrentHashString);

        var page = await _client
            .GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        const string expectedNextPage = $"{EndpointAddresses.TorrentsApi}?take=1&anchorId=2" +
            $"&propertyStartsWith={TestData.Database.SecondTorrentHashString}";

        const string expectedPreviousPage = $"{EndpointAddresses.TorrentsApi}?take=1&anchorId=2&direction=Backward" +
            $"&propertyStartsWith={TestData.Database.SecondTorrentHashString}";

        AssertTorrentPage(page, 1, expectedNextPage, expectedPreviousPage);
        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedNextPage).ConfigureAwait(false);

        AssertTorrentPage(page, 0, null, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenAnchorIdIsTooLarge_ReturnsEmptyTorrentPage()
    {
        var parameters = new FindTorrentPageParameters(AnchorId: long.MaxValue, Take: 5);

        var page = await _client
            .GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(page, 0, null, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenPropertyStartsWithValueDoesNotPointToAnyTorrent_ReturnsEmptyTorrentPage()
    {
        var parameters = new FindTorrentPageParameters(PropertyStartsWith: "NoSuchTextAnywhere");

        var page = await _client
            .GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(page, 0, null, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenOrderByIsNameDescAndTakeIsTwo_ReturnsCorrectPagesAndNextPageLinks()
    {
        var parameters = new FindTorrentPageParameters(
            OrderBy: TorrentOrder.NameDesc,
            Take: 2);

        var page = await _client
            .GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        const string expectedNextPage1 = EndpointAddresses.TorrentsApi +
            "?take=2&orderBy=NameDesc&anchorId=2&anchorValue=TV+Show+2";

        const string expectedPreviousPage1 = EndpointAddresses.TorrentsApi +
            "?take=2&orderBy=NameDesc&anchorId=3&anchorValue=TV+Show+3&direction=Backward";

        AssertTorrentPage(page, 2, expectedNextPage1, expectedPreviousPage1);
        TorrentAssertions.AssertEqual(page.Torrents[0], 3, _torrents[2]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 2, _torrents[1]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedNextPage1).ConfigureAwait(false);

        const string expectedNextPage2 = EndpointAddresses.TorrentsApi +
            "?take=2&orderBy=NameDesc&anchorId=1&anchorValue=TV+Show+1";

        const string expectedPreviousPage2 = expectedNextPage2 + "&direction=Backward";

        AssertTorrentPage(page, 1, expectedNextPage2, expectedPreviousPage2);
        TorrentAssertions.AssertEqual(page.Torrents[0], 1, _torrents[0]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedNextPage2).ConfigureAwait(false);

        AssertTorrentPage(page, 0, null, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenOrderByIsNameDescAndDirectionIsBackwardAndTakeIsTwo_ReturnsCorrectPagesAndPreviousPageLinks()
    {
        var parameters = new FindTorrentPageParameters(
            OrderBy: TorrentOrder.NameDesc,
            Direction: FindTorrentPageDirection.Backward,
            Take: 2);

        var page = await _client
            .GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        const string expectedNextPage1 = EndpointAddresses.TorrentsApi +
            "?take=2&orderBy=NameDesc&anchorId=1&anchorValue=TV+Show+1";

        const string expectedPreviousPage1 = EndpointAddresses.TorrentsApi +
            "?take=2&orderBy=NameDesc&anchorId=2&anchorValue=TV+Show+2&direction=Backward";

        AssertTorrentPage(page, 2, expectedNextPage1, expectedPreviousPage1);
        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 1, _torrents[0]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedPreviousPage1).ConfigureAwait(false);

        const string expectedNextPage2 = EndpointAddresses.TorrentsApi +
            "?take=2&orderBy=NameDesc&anchorId=3&anchorValue=TV+Show+3";

        const string expectedPreviousPage2 = EndpointAddresses.TorrentsApi +
            "?take=2&orderBy=NameDesc&anchorId=3&anchorValue=TV+Show+3&direction=Backward";

        AssertTorrentPage(page, 1, expectedNextPage2, expectedPreviousPage2);
        TorrentAssertions.AssertEqual(page.Torrents[0], 3, _torrents[2]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedPreviousPage2).ConfigureAwait(false);

        AssertTorrentPage(page, 0, null, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenTakeIsZero_ReturnsValidationProblem()
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
    public async Task FindTorrentPageAsync_WhenTakeIsTooLarge_ReturnsValidationProblem()
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
    public async Task FindTorrentPageAsync_WhenOrderByIsInvalid_ReturnsValidationProblem()
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
        string? expectedNextPage,
        string? expectedPreviousPage)
    {
        Assert.That(page, Is.Not.Default);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Torrents, Has.Length.EqualTo(expectedCount));
            Assert.That(page.NextPageAddress, Is.EqualTo(expectedNextPage));
            Assert.That(page.PreviousPageAddress, Is.EqualTo(expectedPreviousPage));
        }
    }
}
