using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.BaseTests.Options;
using TransmissionManager.Transmission.Dto;
using TransmissionManager.Transmission.Options;
using TransmissionManager.Transmission.Serialization;
using TransmissionManager.Transmission.Services;
using TorrentFields = TransmissionManager.Transmission.Dto.TransmissionTorrentGetRequestFields;

namespace TransmissionManager.Transmission.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TransmissionClientTests
{
    private const string _transmissionRpcUri = "http://transmission:9091/transmission/rpc";
    private const string _twoTorrentsAllFieldsResponse = """
        {
            "arguments": {
                "torrents": [
                    {
                        "downloadDir": "/tvshows",
                        "hashString": "0bda511316a069e86dd8ee8a3610475d2013a7fa",
                        "name": "TV Show 1",
                        "percentDone": 1,
                        "sizeWhenDone": 34008064679
                    },
                    {
                        "downloadDir": "/tvshows",
                        "hashString": "738c60cbd44f0e9457ba2afdad9e9231d76243fe",
                        "name": "TV Show 2",
                        "percentDone": 0.5,
                        "sizeWhenDone": 28948006785
                    }
                ]
            },
            "result": "success"
        }
        """;

    private const string _twoTorrentsWithNoFieldsResponse = """{"arguments":{"torrents":[{},{}]},"result":"success"}""";

    private const string _removedSuccessfullyResponse = """{"arguments":{},"result":"success"}""";

    private static readonly FakeOptionsMonitor<TransmissionClientOptions> _options = new(new()
    {
        BaseAddress = "http://transmission:9091",
        RpcEndpointAddressSuffix = "/transmission/rpc"
    });

    [Test]
    public async Task GetTorrentsAsync_WhenCalledWithDefaultParameters_GetsAllTorrentsWithAllFields()
    {
        const string expectedRequest =
            """{"method":"torrent-get","arguments":{"fields":["hashString","name","sizeWhenDone","percentDone","downloadDir"]}}""";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: _twoTorrentsAllFieldsResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var response = await client.GetTorrentsAsync().ConfigureAwait(false);

        AssertTransmissionTorrentGetResponse(response, _twoTorrentsAllFieldsResponse);
    }

    [Test]
    public async Task GetTorrentsAsync_WhenPassedTwoHashStrings_GetsTwoTorrentsWithAllFields()
    {
        const string expectedRequest =
            """{"method":"torrent-get","arguments":{"ids":["0bda511316a069e86dd8ee8a3610475d2013a7fa","738c60cbd44f0e9457ba2afdad9e9231d76243fe"],"fields":["hashString","name","sizeWhenDone","percentDone","downloadDir"]}}""";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: _twoTorrentsAllFieldsResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var response = await client.GetTorrentsAsync(
            [
                "0bda511316a069e86dd8ee8a3610475d2013a7fa",
                "738c60cbd44f0e9457ba2afdad9e9231d76243fe"
            ]).ConfigureAwait(false);

        AssertTransmissionTorrentGetResponse(response, _twoTorrentsAllFieldsResponse);
    }

    [Test]
    public async Task GetTorrentsAsync_WhenPassedTwoRequestedFields_GetsAllTorrentsWithTwoFields()
    {
        const string expectedRequest = """{"method":"torrent-get","arguments":{"fields":["hashString","name"]}}""";
        const string twoTorrentsTwoFieldsResponse = """
            {
                "arguments": {
                    "torrents": [
                        {
                            "hashString": "0bda511316a069e86dd8ee8a3610475d2013a7fa",
                            "name": "TV Show 1"
                        },
                        {
                            "hashString": "738c60cbd44f0e9457ba2afdad9e9231d76243fe",
                            "name": "TV Show 2"
                        }
                    ]
                },
                "result": "success"
            }
            """;

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: twoTorrentsTwoFieldsResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var response = await client
            .GetTorrentsAsync(requestFields: [TorrentFields.HashString, TorrentFields.Name])
            .ConfigureAwait(false);

        AssertTransmissionTorrentGetResponse(response, twoTorrentsTwoFieldsResponse);
    }

