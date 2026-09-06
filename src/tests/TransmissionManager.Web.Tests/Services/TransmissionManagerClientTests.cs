using System.Net;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Dto.Transmission;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.Web.Dto;
using TransmissionManager.Web.Extensions;
using TransmissionManager.Web.Services;

namespace TransmissionManager.Web.Tests.Services;

[Parallelizable(ParallelScope.Self)]
internal sealed class TransmissionManagerClientTests
{
    private static readonly Uri _endpoint = new("http://localhost/api/v1/torrents");

    private const string _addRequestBody =
        """{"sourceUri":"https://api.example/topics#/result/1/hash","sourceKind":"JsonPointer","downloadDir":"/tvshows","magnetRegexPattern":"[a-fA-F0-9]{40}","jsonValueFormat":"magnet:?xt=urn:btih:{0}"}""";

    [Test]
    public async Task AddTorrentAsync_WhenProblemDetailsContainsErrors_ReportsThemWithResponseMetadata()
    {
        const string problem = """
            {
                "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                "title": "Conflict",
                "status": 409,
                "errors": {
                    "torrent": ["A torrent with the same URI or hash already exists."]
                },
                "transmissionResult": "Added"
            }
            """;

        using var handler = CreateAddHandler(HttpStatusCode.Conflict, problem);
        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client.AddTorrentAsync(CreateRequest()).ConfigureAwait(false);
        var problemDetails = result.ProblemDetails!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Conflict));
            Assert.That(result.Value, Is.Null);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(
                problemDetails.Errors!["torrent"],
                Is.EqualTo(["A torrent with the same URI or hash already exists."]));
            Assert.That(problemDetails.TransmissionResult, Is.EqualTo(TransmissionAddResult.Added));
        }
    }

    /// <remarks>
    /// The status code is the only thing left to report when a failure carries no problem details,
    /// so losing it would leave the caller with nothing to say.
    /// </remarks>
    [Test]
    public async Task AddTorrentAsync_WhenFailureBodyIsNotProblemDetails_KeepsTheStatusCode()
    {
        using var handler = CreateAddHandler(HttpStatusCode.BadGateway, "<html>Proxy failure</html>");
        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client.AddTorrentAsync(CreateRequest()).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Failed));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
            Assert.That(result.ProblemDetails, Is.Null);
        }
    }

    /// <remarks>
    /// Only a build older than the API can meet a result it does not know, since both read the same
    /// enum. Typing that member costs the build the rest of the body, because the converter refuses
    /// the name and the whole read fails - an accepted trade, pinned here so it stays a decision
    /// rather than a surprise. The status still says what happened.
    /// </remarks>
    [Test]
    public async Task AddTorrentAsync_WhenTransmissionResultIsNotKnownToThisBuild_ReportsAPlainFailure()
    {
        const string problem = """
            {
                "status": 409,
                "errors": {
                    "torrent": ["A torrent with the same URI or hash already exists."]
                },
                "transmissionResult": "Unknown"
            }
            """;

        using var handler = CreateAddHandler(HttpStatusCode.Conflict, problem);
        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client.AddTorrentAsync(CreateRequest()).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Conflict));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(result.ProblemDetails, Is.Null);
        }
    }

    [Test]
    public async Task AddTorrentAsync_WhenRequestSucceeds_UsesJsonSourceFieldsAndReadsResponse()
    {
        const string response = """
            {
                "torrentDto": {
                    "id": 7,
                    "hashString": "0BDA511316A069E86DD8EE8A3610475D2013A7FA",
                    "refreshDate": "2026-09-03T10:00:00+00:00",
                    "name": "Torrent",
                    "sourceUri": "https://api.example/topics#/result/1/hash",
                    "sourceKind": "JsonPointer",
                    "downloadDir": "/tvshows",
                    "magnetRegexPattern": "[a-fA-F0-9]{40}",
                    "jsonValueFormat": "magnet:?xt=urn:btih:{0}",
                    "version": 1
                },
                "transmissionResult": "Added"
            }
            """;

        var request = CreateRequest();
        using var handler = CreateAddHandler(HttpStatusCode.Created, response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client.AddTorrentAsync(request).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Success));
            Assert.That(result.Value!.TorrentDto.SourceKind, Is.EqualTo(TorrentSourceKind.JsonPointer));
            Assert.That(result.Value.TorrentDto.JsonValueFormat, Is.EqualTo(request.JsonValueFormat));
            Assert.That(result.Value.TransmissionResult, Is.EqualTo(TransmissionAddResult.Added));
        }
    }

    /// <remarks>
    /// The likeliest way to meet an address that answers but is not this API. The charsets are the
    /// point: only UTF-8, UTF-16, ASCII and Latin-1 are built in, so windows-1252, the common
    /// "utf8" typo and UTF-7 fail before the JSON is read at all, raising something other than a
    /// <c>JsonException</c>.
    /// </remarks>
    [TestCase("text/html; charset=utf-8")]
    [TestCase("text/html; charset=windows-1252")]
    [TestCase("application/json; charset=utf8")]
    [TestCase("text/html; charset=utf-7")]
    [TestCase("text/html; charset=bogus")]
    [TestCase("text/html")]
    public async Task GetAppVersionAsync_WhenAnAddressAnswersWithoutJson_ReportsFailure(string contentType)
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, new("http://localhost/api/v1/appversion")),
            new(
                HttpStatusCode.OK,
                Content: "<html>Some other application</html>",
                ContentType: contentType));

        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client.GetAppVersionAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Failed));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }

    /// <remarks>
    /// Problem details are the other body this client parses, and a proxy that fails a request is
    /// exactly where an unreadable charset turns up, so the status has to survive one.
    /// </remarks>
    [TestCase("text/html; charset=windows-1252")]
    [TestCase("text/html; charset=utf-8")]
    public async Task AddTorrentAsync_WhenFailureBodyHasAnUnreadableCharset_KeepsTheStatusCode(string contentType)
    {
        using var handler = CreateAddHandler(
            HttpStatusCode.BadGateway,
            "<html>Proxy failure</html>",
            contentType);

        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client.AddTorrentAsync(CreateRequest()).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Failed));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
            Assert.That(result.ProblemDetails, Is.Null);
        }
    }

    /// <remarks>
    /// The response record declares the torrent as required, but the serializer does not enforce a
    /// non-nullable annotation, so a torrentless success would otherwise be handed to the page as a
    /// success and dereferenced.
    /// </remarks>
    [TestCase("""{"transmissionResult":"Added"}""")]
    [TestCase("""{"torrentDto":null,"transmissionResult":"Added"}""")]
    public async Task AddTorrentAsync_WhenSuccessCarriesNoTorrent_ReportsFailure(string response)
    {
        using var handler = CreateAddHandler(HttpStatusCode.Created, response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client.AddTorrentAsync(CreateRequest()).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Failed));
            Assert.That(result.Value, Is.Null);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        }
    }

    /// <inheritdoc cref="AddTorrentAsync_WhenSuccessCarriesNoTorrent_ReportsFailure"/>
    [TestCase("""{"transmissionResult":"Added"}""")]
    [TestCase("""{"torrentDto":null,"transmissionResult":"Added"}""")]
    public async Task RefreshTorrentByIdAsync_WhenSuccessCarriesNoTorrent_ReportsFailure(string response)
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new("http://localhost/api/v1/torrents/7")),
            new(HttpStatusCode.OK, Content: response));

        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client.RefreshTorrentByIdAsync(7).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Failed));
            Assert.That(result.Value, Is.Null);
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }

    [Test]
    public async Task GetTorrentByIdAsync_WhenTorrentIsMissing_ReportsNotFound()
    {
        const string problem = """{"status":404,"errors":{"id":["No such torrent."]}}""";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, new("http://localhost/api/v1/torrents/7")),
            new(HttpStatusCode.NotFound, Content: problem));
        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client.GetTorrentByIdAsync(7).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.NotFound));
            Assert.That(result.Value, Is.Null);
            Assert.That(result.ProblemDetails?.Errors?["id"], Is.EqualTo(["No such torrent."]));
        }
    }

    [TestCase(
        "magnet:?xt=urn:btih:{0}&tr=https%3A%2F%2Ftracker.example%2Fannounce",
        """{"jsonValueFormat":"magnet:?xt=urn:btih:{0}&tr=https%3A%2F%2Ftracker.example%2Fannounce"}""",
        TestName = "UpdateTorrentByIdAsync_WhenJsonValueFormatChanges_SerializesTheNewValue")]
    [TestCase(
        "",
        """{"jsonValueFormat":""}""",
        TestName = "UpdateTorrentByIdAsync_WhenJsonValueFormatIsCleared_SerializesAnEmptyString")]
    public async Task UpdateTorrentByIdAsync_WhenJsonValueFormatIsSent_SerializesIt(
        string jsonValueFormat,
        string expectedRequest)
    {
        using var handler = new FakeHttpMessageHandler(
            new(
                HttpMethod.Patch,
                new("http://localhost/api/v1/torrents/7?version=3"),
                Content: expectedRequest),
            new(HttpStatusCode.NoContent));
        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client
            .UpdateTorrentByIdAsync(7, 3, new() { JsonValueFormat = jsonValueFormat })
            .ConfigureAwait(false);

        Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Success));
    }

    [Test]
    public async Task UpdateTorrentByIdAsync_WhenConflictHasCurrentVersion_ReturnsIt()
    {
        const string problem = """
            {
                "status": 409,
                "errors": {
                    "version": ["The torrent was modified by another request."]
                },
                "currentVersion": 4
            }
            """;

        using var handler = new FakeHttpMessageHandler(
            new(
                HttpMethod.Patch,
                new("http://localhost/api/v1/torrents/7?version=3"),
                Content: """{"downloadDir":"/tvshows/new"}"""),
            new(HttpStatusCode.Conflict, Content: problem));
        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client
            .UpdateTorrentByIdAsync(7, 3, new() { DownloadDir = "/tvshows/new" })
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Conflict));
            Assert.That(result.CurrentVersion, Is.EqualTo(4));
            Assert.That(
                result.ProblemDetails?.Errors?["version"],
                Is.EqualTo(["The torrent was modified by another request."]));
        }
    }

    [Test]
    public async Task DeleteTorrentByIdAsync_WhenTorrentIsGone_ReportsNotFoundWithProblemDetails()
    {
        const string problem = """{"status":404,"errors":{"id":["No such torrent."]}}""";

        using var handler = new FakeHttpMessageHandler(
            new(
                HttpMethod.Delete,
                new("http://localhost/api/v1/torrents/7?version=3&deleteType=Local")),
            new(HttpStatusCode.NotFound, Content: problem));
        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client
            .DeleteTorrentByIdAsync(7, 3, DeleteTorrentByIdType.Local)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.NotFound));
            Assert.That(result.CurrentVersion, Is.Null);
            Assert.That(result.ProblemDetails?.Errors?["id"], Is.EqualTo(["No such torrent."]));
        }
    }

    /// <remarks>
    /// A dependency failure has no outcome of its own, but it still carries what the daemon refused,
    /// which is the whole reason the result keeps the problem details rather than just a status.
    /// </remarks>
    [Test]
    public async Task DeleteTorrentByIdAsync_WhenTransmissionFails_ReportsWhatItRefused()
    {
        const string problem = """
            {
                "status": 424,
                "errors": {
                    "transmission": ["Transmission could not be reached."]
                }
            }
            """;

        using var handler = new FakeHttpMessageHandler(
            new(
                HttpMethod.Delete,
                new("http://localhost/api/v1/torrents/7?version=3&deleteType=LocalAndTransmission")),
            new(HttpStatusCode.FailedDependency, Content: problem));
        using var httpClient = new HttpClient(handler) { BaseAddress = new("http://localhost") };
        var client = new TransmissionManagerClient(httpClient);

        var result = await client
            .DeleteTorrentByIdAsync(7, 3, DeleteTorrentByIdType.LocalAndTransmission)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(ApiResultStatus.Failed));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.FailedDependency));
            Assert.That(
                result.ProblemDetails!.JoinErrorMessages(),
                Is.EqualTo("transmission: Transmission could not be reached."));
        }
    }

    /// <remarks>
    /// Every caller reads a default result after a call that never reached the server, so this must
    /// never be a success.
    /// </remarks>
    [Test]
    public void ApiResult_WhenDefault_IsAFailureCarryingNothing()
    {
        var defaultResult = default(ApiResult);
        var defaultTypedResult = default(ApiResult<TorrentDto>);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(defaultResult.Status, Is.EqualTo(ApiResultStatus.Failed));
            Assert.That(defaultResult.StatusCode, Is.Null);
            Assert.That(defaultTypedResult.Status, Is.EqualTo(ApiResultStatus.Failed));
            Assert.That(defaultTypedResult.Value, Is.Null);
        }
    }

    private static AddTorrentRequest CreateRequest() =>
        new()
        {
            SourceUri = new("https://api.example/topics#/result/1/hash"),
            SourceKind = TorrentSourceKind.JsonPointer,
            DownloadDir = "/tvshows",
            MagnetRegexPattern = "[a-fA-F0-9]{40}",
            JsonValueFormat = "magnet:?xt=urn:btih:{0}",
        };

    private static FakeHttpMessageHandler CreateAddHandler(
        HttpStatusCode statusCode,
        string content,
        string? contentType = null) =>
        new(
            new(HttpMethod.Post, _endpoint, Content: _addRequestBody),
            new(statusCode, Content: content, ContentType: contentType));
}
