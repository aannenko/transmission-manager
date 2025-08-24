using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.IntegrationTests.Helpers;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.IntegrationTests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class GetTorrentPageTests
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
    public async Task GetTorrentPageAsync_WhenAnchorIdAndTakePointToExistingPage_ReturnsMatchingTorrentPage()
    {
        var parameters = new GetTorrentPageParameters(AnchorId: 1, Take: 3);

        var page = await _client
            .GetFromJsonAsync<GetTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        var expectedNextPage = EndpointAddresses.Torrents + "?take=3&anchorId=3";
        var expectedPreviousPage = EndpointAddresses.Torrents + "?take=3&anchorId=2&direction=Backward";

        AssertTorrentPage(page, _torrents[1..], expectedNextPage, expectedPreviousPage);

        page = await _client.GetFromJsonAsync<GetTorrentPageResponse>(expectedNextPage).ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenPropertyStartsWithAndCronExistsPointToExistingNameAndCron_ReturnsMatchingTorrents()
    {
        var parameters = new GetTorrentPageParameters(Take: 2, PropertyStartsWith: "TV Show", CronExists: true);

        var page = await _client
            .GetFromJsonAsync<GetTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        var expectedNextPage = EndpointAddresses.Torrents +
            "?take=2&anchorId=3&propertyStartsWith=TV+Show&cronExists=True";

        var expectedPreviousPage = EndpointAddresses.Torrents +
            "?take=2&anchorId=1&direction=Backward&propertyStartsWith=TV+Show&cronExists=True";

        AssertTorrentPage(page, [_torrents[0], _torrents[2]], expectedNextPage, expectedPreviousPage);

        page = await _client.GetFromJsonAsync<GetTorrentPageResponse>(expectedNextPage).ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenPropertyStartsWithPointsToExistingWebPageUri_ReturnsMatchingTorrent()
    {
        var parameters = new GetTorrentPageParameters(
            Take: 1,
            PropertyStartsWith: TestData.Database.SecondTorrentWebPageAddress);

        var page = await _client
            .GetFromJsonAsync<GetTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        var expectedNextPage = $"{EndpointAddresses.Torrents}?take=1&anchorId=2" +
            "&propertyStartsWith=https%3A%2F%2FtorrentTracker.com%2Fforum%2Fviewtopic.php%3Ft%3D1234568";

        var expectedPreviousPage = $"{EndpointAddresses.Torrents}?take=1&anchorId=2&direction=Backward" +
            "&propertyStartsWith=https%3A%2F%2FtorrentTracker.com%2Fforum%2Fviewtopic.php%3Ft%3D1234568";

        AssertTorrentPage(page, _torrents[1..2], expectedNextPage, expectedPreviousPage);

        page = await _client.GetFromJsonAsync<GetTorrentPageResponse>(expectedNextPage).ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenPropertyStartsWithPointsToExistingHashString_ReturnsMatchingTorrent()
    {
        var parameters = new GetTorrentPageParameters(
            Take: 1,
            PropertyStartsWith: TestData.Database.SecondTorrentHashString);

        var page = await _client
            .GetFromJsonAsync<GetTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        var expectedNextPage = $"{EndpointAddresses.Torrents}?take=1&anchorId=2" +
            $"&propertyStartsWith={TestData.Database.SecondTorrentHashString}";

        var expectedPreviousPage = $"{EndpointAddresses.Torrents}?take=1&anchorId=2&direction=Backward" +
            $"&propertyStartsWith={TestData.Database.SecondTorrentHashString}";

        AssertTorrentPage(page, _torrents[1..2], expectedNextPage, expectedPreviousPage);

        page = await _client.GetFromJsonAsync<GetTorrentPageResponse>(expectedNextPage).ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenAnchorIdIsTooLarge_ReturnsEmptyTorrentPage()
    {
        var parameters = new GetTorrentPageParameters(AnchorId: long.MaxValue, Take: 5);

        var page = await _client
            .GetFromJsonAsync<GetTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenPropertyStartsWithValueDoesNotPointToAnyTorrent_ReturnsEmptyTorrentPage()
    {
        var parameters = new GetTorrentPageParameters(PropertyStartsWith: "NoSuchTextAnywhere");

        var page = await _client
            .GetFromJsonAsync<GetTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenOrderByIsNameDescAndTakeIsTwo_ReturnsCorrectPagesAndNextPageLinks()
    {
        var parameters = new GetTorrentPageParameters(
            OrderBy: GetTorrentPageOrder.NameDesc,
            Take: 2);

        var page = await _client
            .GetFromJsonAsync<GetTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        var expectedNextPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=2&anchorValue=TV+Show+2";

        var expectedPreviousPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=3&anchorValue=TV+Show+3&direction=Backward";

        AssertTorrentPage(page, [_torrents[2], _torrents[1]], expectedNextPage1, expectedPreviousPage1);

        page = await _client.GetFromJsonAsync<GetTorrentPageResponse>(expectedNextPage1).ConfigureAwait(false);

        var expectedNextPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=1&anchorValue=TV+Show+1";

        var expectedPreviousPage2 = expectedNextPage2 + "&direction=Backward";

        AssertTorrentPage(page, _torrents[0..1], expectedNextPage2, expectedPreviousPage2);

        page = await _client.GetFromJsonAsync<GetTorrentPageResponse>(expectedNextPage2).ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenOrderByIsNameDescAndDirectionIsBackwardAndTakeIsTwo_ReturnsCorrectPagesAndPreviousPageLinks()
    {
        var parameters = new GetTorrentPageParameters(
            OrderBy: GetTorrentPageOrder.NameDesc,
            Direction: GetTorrentPageDirection.Backward,
            Take: 2);

        var page = await _client
            .GetFromJsonAsync<GetTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        var expectedNextPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=1&anchorValue=TV+Show+1";

        var expectedPreviousPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=2&anchorValue=TV+Show+2&direction=Backward";

        AssertTorrentPage(page, [_torrents[1], _torrents[0]], expectedNextPage1, expectedPreviousPage1);

        page = await _client.GetFromJsonAsync<GetTorrentPageResponse>(expectedPreviousPage1).ConfigureAwait(false);

        var expectedNextPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=3&anchorValue=TV+Show+3";

        var expectedPreviousPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=3&anchorValue=TV+Show+3&direction=Backward";

        AssertTorrentPage(page, [_torrents[2]], expectedNextPage2, expectedPreviousPage2);

        page = await _client.GetFromJsonAsync<GetTorrentPageResponse>(expectedPreviousPage2).ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenOrderByIsRefreshDateAndDirectionIsBackwardAndTakeIsTwo_ReturnsCorrectPagesAndPreviousPageLinks()
    {
        var parameters = new GetTorrentPageParameters(
            OrderBy: GetTorrentPageOrder.RefreshDate,
            Take: 2);

        var page = await _client
            .GetFromJsonAsync<GetTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        var expectedNextPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDate&anchorId=2&anchorValue=2023-11-02T11%3A22%3A33.4440000Z";

        var expectedPreviousPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDate&anchorId=3&anchorValue=2022-10-01T12%3A34%3A56.7770000Z&direction=Backward";

        AssertTorrentPage(page, [_torrents[2], _torrents[1]], expectedNextPage1, expectedPreviousPage1);

        page = await _client.GetFromJsonAsync<GetTorrentPageResponse>(expectedNextPage1).ConfigureAwait(false);

        var expectedNextPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDate&anchorId=1&anchorValue=2024-12-03T10%3A20%3A30.4000000Z";

        var expectedPreviousPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDate&anchorId=1&anchorValue=2024-12-03T10%3A20%3A30.4000000Z&direction=Backward";

        AssertTorrentPage(page, [_torrents[0]], expectedNextPage2, expectedPreviousPage2);

        page = await _client.GetFromJsonAsync<GetTorrentPageResponse>(expectedNextPage2).ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [Test]
    public async Task GetTorrentPageAsync_WhenOrderByIsRefreshDateDescAndDirectionIsBackwardAndTakeIsTwo_ReturnsCorrectPagesAndPreviousPageLinks()
    {
        var parameters = new GetTorrentPageParameters(
            OrderBy: GetTorrentPageOrder.RefreshDateDesc,
            Direction: GetTorrentPageDirection.Backward,
            Take: 2);

        var page = await _client
            .GetFromJsonAsync<GetTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        var expectedNextPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDateDesc&anchorId=3&anchorValue=2022-10-01T12%3A34%3A56.7770000Z";

        var expectedPreviousPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDateDesc&anchorId=2&anchorValue=2023-11-02T11%3A22%3A33.4440000Z&direction=Backward";

        AssertTorrentPage(page, [_torrents[1], _torrents[2]], expectedNextPage1, expectedPreviousPage1);

        page = await _client.GetFromJsonAsync<GetTorrentPageResponse>(expectedPreviousPage1).ConfigureAwait(false);

        var expectedNextPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDateDesc&anchorId=1&anchorValue=2024-12-03T10%3A20%3A30.4000000Z";

        var expectedPreviousPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDateDesc&anchorId=1&anchorValue=2024-12-03T10%3A20%3A30.4000000Z&direction=Backward";

        AssertTorrentPage(page, [_torrents[0]], expectedNextPage2, expectedPreviousPage2);

        page = await _client.GetFromJsonAsync<GetTorrentPageResponse>(expectedPreviousPage2).ConfigureAwait(false);

        AssertTorrentPage(page, [], null, null);
    }

    [TestCaseSource(nameof(GetGetTorrentPageAsyncBadRequestTestCases))]
    public async Task GetTorrentPageAsync_WhenSpecificParametersAreUsed_ReturnsExpectedValidationProblem(
        GetTorrentPageParameters parameters,
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

    private static IEnumerable<TestCaseData<GetTorrentPageParameters, string, string>>
        GetGetTorrentPageAsyncBadRequestTestCases()
    {
        yield return new(
            new GetTorrentPageParameters(Take: 0),
            nameof(GetTorrentPageParameters.Take),
            $"The field Take must be between 1 and {GetTorrentPageParameters.MaxTake}.")
        {
            TestName = "GetTorrentPageAsync_WhenTakeIsZero_ReturnsValidationProblem"
        };

        yield return new(
            new GetTorrentPageParameters(Take: 1001),
            nameof(GetTorrentPageParameters.Take),
            $"The field Take must be between 1 and {GetTorrentPageParameters.MaxTake}.")
        {
            TestName = "GetTorrentPageAsync_WhenTakeIsTooLarge_ReturnsValidationProblem"
        };

        yield return new(
            new GetTorrentPageParameters(OrderBy: (GetTorrentPageOrder)999),
            nameof(GetTorrentPageParameters.OrderBy),
            "The field OrderBy is invalid.")
        {
            TestName = "GetTorrentPageAsync_WhenOrderByIsInvalid_ReturnsValidationProblem"
        };

        yield return new(
            new GetTorrentPageParameters(OrderBy: GetTorrentPageOrder.Id, AnchorValue: "abc"),
            nameof(GetTorrentPageParameters.AnchorValue),
            "When OrderBy is 'Id', AnchorValue must be 'null'.")
        {
            TestName = "GetTorrentPageAsync_WhenOrderByIsIdAndAnchorValueIsNotNull_ReturnsValidationProblem"
        };

        yield return new(
            new GetTorrentPageParameters(OrderBy: GetTorrentPageOrder.IdDesc, AnchorValue: "abc"),
            nameof(GetTorrentPageParameters.AnchorValue),
            "When OrderBy is 'IdDesc', AnchorValue must be 'null'.")
        {
            TestName = "GetTorrentPageAsync_WhenOrderByIsIdDescAndAnchorValueIsNotNull_ReturnsValidationProblem"
        };

        yield return new(
            new GetTorrentPageParameters(OrderBy: GetTorrentPageOrder.RefreshDate, AnchorValue: "abc"),
            nameof(GetTorrentPageParameters.AnchorValue),
            $"When OrderBy is 'RefreshDate', AnchorValue must match '{GetTorrentPageParameters.Iso8601DateRegexPattern}'.")
        {
            TestName = "GetTorrentPageAsync_WhenOrderByIsRefreshDateAndAnchorValueIsInvalid_ReturnsValidationProblem"
        };

        yield return new(
            new GetTorrentPageParameters(OrderBy: GetTorrentPageOrder.RefreshDateDesc, AnchorValue: "abc"),
            nameof(GetTorrentPageParameters.AnchorValue),
            $"When OrderBy is 'RefreshDateDesc', AnchorValue must match '{GetTorrentPageParameters.Iso8601DateRegexPattern}'.")
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
}