    [Test]
    public async Task GetTorrentsAsync_WhenRequestedFieldsDoNotExist_GetsAllTorrentsWithNoFields()
    {
        const string expectedRequest = """{"method":"torrent-get","arguments":{"fields":[998,999]}}""";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: _twoTorrentsWithNoFieldsResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var response = await client
            .GetTorrentsAsync(requestFields: [(TorrentFields)998, (TorrentFields)999])
            .ConfigureAwait(false);

        AssertTransmissionTorrentGetResponse(response, _twoTorrentsWithNoFieldsResponse);
    }

    [Test]
    public async Task GetTorrentsAsync_WhenRequestedFieldsAreEmpty_GetsAllTorrentsWithNoFields()
    {
        const string expectedRequest = """{"method":"torrent-get","arguments":{"fields":[]}}""";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: _twoTorrentsWithNoFieldsResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var response = await client.GetTorrentsAsync(requestFields: []).ConfigureAwait(false);

        AssertTransmissionTorrentGetResponse(response, _twoTorrentsWithNoFieldsResponse);
    }

    [Test]
    public async Task GetTorrentsAsync_WhenHashStringDoesNotExist_GetsNoTorrents()
    {
        const string expectedRequest =
            """{"method":"torrent-get","arguments":{"ids":["0bda511316a069e86dd8ee8a3610475d2013a7fb"],"fields":[]}}""";

        const string noTorrentsResponse = """{"arguments":{"torrents":[]},"result":"success"}""";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: noTorrentsResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var response = await client
            .GetTorrentsAsync(["0bda511316a069e86dd8ee8a3610475d2013a7fb"], [])
            .ConfigureAwait(false);

        AssertTransmissionTorrentGetResponse(response, noTorrentsResponse);
    }

    [Test]
    public void GetTorrentsAsync_WhenCancellationTokenIsCanceled_ThrowsTaskCanceledExceptions()
    {
        const string expectedRequest =
            """{"method":"torrent-get","arguments":{"fields":["hashString","name","sizeWhenDone","percentDone","downloadDir"]}}""";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var task = client.GetTorrentsAsync(cancellationToken: new(true));

        Assert.That(
            async () => await task.ConfigureAwait(false),
            Throws.TypeOf<TaskCanceledException>());
    }

    [Test]
    public async Task AddTorrentUsingMagnetUriAsync_WhenNewMagnetAndDownloadDirProvided_ReturnsTorrentAdded()
    {
        const string expectedRequest =
            """{"method":"torrent-add","arguments":{"filename":"magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2&dn=Example+Name","download-dir":"/tvshows"}}""";

        const string torrentAddedResponse = """
            {
                "arguments": {
                    "torrent-added": {
                        "hashString": "3a81aaa70e75439d332c146abde899e546356be2",
                        "id": 1,
                        "name": "Example Name"
                    }
                },
                "result": "success"
            }
            """;

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: torrentAddedResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var response = await client
            .AddTorrentUsingMagnetUriAsync(new("magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2&dn=Example+Name"), "/tvshows")
            .ConfigureAwait(false);

        AssertTransmissionTorrentAddResponse(response, torrentAddedResponse);
    }

    [Test]
    public async Task AddTorrentUsingMagnetUriAsync_WhenMagnetAlreadyExists_ReturnsTorrentDuplicate()
    {
        const string expectedRequest =
            """{"method":"torrent-add","arguments":{"filename":"magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2&dn=Example+Name","download-dir":"/tvshows"}}""";

        const string torrentDuplicateResponse = """
            {
                "arguments": {
                    "torrent-duplicate": {
                        "hashString": "3A81AAA70E75439D332C146ABDE899E546356BE2",
                        "id": 1,
                        "name": "Example Name"
                    }
                },
                "result": "success"
            }
            """;

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: torrentDuplicateResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var response = await client
            .AddTorrentUsingMagnetUriAsync(new("magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2&dn=Example+Name"), "/tvshows")
            .ConfigureAwait(false);

        AssertTransmissionTorrentAddResponse(response, torrentDuplicateResponse);
    }

