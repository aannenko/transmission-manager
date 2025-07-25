﻿using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.IntegrationTests.Helpers;
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

        var expectedNextPage = EndpointAddresses.Torrents + "?take=3&anchorId=3";
        var expectedPreviousPage = EndpointAddresses.Torrents + "?take=3&anchorId=2&direction=Backward";

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

        var expectedNextPage = EndpointAddresses.Torrents +
            "?take=2&anchorId=3&propertyStartsWith=TV+Show&cronExists=True";

        var expectedPreviousPage = EndpointAddresses.Torrents +
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

        var expectedNextPage = $"{EndpointAddresses.Torrents}?take=1&anchorId=2" +
            "&propertyStartsWith=https%3A%2F%2FtorrentTracker.com%2Fforum%2Fviewtopic.php%3Ft%3D1234568";

        var expectedPreviousPage = $"{EndpointAddresses.Torrents}?take=1&anchorId=2&direction=Backward" +
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

        var expectedNextPage = $"{EndpointAddresses.Torrents}?take=1&anchorId=2" +
            $"&propertyStartsWith={TestData.Database.SecondTorrentHashString}";

        var expectedPreviousPage = $"{EndpointAddresses.Torrents}?take=1&anchorId=2&direction=Backward" +
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
            OrderBy: FindTorrentPageOrder.NameDesc,
            Take: 2);

        var page = await _client
            .GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        var expectedNextPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=2&anchorValue=TV+Show+2";

        var expectedPreviousPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=3&anchorValue=TV+Show+3&direction=Backward";

        AssertTorrentPage(page, 2, expectedNextPage1, expectedPreviousPage1);
        TorrentAssertions.AssertEqual(page.Torrents[0], 3, _torrents[2]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 2, _torrents[1]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedNextPage1).ConfigureAwait(false);

        var expectedNextPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=1&anchorValue=TV+Show+1";

        var expectedPreviousPage2 = expectedNextPage2 + "&direction=Backward";

        AssertTorrentPage(page, 1, expectedNextPage2, expectedPreviousPage2);
        TorrentAssertions.AssertEqual(page.Torrents[0], 1, _torrents[0]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedNextPage2).ConfigureAwait(false);

        AssertTorrentPage(page, 0, null, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenOrderByIsNameDescAndDirectionIsBackwardAndTakeIsTwo_ReturnsCorrectPagesAndPreviousPageLinks()
    {
        var parameters = new FindTorrentPageParameters(
            OrderBy: FindTorrentPageOrder.NameDesc,
            Direction: FindTorrentPageDirection.Backward,
            Take: 2);

        var page = await _client
            .GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        var expectedNextPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=1&anchorValue=TV+Show+1";

        var expectedPreviousPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=2&anchorValue=TV+Show+2&direction=Backward";

        AssertTorrentPage(page, 2, expectedNextPage1, expectedPreviousPage1);
        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 1, _torrents[0]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedPreviousPage1).ConfigureAwait(false);

        var expectedNextPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=3&anchorValue=TV+Show+3";

        var expectedPreviousPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=NameDesc&anchorId=3&anchorValue=TV+Show+3&direction=Backward";

        AssertTorrentPage(page, 1, expectedNextPage2, expectedPreviousPage2);
        TorrentAssertions.AssertEqual(page.Torrents[0], 3, _torrents[2]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedPreviousPage2).ConfigureAwait(false);

        AssertTorrentPage(page, 0, null, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenOrderByIsRefreshDateAndDirectionIsBackwardAndTakeIsTwo_ReturnsCorrectPagesAndPreviousPageLinks()
    {
        var parameters = new FindTorrentPageParameters(
            OrderBy: FindTorrentPageOrder.RefreshDate,
            Take: 2);

        var page = await _client
            .GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        var expectedNextPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDate&anchorId=2&anchorValue=2023-11-02T11%3A22%3A33.4440000Z";

        var expectedPreviousPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDate&anchorId=3&anchorValue=2022-10-01T12%3A34%3A56.7770000Z&direction=Backward";

        AssertTorrentPage(page, 2, expectedNextPage1, expectedPreviousPage1);
        TorrentAssertions.AssertEqual(page.Torrents[0], 3, _torrents[2]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 2, _torrents[1]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedNextPage1).ConfigureAwait(false);

        var expectedNextPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDate&anchorId=1&anchorValue=2024-12-03T10%3A20%3A30.4000000Z";

        var expectedPreviousPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDate&anchorId=1&anchorValue=2024-12-03T10%3A20%3A30.4000000Z&direction=Backward";

        AssertTorrentPage(page, 1, expectedNextPage2, expectedPreviousPage2);
        TorrentAssertions.AssertEqual(page.Torrents[0], 1, _torrents[0]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedNextPage2).ConfigureAwait(false);

        AssertTorrentPage(page, 0, null, null);
    }

    [Test]
    public async Task FindTorrentPageAsync_WhenOrderByIsRefreshDateDescAndDirectionIsBackwardAndTakeIsTwo_ReturnsCorrectPagesAndPreviousPageLinks()
    {
        var parameters = new FindTorrentPageParameters(
            OrderBy: FindTorrentPageOrder.RefreshDateDesc,
            Direction: FindTorrentPageDirection.Backward,
            Take: 2);

        var page = await _client
            .GetFromJsonAsync<FindTorrentPageResponse>(parameters.ToPathAndQueryString())
            .ConfigureAwait(false);

        var expectedNextPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDateDesc&anchorId=3&anchorValue=2022-10-01T12%3A34%3A56.7770000Z";

        var expectedPreviousPage1 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDateDesc&anchorId=2&anchorValue=2023-11-02T11%3A22%3A33.4440000Z&direction=Backward";

        AssertTorrentPage(page, 2, expectedNextPage1, expectedPreviousPage1);
        TorrentAssertions.AssertEqual(page.Torrents[0], 2, _torrents[1]);
        TorrentAssertions.AssertEqual(page.Torrents[1], 3, _torrents[2]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedPreviousPage1).ConfigureAwait(false);

        var expectedNextPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDateDesc&anchorId=1&anchorValue=2024-12-03T10%3A20%3A30.4000000Z";

        var expectedPreviousPage2 = EndpointAddresses.Torrents +
            "?take=2&orderBy=RefreshDateDesc&anchorId=1&anchorValue=2024-12-03T10%3A20%3A30.4000000Z&direction=Backward";

        AssertTorrentPage(page, 1, expectedNextPage2, expectedPreviousPage2);
        TorrentAssertions.AssertEqual(page.Torrents[0], 1, _torrents[0]);

        page = await _client.GetFromJsonAsync<FindTorrentPageResponse>(expectedPreviousPage2).ConfigureAwait(false);

        AssertTorrentPage(page, 0, null, null);
    }

    [TestCaseSource(nameof(GetFindTorrentPageAsyncBadRequestTestCases))]
    public async Task FindTorrentPageAsync_WhenSpecificParametersAreUsed_ReturnsExpectedValidationProblem(
        FindTorrentPageParameters parameters,
        string problematicParameterName,
        string expectedErrorMessage)
    {
        var response = await _client.GetAsync(parameters.ToPathAndQueryString()).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem!.Errors, Has.Count.EqualTo(1));
            Assert.That(problem.Errors, Contains.Key(problematicParameterName));
            if (problem.Errors.TryGetValue(problematicParameterName, out var errors))
            {
                Assert.That(errors, Has.Length.EqualTo(1));
                Assert.That(errors[0], Is.EqualTo(expectedErrorMessage));
            }
        }
    }

    private static IEnumerable<TestCaseData<FindTorrentPageParameters, string, string>>
        GetFindTorrentPageAsyncBadRequestTestCases()
    {
        yield return new(
            new FindTorrentPageParameters(Take: 0),
            nameof(FindTorrentPageParameters.Take),
            $"The field Take must be between 1 and {FindTorrentPageParameters.MaxTake}.")
        {
            TestName = "FindTorrentPageAsync_WhenTakeIsZero_ReturnsValidationProblem"
        };

        yield return new(
            new FindTorrentPageParameters(Take: 1001),
            nameof(FindTorrentPageParameters.Take),
            $"The field Take must be between 1 and {FindTorrentPageParameters.MaxTake}.")
        {
            TestName = "FindTorrentPageAsync_WhenTakeIsTooLarge_ReturnsValidationProblem"
        };

        yield return new(
            new FindTorrentPageParameters(OrderBy: (FindTorrentPageOrder)999),
            nameof(FindTorrentPageParameters.OrderBy),
            "The field OrderBy is invalid.")
        {
            TestName = "FindTorrentPageAsync_WhenOrderByIsInvalid_ReturnsValidationProblem"
        };

        yield return new(
            new FindTorrentPageParameters(OrderBy: FindTorrentPageOrder.Id, AnchorValue: "abc"),
            nameof(FindTorrentPageParameters.AnchorValue),
            "When OrderBy is 'Id', AnchorValue must be 'null'.")
        {
            TestName = "FindTorrentPageAsync_WhenOrderByIsIdAndAnchorValueIsNotNull_ReturnsValidationProblem"
        };

        yield return new(
            new FindTorrentPageParameters(OrderBy: FindTorrentPageOrder.IdDesc, AnchorValue: "abc"),
            nameof(FindTorrentPageParameters.AnchorValue),
            "When OrderBy is 'IdDesc', AnchorValue must be 'null'.")
        {
            TestName = "FindTorrentPageAsync_WhenOrderByIsIdDescAndAnchorValueIsNotNull_ReturnsValidationProblem"
        };

        yield return new(
            new FindTorrentPageParameters(OrderBy: FindTorrentPageOrder.RefreshDate, AnchorValue: "abc"),
            nameof(FindTorrentPageParameters.AnchorValue),
            $"When OrderBy is 'RefreshDate', AnchorValue must match '{FindTorrentPageParameters.Iso8601DateRegexPattern}'.")
        {
            TestName = "FindTorrentPageAsync_WhenOrderByIsRefreshDateAndAnchorValueIsInvalid_ReturnsValidationProblem"
        };

        yield return new(
            new FindTorrentPageParameters(OrderBy: FindTorrentPageOrder.RefreshDateDesc, AnchorValue: "abc"),
            nameof(FindTorrentPageParameters.AnchorValue),
            $"When OrderBy is 'RefreshDateDesc', AnchorValue must match '{FindTorrentPageParameters.Iso8601DateRegexPattern}'.")
        {
            TestName = "FindTorrentPageAsync_WhenOrderByIsRefreshDateDescAndAnchorValueIsInvalid_ReturnsValidationProblem"
        };
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
            Assert.That(page.Torrents, Has.Count.EqualTo(expectedCount));
            Assert.That(page.NextPageAddress, Is.EqualTo(expectedNextPage));
            Assert.That(page.PreviousPageAddress, Is.EqualTo(expectedPreviousPage));
        }
    }
}
