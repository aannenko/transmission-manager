using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Models;
using Parameters = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageParameters;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class GetTorrentPageTests
{
    private static readonly Torrent[] _torrents = TestData.Database.CreateInitialTorrents();

    private TestWebApplicationFactory<Program> _factory = default!;
    private HttpClient _client = default!;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory<Program>(_torrents, null, null);
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async ValueTask TearDown()
    {
        _client?.Dispose();
        await _factory.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenAnchorIdAndTakePointToExistingPage_ReturnsMatchingTorrentPage()
    {
        var parameters = new Parameters(AnchorId: 1, Take: 3);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        var expectedNextPage = EndpointAddresses.Torrents + "?take=3&anchorId=3";
        var expectedPreviousPage = EndpointAddresses.Torrents + "?take=3&anchorId=2&direction=Backward";

        AssertTorrentPage(page, _torrents[1..], expectedNextPage, expectedPreviousPage);

        response = await _client.GetAsync(expectedNextPage).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenPropertyStartsWithAndCronExistsPointToExistingNameAndCron_ReturnsMatchingTorrents()
    {
        var parameters = new Parameters(Take: 2, PropertyStartsWith: "TV Show", CronExists: true);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        var expectedNextPage = EndpointAddresses.Torrents +
            "?take=2&anchorId=3&propertyStartsWith=TV+Show&cronExists=True";

        var expectedPreviousPage = EndpointAddresses.Torrents +
            "?take=2&anchorId=1&direction=Backward&propertyStartsWith=TV+Show&cronExists=True";

        AssertTorrentPage(page, [_torrents[0], _torrents[2]], expectedNextPage, expectedPreviousPage);

        response = await _client.GetAsync(expectedNextPage).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenPropertyStartsWithPointsToExistingWebPageUri_ReturnsMatchingTorrent()
    {
        var parameters = new Parameters(
            Take: 1,
            PropertyStartsWith: TestData.Database.SecondTorrentWebPageAddress);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        var expectedNextPage = $"{EndpointAddresses.Torrents}?take=1&anchorId=2" +
            "&propertyStartsWith=https%3A%2F%2FtorrentTracker.com%2Fforum%2Fviewtopic.php%3Ft%3D1234568";

        var expectedPreviousPage = $"{EndpointAddresses.Torrents}?take=1&anchorId=2&direction=Backward" +
            "&propertyStartsWith=https%3A%2F%2FtorrentTracker.com%2Fforum%2Fviewtopic.php%3Ft%3D1234568";

        AssertTorrentPage(page, _torrents[1..2], expectedNextPage, expectedPreviousPage);

        response = await _client.GetAsync(expectedNextPage).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenPropertyStartsWithPointsToExistingHashString_ReturnsMatchingTorrent()
    {
        var parameters = new Parameters(
            Take: 1,
            PropertyStartsWith: TestData.Database.SecondTorrentHashString);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        var expectedNextPage = $"{EndpointAddresses.Torrents}?take=1&anchorId=2" +
            $"&propertyStartsWith={TestData.Database.SecondTorrentHashString}";

        var expectedPreviousPage = $"{EndpointAddresses.Torrents}?take=1&anchorId=2&direction=Backward" +
            $"&propertyStartsWith={TestData.Database.SecondTorrentHashString}";

        AssertTorrentPage(page, _torrents[1..2], expectedNextPage, expectedPreviousPage);

        response = await _client.GetAsync(expectedNextPage).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenAnchorIdIsTooLarge_ReturnsEmptyTorrentPage()
    {
        var parameters = new Parameters(AnchorId: long.MaxValue, Take: 5);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenPropertyStartsWithValueDoesNotPointToAnyTorrent_ReturnsEmptyTorrentPage()
    {
        var parameters = new Parameters(PropertyStartsWith: "NoSuchTextAnywhere");

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenOrderByIsNameDescAndTakeIsTwo_ReturnsCorrectPagesAndNextPageLinks()
    {
        var parameters = new Parameters(
            OrderBy: GetTorrentPageOrder.NameDesc,
            Take: 2);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        var expectedNextPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=2&anchorValue=TV+Show+2";

        var expectedPreviousPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=3&anchorValue=TV+Show+3&direction=Backward";

        AssertTorrentPage(page, [_torrents[2], _torrents[1]], expectedNextPage1, expectedPreviousPage1);

        response = await _client.GetAsync(expectedNextPage1).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        var expectedNextPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=1&anchorValue=TV+Show+1";

        var expectedPreviousPage2 = expectedNextPage2 + "&direction=Backward";

        AssertTorrentPage(page, _torrents[0..1], expectedNextPage2, expectedPreviousPage2);

        response = await _client.GetAsync(expectedNextPage2).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenOrderByIsNameDescAndDirectionIsBackwardAndTakeIsTwo_ReturnsCorrectPagesAndPreviousPageLinks()
    {
        var parameters = new Parameters(
            OrderBy: GetTorrentPageOrder.NameDesc,
            Direction: GetTorrentPageDirection.Backward,
            Take: 2);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        var expectedNextPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=1&anchorValue=TV+Show+1";

        var expectedPreviousPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=2&anchorValue=TV+Show+2&direction=Backward";

        AssertTorrentPage(page, [_torrents[1], _torrents[0]], expectedNextPage1, expectedPreviousPage1);

        response = await _client.GetAsync(expectedPreviousPage1).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        var expectedNextPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=3&anchorValue=TV+Show+3";

        var expectedPreviousPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=3&anchorValue=TV+Show+3&direction=Backward";

        AssertTorrentPage(page, [_torrents[2]], expectedNextPage2, expectedPreviousPage2);

        response = await _client.GetAsync(expectedPreviousPage2).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenOrderByIsRefreshDateAndTakeIsTwo_ReturnsCorrectPagesAndPreviousPageLinks()
    {
        var parameters = new Parameters(
            OrderBy: GetTorrentPageOrder.RefreshDate,
            Take: 2);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        var expectedNextPageTime1 = ToExpectedDateTimeAnchorString(_torrents[1].RefreshDate);
        var expectedNextPage1 = EndpointAddresses.Torrents +
            $"?take=2&orderBy=RefreshDate&anchorId=2&anchorValue={expectedNextPageTime1}";

        var expectedPrevPageTime1 = ToExpectedDateTimeAnchorString(_torrents[2].RefreshDate);
        var expectedPreviousPage1 = EndpointAddresses.Torrents +
            $"?take=2&orderBy=RefreshDate&anchorId=3&anchorValue={expectedPrevPageTime1}&direction=Backward";

        AssertTorrentPage(page, [_torrents[2], _torrents[1]], expectedNextPage1, expectedPreviousPage1);

        response = await _client.GetAsync(expectedNextPage1).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        var expectedTime2 = ToExpectedDateTimeAnchorString(_torrents[0].RefreshDate);

        var expectedNextPage2 = EndpointAddresses.Torrents +
            $"?take=2&orderBy=RefreshDate&anchorId=1&anchorValue={expectedTime2}";

        var expectedPreviousPage2 = EndpointAddresses.Torrents +
            $"?take=2&orderBy=RefreshDate&anchorId=1&anchorValue={expectedTime2}&direction=Backward";

        AssertTorrentPage(page, [_torrents[0]], expectedNextPage2, expectedPreviousPage2);

        response = await _client.GetAsync(expectedNextPage2).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenOrderByIsRefreshDateDescAndDirectionIsBackwardAndTakeIsTwo_ReturnsCorrectPagesAndPreviousPageLinks()
    {
        var parameters = new Parameters(
            OrderBy: GetTorrentPageOrder.RefreshDateDesc,
            Direction: GetTorrentPageDirection.Backward,
            Take: 2);

        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        var expectedNextPageTime1 = ToExpectedDateTimeAnchorString(_torrents[2].RefreshDate);
        var expectedNextPage1 = EndpointAddresses.Torrents +
            $"?take=2&orderBy=RefreshDateDesc&anchorId=3&anchorValue={expectedNextPageTime1}";

        var expectedPrevPageTime1 = ToExpectedDateTimeAnchorString(_torrents[1].RefreshDate);
        var expectedPreviousPage1 = EndpointAddresses.Torrents +
            $"?take=2&orderBy=RefreshDateDesc&anchorId=2&anchorValue={expectedPrevPageTime1}&direction=Backward";

        AssertTorrentPage(page, [_torrents[1], _torrents[2]], expectedNextPage1, expectedPreviousPage1);

        response = await _client.GetAsync(expectedPreviousPage1).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        var expectedTime2 = ToExpectedDateTimeAnchorString(_torrents[0].RefreshDate);

        var expectedNextPage2 = EndpointAddresses.Torrents +
            $"?take=2&orderBy=RefreshDateDesc&anchorId=1&anchorValue={expectedTime2}";

        var expectedPreviousPage2 = EndpointAddresses.Torrents +
            $"?take=2&orderBy=RefreshDateDesc&anchorId=1&anchorValue={expectedTime2}&direction=Backward";

        AssertTorrentPage(page, [_torrents[0]], expectedNextPage2, expectedPreviousPage2);

        response = await _client.GetAsync(expectedPreviousPage2).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [TestCaseSource(nameof(GetGetTorrentPageAsyncBadRequestTestCases))]
    public async Task GetTorrentPageAsync_WhenSpecificParametersAreUsed_ReturnsExpectedValidationProblem(
        Parameters parameters,
        string problematicParameterName,
        string expectedErrorMessage)
    {
        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Errors, Has.Count.EqualTo(1));
            Assert.That(problem.Errors, Contains.Key(problematicParameterName));
            if (problem.Errors.TryGetValue(problematicParameterName, out var errors))
            {
                Assert.That(errors, Has.Length.EqualTo(1));
                Assert.That(errors[0], Is.EqualTo(expectedErrorMessage));
            }
        }
    }

    private static IEnumerable<TestCaseData<Parameters, string, string>>
        GetGetTorrentPageAsyncBadRequestTestCases()
    {
        yield return new(
            new(Take: 0),
            "take",
            $"The field take must be between 1 and {Parameters.MaxTake}.")
        {
            TestName = "GetTorrentPageAsync_WhenTakeIsZero_ReturnsValidationProblem"
        };

        yield return new(
            new(Take: 1001),
            "take",
            $"The field take must be between 1 and {Parameters.MaxTake}.")
        {
            TestName = "GetTorrentPageAsync_WhenTakeIsTooLarge_ReturnsValidationProblem"
        };

        yield return new(
            new(Take: -1),
            "take",
            $"The field take must be between 1 and {Parameters.MaxTake}.")
        {
            TestName = "GetTorrentPageAsync_WhenTakeIsNegative_ReturnsValidationProblem"
        };

        yield return new(
            new(OrderBy: (GetTorrentPageOrder)999),
            "orderBy",
            "The field orderBy is invalid.")
        {
            TestName = "GetTorrentPageAsync_WhenOrderByIsInvalid_ReturnsValidationProblem"
        };

        yield return new(
            new(Direction: (GetTorrentPageDirection)999),
            "direction",
            "The field direction is invalid.")
        {
            TestName = "GetTorrentPageAsync_WhenDirectionIsInvalid_ReturnsValidationProblem"
        };

        yield return new(
            new(OrderBy: GetTorrentPageOrder.Id, AnchorValue: "abc"),
            "anchorValue",
            "When orderBy is 'Id', anchorValue must be 'null'.")
        {
            TestName = "GetTorrentPageAsync_WhenOrderByIsIdAndAnchorValueIsNotNull_ReturnsValidationProblem"
        };

        yield return new(
            new(OrderBy: GetTorrentPageOrder.IdDesc, AnchorValue: "abc"),
            "anchorValue",
            "When orderBy is 'IdDesc', anchorValue must be 'null'.")
        {
            TestName = "GetTorrentPageAsync_WhenOrderByIsIdDescAndAnchorValueIsNotNull_ReturnsValidationProblem"
        };

        yield return new(
            new(OrderBy: GetTorrentPageOrder.RefreshDate, AnchorValue: "abc"),
            "anchorValue",
            $"When orderBy is 'RefreshDate', anchorValue must match format '{Parameters.DateFormat}'.")
        {
            TestName = "GetTorrentPageAsync_WhenOrderByIsRefreshDateAndAnchorValueIsInvalid_ReturnsValidationProblem"
        };

        yield return new(
            new(OrderBy: GetTorrentPageOrder.RefreshDateDesc, AnchorValue: "abc"),
            "anchorValue",
            $"When orderBy is 'RefreshDateDesc', anchorValue must match format '{Parameters.DateFormat}'.")
        {
            TestName = "GetTorrentPageAsync_WhenOrderByIsRefreshDateDescAndAnchorValueIsInvalid_ReturnsValidationProblem"
        };
    }

    private static void AssertTorrentPage(
        GetTorrentPageResponse? page,
        Torrent[] expectedTorrents,
        string? expectedNextPage,
        string? expectedPreviousPage)
    {
        Assert.That(page, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Torrents, Has.Count.EqualTo(expectedTorrents.Length));
            Assert.That(page.NextPageAddress, Is.EqualTo(expectedNextPage));
            Assert.That(page.PreviousPageAddress, Is.EqualTo(expectedPreviousPage));
        }

        for (var i = 0; i < expectedTorrents.Length; i++)
            TorrentAssertions.AssertEqual(page.Torrents[i], expectedTorrents[i]);
    }

    private static string ToExpectedDateTimeAnchorString(DateTime dateTime) =>
        dateTime.ToUniversalTime().ToString(Parameters.DateFormat, CultureInfo.InvariantCulture);
}
