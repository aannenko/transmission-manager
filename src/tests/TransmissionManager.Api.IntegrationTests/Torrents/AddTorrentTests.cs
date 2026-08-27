using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
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
internal sealed class AddTorrentTests
{
    private static readonly Torrent[] _initialTorrents = [TestData.Database.CreateInitialTorrents()[0]];

    #region Transmission Test Data

    // Common

    private static readonly TestResponse _invalidHeaderResponse = new(
        HttpStatusCode.Conflict,
        TestData.Transmission.ConflictResponseHeaders,
        TestData.Transmission.ConflictResponseBody);

    // Add New Torrent

    private static readonly string _addNewTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.AddTorrentRequestBodyFormat,
        TestData.WebPages.FourthPageMagnetNew,
        _initialTorrents[0].DownloadDir);

    private static readonly TestRequest _addNewTorrentInvalidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.EmptyRequestHeaders,
        _addNewTorrentRequestBody);

    private static readonly TestRequest _addNewTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _addNewTorrentRequestBody);

    private const string _addNewTorrentResponseHashString = "3A81AAA70E75439D332C146ABDE899E546356BE2";
    private const int _addNewTorrentResponseId = 26;
    private const string _addNewTorrentResponseName = "TV Show 4";
    private static readonly string _addNewTorrentResponseBody = string.Format(
        null,
        TestData.Transmission.AddTorrentAddedResponseBodyFormat,
        _addNewTorrentResponseHashString,
        _addNewTorrentResponseId,
        _addNewTorrentResponseName);

    private static readonly TestResponse _addNewTorrentValidHeaderResponse = new(
        HttpStatusCode.Created,
        TestData.Transmission.DefaultResponseHeaders,
        _addNewTorrentResponseBody);

    // Add Existing Torrent

    private static readonly string _addExistingTorrentRequestBody = string.Format(
        null,
        TestData.Transmission.AddTorrentRequestBodyFormat,
        TestData.WebPages.FirstPageMagnetExisting,
        _initialTorrents[0].DownloadDir);

    private static readonly string _addExistingTorrentResponseBody = string.Format(
        null,
        TestData.Transmission.AddTorrentDuplicateResponseBodyFormat,
        _initialTorrents[0].HashString,
        25,
        _initialTorrents[0].Name);

    private static readonly TestRequest _addExistingTorrentInvalidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.EmptyRequestHeaders,
        _addExistingTorrentRequestBody);

    private static readonly TestRequest _addExistingTorrentValidHeaderRequest = new(
        HttpMethod.Post,
        TestData.Transmission.ApiUri,
        TestData.Transmission.FilledRequestHeaders,
        _addExistingTorrentRequestBody);

    private static readonly TestResponse _addExistingTorrentValidHeaderResponse = new(
        HttpStatusCode.Created,
        TestData.Transmission.DefaultResponseHeaders,
        _addExistingTorrentResponseBody);

    // Request-Response map

    private static readonly Dictionary<TestRequest, TestResponse> _transmissionRequestResponseMap = new()
    {
        [_addNewTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_addNewTorrentValidHeaderRequest] = _addNewTorrentValidHeaderResponse,
        [_addExistingTorrentInvalidHeaderRequest] = _invalidHeaderResponse,
        [_addExistingTorrentValidHeaderRequest] = _addExistingTorrentValidHeaderResponse,
    };

    #endregion

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

    [Test]
    public async Task AddTorrentAsync_WhenSourceUriIsNew_AddsTorrentToTransmissionAndDb()
    {
        var dto = new AddTorrentRequest
        {
            SourceUri = new("https://torrenttracker.com/forum/viewtopic.php?t=1234570"),
            DownloadDir = "/tvshows",
            Cron = "0 9,17 * * *"
        };

        var countBefore = await GetTorrentCountAsync().ConfigureAwait(false);

        var response = await _client.PostAsJsonAsync(EndpointAddresses.Torrents, dto).ConfigureAwait(false);

        const long expectedId = 2;
        var expectedLocation = $"{EndpointAddresses.Torrents}/{expectedId}";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers.Location?.OriginalString, Is.EqualTo(expectedLocation));
        }

        var addTorrentResponse = await response.Content.ReadFromJsonAsync<AddTorrentResponse>().ConfigureAwait(false);

        Assert.That(addTorrentResponse, Is.Not.Null);
        Assert.That(addTorrentResponse.TransmissionResult, Is.EqualTo(TransmissionAddResult.Added));

        var expectedTorrent = new Torrent
        {
            Id = expectedId,
            HashString = _addNewTorrentResponseHashString,
            RefreshDate = DateTime.UtcNow,
            Name = _addNewTorrentResponseName,
            SourceUri = dto.SourceUri.OriginalString,
            SourceKind = DbSourceKind.WebPage,
            DownloadDir = dto.DownloadDir,
            Cron = dto.Cron,
            Version = 1,
        };

        TorrentAssertions.AssertEqual(addTorrentResponse.TorrentDto, expectedTorrent, TimeSpan.FromSeconds(1));

        var getTorrentResponse = await _client.GetAsync(expectedLocation).ConfigureAwait(false);

        Assert.That(getTorrentResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var newTorrent = await getTorrentResponse.Content.ReadFromJsonAsync<TorrentDto>().ConfigureAwait(false);

        TorrentAssertions.AssertEqual(newTorrent, expectedTorrent, TimeSpan.FromSeconds(2));

        var countAfter = await GetTorrentCountAsync().ConfigureAwait(false);
        Assert.That(countAfter, Is.EqualTo(countBefore + 1));
    }

    [Test]
    public async Task AddTorrentAsync_WhenSourceUriExists_ReturnsConflictResponse()
    {
        var dto = new AddTorrentRequest
        {
            SourceUri = new(_initialTorrents[0].SourceUri),
            DownloadDir = _initialTorrents[0].DownloadDir,
            Cron = _initialTorrents[0].Cron,
        };

        var response = await _client.PostAsJsonAsync(EndpointAddresses.Torrents, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            var error =
                $"Torrent '{dto.SourceUri}' addition failed: 'A torrent with the same URI or hash already exists.'.";

            Assert.That(problemDetails.Detail, Is.EqualTo(error));
            Assert.That(problemDetails.Extensions.TryGetValue("transmissionResult", out var transmissionResult));
            Assert.That(transmissionResult?.ToString(), Is.EqualTo("Duplicate"));
        }
    }

    /// <remarks>
    /// Separate from the cron case on purpose: property-level validation short-circuits, so
    /// <c>IValidatableObject.Validate</c> - where the kind-conditional pattern rule lives - never
    /// runs while any attribute is also failing. Merging the two would silently stop testing this.
    /// </remarks>
    [Test]
    public async Task AddTorrentAsync_WhenMagnetRegexPatternDoesNotMatchSourceKind_ReturnsValidationError()
    {
        var dto = new AddTorrentRequest
        {
            SourceUri = new("https://torrenttracker.com/forum/viewtopic.php?t=1234570"),
            DownloadDir = "/tvshows",
            MagnetRegexPattern = "(?<value>[a-fA-F0-9]{40})", // valid for a JSON source, not a web page
        };

        var response = await _client.PostAsJsonAsync(EndpointAddresses.Torrents, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Extensions.TryGetValue("errors", out var errorObject));
        Assert.That(errorObject?.ToString(), Contains.Substring(nameof(AddTorrentRequest.MagnetRegexPattern)));
    }

    [Test]
    public async Task AddTorrentAsync_WhenCronIsInvalid_ReturnsValidationError()
    {
        var dto = new AddTorrentRequest
        {
            SourceUri = new("https://torrenttracker.com/forum/viewtopic.php?t=1234570"),
            DownloadDir = "/tvshows",
            Cron = " "
        };

        var response = await _client.PostAsJsonAsync(EndpointAddresses.Torrents, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problemDetails, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(problemDetails.Title, Is.EqualTo("One or more validation errors occurred."));
            Assert.That(problemDetails.Extensions.TryGetValue("errors", out var errorObject));
            Assert.That(errorObject?.ToString(), Contains.Substring(nameof(AddTorrentRequest.Cron)));
        }
    }

    /// <remarks>
    /// Proves <c>[HttpUri]</c> is actually wired into minimal-API validation. It is the first plain
    /// <c>ValidationAttribute</c> in this project - its siblings all derive from
    /// <c>RegularExpressionAttribute</c> - so without this the 400 it is supposed to produce could
    /// silently be no validation at all, and a relative address would reach <c>HttpClient</c>.
    /// </remarks>
    [TestCase("/forum/viewtopic.php?t=1234570", UriKind.Relative)]
    [TestCase("ftp://torrenttracker.com/file", UriKind.Absolute)]
    public async Task AddTorrentAsync_WhenSourceUriIsNotFetchable_ReturnsValidationError(
        string address,
        UriKind uriKind)
    {
        var dto = new AddTorrentRequest
        {
            SourceUri = new(address, uriKind),
            DownloadDir = "/tvshows",
        };

        var response = await _client.PostAsJsonAsync(EndpointAddresses.Torrents, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key(nameof(AddTorrentRequest.SourceUri)));

        var errors = problem.Errors[nameof(AddTorrentRequest.SourceUri)];

        Assert.That(errors, Is.Not.Empty);
        Assert.That(errors[0], Contains.Substring("absolute http or https address"));
    }

    /// <remarks>
    /// The status code alone cannot tell validation from deserialisation: a <c>sourceKind</c> string
    /// that is not a member is rejected by the converter and also surfaces as 400. Asserting the
    /// property key is what proves <c>[EnumDataType]</c> is the thing doing the work.
    /// </remarks>
    [TestCase(999)]
    [TestCase(-5)]
    public async Task AddTorrentAsync_WhenSourceKindIsNotADefinedMember_ReturnsValidationError(int sourceKind)
    {
        var body =
            $$"""{"sourceUri":"https://torrenttracker.com/x","sourceKind":{{sourceKind}},"downloadDir":"/tvshows"}""";

        using var content = new StringContent(body, new MediaTypeHeaderValue("application/json"));
        var response = await _client.PostAsync(EndpointAddresses.Torrents, content).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key(nameof(AddTorrentRequest.SourceKind)));
    }

    /// <remarks>
    /// Anti-bot challenges served with a success status land here too.
    /// </remarks>
    [Test]
    public async Task AddTorrentAsync_WhenSourcePageHoldsNoMagnet_ReturnsBadRequest()
    {
        var dto = new AddTorrentRequest
        {
            SourceUri = new(TestData.WebPages.NoMagnetPageAddress),
            DownloadDir = "/tvshows",
        };

        var response = await _client.PostAsJsonAsync(EndpointAddresses.Torrents, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Detail, Contains.Substring("No magnet link was found"));
    }

    /// <remarks>
    /// The format is well-formed and would be accepted on a JSON source; what makes it invalid is
    /// the kind it arrives with. Refusing it stops a setting being stored that nothing will read,
    /// since the kind cannot be changed afterwards.
    /// </remarks>
    [Test]
    public async Task AddTorrentAsync_WhenMagnetFormatIsGivenForAWebPageSource_ReturnsValidationError()
    {
        var dto = new AddTorrentRequest
        {
            SourceUri = new("https://torrenttracker.com/forum/viewtopic.php?t=1234570"),
            DownloadDir = "/tvshows",
            JsonValueFormat = "magnet:?xt=urn:btih:{0}"
        };

        var response = await _client.PostAsJsonAsync(EndpointAddresses.Torrents, dto).ConfigureAwait(false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>().ConfigureAwait(false);

        Assert.That(problem, Is.Not.Null);
        Assert.That(problem.Errors, Contains.Key(nameof(AddTorrentRequest.JsonValueFormat)));
    }

    private async Task<long> GetTorrentCountAsync()
    {
        var response = await _client
            .GetAsync(new GetTorrentPageParameters(Take: 1).ToPathAndQueryString())
            .ConfigureAwait(false);

        var page = await response.Content.ReadFromJsonAsync<GetTorrentPageResponse>().ConfigureAwait(false);
        return page!.Count;
    }
}