    [Test]
    public void AddTorrentUsingMagnetUriAsync_WhenMagnetIsInvalid_ThrowsHttpRequestException()
    {
        const string expectedRequest =
            """{"method":"torrent-add","arguments":{"filename":"magnet:?xt=urn:btih:INVALIDMAGNET","download-dir":"/tvshows"}}""";

        const string unrecognizedInfoResponse = """{"arguments":{},"result":"unrecognized info"}""";

        const string error = "Response from Transmission does not indicate success: 'unrecognized info'";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: unrecognizedInfoResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var task = client
            .AddTorrentUsingMagnetUriAsync(new("magnet:?xt=urn:btih:INVALIDMAGNET"), "/tvshows")
            .ConfigureAwait(false);

        Assert.That(async () => await task, Throws.TypeOf<HttpRequestException>().With.Message.EqualTo(error));
    }

    [Test]
    public void AddTorrentUsingMagnetUriAsync_WhenDownloadDirIsInvalid_ThrowsHttpRequestException()
    {
        const string expectedRequest =
            """{"method":"torrent-add","arguments":{"filename":"magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2","download-dir":"^&*("}}""";

        const string unrecognizedInfoResponse = """{"arguments":{},"result":"download directory path is not absolute"}""";

        const string error = "Response from Transmission does not indicate success: 'download directory path is not absolute'";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: unrecognizedInfoResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var task = client
            .AddTorrentUsingMagnetUriAsync(new("magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2"), "^&*(")
            .ConfigureAwait(false);

        Assert.That(async () => await task, Throws.TypeOf<HttpRequestException>().With.Message.EqualTo(error));
    }

    [Test]
    public async Task RemoveTorrentsAsync_WhenHashStringsExist_RemovesTorrents()
    {
        const string expectedRequest =
            """{"method":"torrent-remove","arguments":{"ids":["0bda511316a069e86dd8ee8a3610475d2013a7fa","738c60cbd44f0e9457ba2afdad9e9231d76243fe"],"delete-local-data":true}}""";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: _removedSuccessfullyResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var response = await client
            .RemoveTorrentsAsync(["0bda511316a069e86dd8ee8a3610475d2013a7fa", "738c60cbd44f0e9457ba2afdad9e9231d76243fe"], true)
            .ConfigureAwait(false);

        AssertTransmissionTorrentRemoveResponse(response, _removedSuccessfullyResponse);
    }

    [Test]
    public async Task RemoveTorrentsAsync_WhenArrayOfHashStringsIsEmpty_ReturnsSuccess()
    {
        const string expectedRequest =
            """{"method":"torrent-remove","arguments":{"ids":[],"delete-local-data":false}}""";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: _removedSuccessfullyResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var response = await client.RemoveTorrentsAsync([], false).ConfigureAwait(false);

        AssertTransmissionTorrentRemoveResponse(response, _removedSuccessfullyResponse);
    }

    [Test]
    public async Task RemoveTorrentsAsync_WhenArrayOfHashStringsIsNull_ReturnsSuccess()
    {
        const string expectedRequest =
            """{"method":"torrent-remove","arguments":{"delete-local-data":false}}""";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: _removedSuccessfullyResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var response = await client.RemoveTorrentsAsync(null, false).ConfigureAwait(false);

        AssertTransmissionTorrentRemoveResponse(response, _removedSuccessfullyResponse);
    }

