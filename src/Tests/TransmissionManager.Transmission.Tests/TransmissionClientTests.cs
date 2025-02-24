﻿using System.Net;
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
    public async Task GetTorrentsAsync_WhenGivenNoArguments_GetsAllTorrentsWithAllFields()
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
    public async Task GetTorrentsAsync_WhenGivenTwoHashStrings_GetsTwoTorrentsWithAllFields()
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
    public async Task GetTorrentsAsync_WhenGivenTwoRequestedFields_GetsAllTorrentsWithTwoFields()
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
    public async Task GetTorrentsAsync_WhenGivenNonExistentRequestedFields_GetsAllTorrentsWithNoFields()
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
    public async Task GetTorrentsAsync_WhenGivenEmptyRequestedFields_GetsAllTorrentsWithNoFields()
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
    public async Task GetTorrentsAsync_WhenGivenNonExistentHashstring_GetsNoTorrents()
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
    public void GetTorrentsAsync_WhenGivenCanceledToken_ThrowsTaskCanceledExceptions()
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
            Throws.TypeOf<HttpRequestException>().With.InnerException.TypeOf<TaskCanceledException>());
    }

    [Test]
    public async Task AddTorrentsAsync_WhenGivenNewMagnetAndDownloadDirProvided_ReturnsTorrentAdded()
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
    public async Task AddTorrentsAsync_WhenGivenExistingMagnetProvided_ReturnsTorrentDuplicate()
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
    public void AddTorrentsAsync_WhenGivenInvalidMagnet_ThrowsHttpRequestException()
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
    public void AddTorrentsAsync_WhenGivenInvalidDownloadDir_ThrowsHttpRequestException()
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
    public async Task RemoveTorrentAsync_WhenGivenExistingHashStrings_RemovesTorrents()
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
    public async Task RemoveTorrentAsync_WhenGivenEmptyArrayOfHashStrings_ReturnsSuccess()
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
    public async Task RemoveTorrentAsync_WhenGivenNullHashStrings_ReturnsSuccess()
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
    public async Task RemoveTorrentAsync_WhenGivenInvalidHashString_ReturnsSuccess()
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
            Assert.That(actual.Result, Is.EqualTo(deserialized!.Result));
            Assert.That(actual.Arguments is null, Is.EqualTo(deserialized.Arguments is null));
            Assert.That(actual.Arguments?.Torrents is null, Is.EqualTo(deserialized.Arguments?.Torrents is null));
        }

        if (deserialized!.Arguments?.Torrents is not null)
        {
            Assert.That(actual.Arguments!.Torrents, Has.Count.EqualTo(deserialized.Arguments.Torrents.Count));
            using (Assert.EnterMultipleScope())
            {
                for (var i = 0; i < actual.Arguments.Torrents!.Count; i++)
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
            Assert.That(actual.Result, Is.EqualTo(deserialized!.Result));
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

        Assert.That(actual.Result, Is.EqualTo(deserialized!.Result));
    }
}