    [Test]
    public async Task RemoveTorrentsAsync_WhenHashStringIsInvalid_ReturnsSuccess()
    {
        const string expectedRequest =
            """{"method":"torrent-remove","arguments":{"ids":["INVALID"],"delete-local-data":false}}""";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: _removedSuccessfullyResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var response = await client.RemoveTorrentsAsync(["INVALID"], false).ConfigureAwait(false);

        AssertTransmissionTorrentRemoveResponse(response, _removedSuccessfullyResponse);
    }

    [Test]
    public void RemoveTorrentsAsync_WhenResponseIsMalformed_ThrowsHttpRequestException()
    {
        const string expectedRequest =
            """{"method":"torrent-remove","arguments":{"ids":["0bda511316a069e86dd8ee8a3610475d2013a7fa"],"delete-local-data":false}}""";

        const string malformedResponse = "<html><body>502 Bad Gateway</body></html>";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: malformedResponse));

        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };
        var client = new TransmissionClient(_options, httpClient);

        var task = client
            .RemoveTorrentsAsync(["0bda511316a069e86dd8ee8a3610475d2013a7fa"], false)
            .ConfigureAwait(false);

        Assert.That(
            async () => await task,
            Throws.TypeOf<HttpRequestException>()
                .With.Message.Contains("Unexpected response from Transmission")
                .And.InnerException.TypeOf<JsonException>());
    }

    [TestCase("text/plain; charset=utf-8")]
    [TestCase("text/html; charset=iso-8859-1")]
    [TestCase("text/html; charset=bogus")]
    [TestCase("text/html; charset=utf-7")]
    public void GetTorrentsAsync_WhenResponseIsNotJson_QuotesItSummarized(string contentType)
    {
        var page = "<html>\r\n\u001b[31m\u0085" + new string('x', 100_000) + "</html>";

        var expectedError =
            "Unexpected response from Transmission. Cannot deserialize the following content to " +
            $"{typeof(TransmissionTorrentGetResponse).FullName}: '<html>___[31m_{new string('x', 66)}...'.";

        using var handler = new SequencedHttpMessageHandler();
        handler.Enqueue(_ => Answer(page, contentType));

        Assert.That(
            async () => await GetTorrentsAnsweredByAsync(handler).ConfigureAwait(false),
            Throws.TypeOf<HttpRequestException>().With.Message.EqualTo(expectedError));
    }

    /// <remarks>
    /// Served through a stream that cannot seek, as a body off the network is, so the excerpt can only be
    /// read if the body was buffered first.
    /// </remarks>
    [TestCaseSource(nameof(UnreadableBodyTestCases))]
    public void GetTorrentsAsync_WhenBodyIsNotAResponse_QuotesItDecodedAsUtf8(byte[] body, string expectedExcerpt)
    {
        using var handler = new SequencedHttpMessageHandler();
        handler.Enqueue(_ => Answer(body));

        Assert.That(
            async () => await GetTorrentsAnsweredByAsync(handler).ConfigureAwait(false),
            Throws.TypeOf<HttpRequestException>().With.Message.EqualTo(
                "Unexpected response from Transmission. Cannot deserialize the following content to " +
                $"{typeof(TransmissionTorrentGetResponse).FullName}: '{expectedExcerpt}'."));
    }

    [TestCase("application/json; charset=bogus")]
    [TestCase("application/json; charset=windows-1252")]
    [TestCase("application/json; charset=utf-7")]
    [TestCase("application/json; charset=iso-8859-1")]
    [TestCase("application/json; charset=utf-16")]
    public async Task GetTorrentsAsync_WhenResponseDeclaresACharsetOtherThanUtf8_ReadsItAsUtf8(string contentType)
    {
        const string name = "Caf\u00e9";
        const string body = $$"""{"arguments":{"torrents":[{"name":"{{name}}"}]},"result":"success"}""";

        using var handler = new SequencedHttpMessageHandler();
        handler.Enqueue(_ => Answer(body, contentType));

        var response = await GetTorrentsAnsweredByAsync(handler).ConfigureAwait(false);

        Assert.That(response.Arguments?.Torrents?.Single().Name, Is.EqualTo(name));
    }

    [Test]
    public void GetTorrentsAsync_WhenResultIsNotSuccess_QuotesItSummarized()
    {
        var body = $$"""{"arguments":{},"result":"bad\u001b[31m\r\nFORGED{{new string('x', 100)}}"}""";

        var expectedError =
            $"Response from Transmission does not indicate success: 'bad_[31m__FORGED{new string('x', 64)}...'";

        using var handler = new SequencedHttpMessageHandler();
        handler.Enqueue(_ => Answer(body, "application/json"));

        Assert.That(
            async () => await GetTorrentsAnsweredByAsync(handler).ConfigureAwait(false),
            Throws.TypeOf<HttpRequestException>().With.Message.EqualTo(expectedError));
    }

    [Test]
    public void GetTorrentsAsync_WhenRequestFails_QuotesTheReasonSummarized()
    {
        using var handler = new ThrowingHttpMessageHandler("boom\r\n\u001b[31mFORGED" + new string('x', 100));

        var expectedError = $"Request to Transmission failed unexpectedly: 'boom___[31mFORGED{new string('x', 63)}...'.";

        Assert.That(
            async () => await GetTorrentsAnsweredByAsync(handler).ConfigureAwait(false),
            Throws.TypeOf<HttpRequestException>().With.Message.EqualTo(expectedError));
    }

    [Test]
    public void GetTorrentsAsync_WhenStatusIsNotSuccess_ReportsItsCodeButNotItsReasonPhrase()
    {
        using var handler = new SequencedHttpMessageHandler();
        handler.Enqueue(static _ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            ReasonPhrase = "Bad\u001b[31mGateway\u0085",
        });

        Assert.That(
            async () => await GetTorrentsAnsweredByAsync(handler).ConfigureAwait(false),
            Throws.TypeOf<HttpRequestException>()
                .With.Message.EqualTo("The server responded with 502 BadGateway.")
                .And.Property(nameof(HttpRequestException.StatusCode)).EqualTo(HttpStatusCode.BadGateway));
    }

    private static void AssertTransmissionTorrentGetResponse(TransmissionTorrentGetResponse actual, string expected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);

        var deserialized = JsonSerializer.Deserialize(
            expected,
            TransmissionJsonSerializerContext.Default.TransmissionTorrentGetResponse);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(actual, Is.Not.Null);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual.Result, Is.EqualTo(deserialized.Result));
            Assert.That(actual.Arguments is null, Is.EqualTo(deserialized.Arguments is null));
            Assert.That(actual.Arguments?.Torrents is null, Is.EqualTo(deserialized.Arguments?.Torrents is null));
        }

        if (deserialized.Arguments?.Torrents is not null)
        {
            Assert.That(actual.Arguments.Torrents, Has.Count.EqualTo(deserialized.Arguments.Torrents.Count));
            using (Assert.EnterMultipleScope())
            {
                for (var i = 0; i < actual.Arguments.Torrents.Count; i++)
                {
                    var actualTorrent = actual.Arguments.Torrents[i];
                    var expectedTorrent = deserialized.Arguments.Torrents[i];
                    Assert.That(actualTorrent.DownloadDir, Is.EqualTo(expectedTorrent.DownloadDir));
                    Assert.That(actualTorrent.HashString, Is.EqualTo(expectedTorrent.HashString));
                    Assert.That(actualTorrent.Name, Is.EqualTo(expectedTorrent.Name));
                    Assert.That(actualTorrent.PercentDone, Is.EqualTo(expectedTorrent.PercentDone));
                    Assert.That(actualTorrent.SizeWhenDone, Is.EqualTo(expectedTorrent.SizeWhenDone));
                }
            }
        }
    }

    private static void AssertTransmissionTorrentAddResponse(TransmissionTorrentAddResponse actual, string expected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);

        var deserialized = JsonSerializer.Deserialize(
            expected,
            TransmissionJsonSerializerContext.Default.TransmissionTorrentAddResponse);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(actual, Is.Not.Null);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual.Result, Is.EqualTo(deserialized.Result));
            Assert.That(actual.Arguments is null, Is.EqualTo(deserialized.Arguments is null));
            if (actual.Arguments is not null && deserialized.Arguments is not null)
            {
                AssertTransmissionTorrentAddResponseItem(
                    actual.Arguments.TorrentAdded,
                    deserialized.Arguments.TorrentAdded);

                AssertTransmissionTorrentAddResponseItem(
                    actual.Arguments.TorrentDuplicate,
                    deserialized.Arguments.TorrentDuplicate);
            }
        }

        static void AssertTransmissionTorrentAddResponseItem(
            TransmissionTorrentAddResponseItem? actual,
            TransmissionTorrentAddResponseItem? expected)
        {
            Assert.That(actual is null, Is.EqualTo(expected is null));
            if (actual is not null && expected is not null)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(actual.HashString, Is.EqualTo(expected.HashString));
                    Assert.That(actual.Name, Is.EqualTo(expected.Name));
                }
            }
        }
    }

    private static void AssertTransmissionTorrentRemoveResponse(
        TransmissionTorrentRemoveResponse actual,
        string expected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);

        var deserialized = JsonSerializer.Deserialize(
            expected,
            TransmissionJsonSerializerContext.Default.TransmissionTorrentRemoveResponse);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(actual, Is.Not.Null);
        }

        Assert.That(actual.Result, Is.EqualTo(deserialized.Result));
    }

    private static HttpResponseMessage Answer(string body, string contentType)
    {
        var content = new StringContent(body) { Headers = { ContentType = MediaTypeHeaderValue.Parse(contentType) } };

        return new(HttpStatusCode.OK) { Content = content };
    }

    private static HttpResponseMessage Answer(byte[] body) =>
        new(HttpStatusCode.OK) { Content = new StreamContent(new NonSeekableStream(new MemoryStream(body))) };

    private static async Task<TransmissionTorrentGetResponse> GetTorrentsAnsweredByAsync(HttpMessageHandler handler)
    {
        using var httpClient = new HttpClient(handler) { BaseAddress = new(_options.CurrentValue.BaseAddress) };

        return await new TransmissionClient(_options, httpClient).GetTorrentsAsync().ConfigureAwait(false);
    }

    private static IEnumerable<TestCaseData<byte[], string>> UnreadableBodyTestCases()
    {
        const string test = nameof(GetTorrentsAsync_WhenBodyIsNotAResponse_QuotesItDecodedAsUtf8);

        yield return new(Encoding.UTF8.GetBytes("null"), "null") { TestName = $"{test}(JSON null)" };

        yield return new([], string.Empty) { TestName = $"{test}(empty)" };

        yield return new([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("<p>x</p>")], "<p>x</p>")
        { TestName = $"{test}(UTF-8 BOM)" };

        yield return new([.. Encoding.UTF8.GetBytes("<p>caf"), 0xE9, .. Encoding.UTF8.GetBytes("</p>")], "<p>caf\uFFFD</p>")
        { TestName = $"{test}(Latin-1 byte)" };

        yield return new(
            [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("""{"result":"success"}""")],
            "\uFFFD\uFFFD{_\"_r_e_s_u_l_t_\"_:_\"_s_u_c_c_e_s_s_\"_}_")
        { TestName = $"{test}(UTF-16 JSON)" };

        yield return new(Encoding.UTF8.GetBytes(new string('x', 79) + "\U0001F600tail"), new string('x', 79) + "...")
        { TestName = $"{test}(surrogate pair at the cut)" };
    }

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
