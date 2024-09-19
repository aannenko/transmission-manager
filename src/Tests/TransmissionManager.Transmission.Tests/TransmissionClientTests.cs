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
public sealed class TransmissionClientTests
{
    private const string _transmissionRpcUri = "http://transmission:9091/transmission/rpc";

    const string _twoTorrentsAllFieldsResponse = """
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

    const string _twoTorrentsWithNoFieldsResponse = """{"arguments":{"torrents":[{},{}]},"result":"success"}""";

    private static readonly FakeOptionsMonitor<TransmissionClientOptions> _options = new(new()
    {
        BaseAddress = "http://transmission:9091",
        RpcEndpointAddressSuffix = "/transmission/rpc"
    });

    [Test]
    public async Task GetTorrentsAsync_GetsAllTorrentsWithAllFields_WhenNoArgumentsProvided()
    {
        const string expectedRequest =
            """{"method":"torrent-get","arguments":{"fields":["hashString","name","sizeWhenDone","percentDone","downloadDir"]}}""";

        var client = CreateClient(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: _twoTorrentsAllFieldsResponse));

        var response = await client.GetTorrentsAsync();

        AssertUponTransmissionTorrentGetResponse(response, _twoTorrentsAllFieldsResponse);
    }

    [Test]
    public async Task GetTorrentsAsync_GetsTwoTorrentsWithAllFields_WhenTwoTorrentHashstringsProvided()
    {
        const string expectedRequest =
            """{"method":"torrent-get","arguments":{"fields":["hashString","name","sizeWhenDone","percentDone","downloadDir"],"ids":["0bda511316a069e86dd8ee8a3610475d2013a7fa","738c60cbd44f0e9457ba2afdad9e9231d76243fe"]}}""";

        var client = CreateClient(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: _twoTorrentsAllFieldsResponse));

        var response = await client.GetTorrentsAsync(
            [
                "0bda511316a069e86dd8ee8a3610475d2013a7fa",
                "738c60cbd44f0e9457ba2afdad9e9231d76243fe"
            ]);

        AssertUponTransmissionTorrentGetResponse(response, _twoTorrentsAllFieldsResponse);
    }

    [Test]
    public async Task GetTorrentsAsync_GetsAllTorrentsWithTwoFields_WhenTwoRequestedFieldsProvided()
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

        var client = CreateClient(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: twoTorrentsTwoFieldsResponse));

        var response = await client.GetTorrentsAsync(requestFields: [ TorrentFields.HashString, TorrentFields.Name ]);

        AssertUponTransmissionTorrentGetResponse(response, twoTorrentsTwoFieldsResponse);
    }

    [Test]
    public async Task GetTorrentsAsync_GetsAllTorrentsWithNoFields_WhenNonExistingRequestedFieldsProvided()
    {
        const string expectedRequest = """{"method":"torrent-get","arguments":{"fields":[998,999]}}""";

        var client = CreateClient(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: _twoTorrentsWithNoFieldsResponse));

        var response = await client.GetTorrentsAsync(requestFields: [(TorrentFields)998, (TorrentFields)999]);

        AssertUponTransmissionTorrentGetResponse(response, _twoTorrentsWithNoFieldsResponse);
    }

    [Test]
    public async Task GetTorrentsAsync_GetsAllTorrentsWithNoFields_WhenEmptyRequestedFieldsProvided()
    {
        const string expectedRequest = """{"method":"torrent-get","arguments":{"fields":[]}}""";

        var client = CreateClient(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: _twoTorrentsWithNoFieldsResponse));

        var response = await client.GetTorrentsAsync(requestFields: []);

        AssertUponTransmissionTorrentGetResponse(response, _twoTorrentsWithNoFieldsResponse);
    }

    [Test]
    public async Task GetTorrentsAsync_GetsNoTorrents_WhenNonExistingHashstringProvided()
    {
        const string expectedRequest =
            """{"method":"torrent-get","arguments":{"fields":[],"ids":["0bda511316a069e86dd8ee8a3610475d2013a7fb"]}}""";

        const string noTorrentsResponse = """{"arguments":{"torrents":[]},"result":"success"}""";

        var client = CreateClient(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: noTorrentsResponse));

        var response = await client.GetTorrentsAsync(["0bda511316a069e86dd8ee8a3610475d2013a7fb"], []);

        AssertUponTransmissionTorrentGetResponse(response, noTorrentsResponse);
    }

    [Test]
    public void GetTorrentsAsync_ThrowsTaskCanceledExceptions_WhenCanceledTokenProvided()
    {
        const string expectedRequest =
            """{"method":"torrent-get","arguments":{"fields":["hashString","name","sizeWhenDone","percentDone","downloadDir"]}}""";

        var client = CreateClient(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK));

        var task = client.GetTorrentsAsync(cancellationToken: new(true));

        Assert.That(async () => await task, Throws.TypeOf<TaskCanceledException>());
    }

    [Test]
    public async Task AddTorrentsAsync_ReturnsTorrentAdded_WhenNewMagnetAndDownloadDirProvided()
    {
        const string expectedRequest =
            """{"method":"torrent-add","arguments":{"filename":"magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2\u0026dn=Example%20Name","download-dir":"/tvshows"}}""";

        const string torrentAddedResponse = """
            {
                "arguments": {
                    "torrent-added": {
                        "hashString": "3A81AAA70E75439D332C146ABDE899E546356BE2",
                        "id": 1,
                        "name": "Example Name"
                    }
                },
                "result": "success"
            }
            """;

        var client = CreateClient(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: torrentAddedResponse));

        var response = await client.AddTorrentUsingMagnetUriAsync("magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2&dn=Example%20Name", "/tvshows");

        AssertUponTransmissionTorrentAddResponse(response, torrentAddedResponse);
    }

    [Test]
    public async Task AddTorrentsAsync_ReturnsTorrentDuplicate_WhenExistingMagnetProvided()
    {
        const string expectedRequest =
            """{"method":"torrent-add","arguments":{"filename":"magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2\u0026dn=Example%20Name","download-dir":"/tvshows"}}""";

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

        var client = CreateClient(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: torrentDuplicateResponse));

        var response = await client.AddTorrentUsingMagnetUriAsync("magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2&dn=Example%20Name", "/tvshows");

        AssertUponTransmissionTorrentAddResponse(response, torrentDuplicateResponse);
    }

    [Test]
    public void AddTorrentsAsync_ThrowsHttpRequestException_WhenInvalidMagnetProvided()
    {
        const string expectedRequest =
            """{"method":"torrent-add","arguments":{"filename":"magnet:?xt=urn:btih:INVALIDMAGNET","download-dir":"/tvshows"}}""";

        const string unrecognizedInfoResponse = """{"arguments":{},"result":"unrecognized info"}""";

        const string error = "Response from Transmission does not indicate success: 'unrecognized info'";

        var client = CreateClient(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: unrecognizedInfoResponse));

        var task = client.AddTorrentUsingMagnetUriAsync("magnet:?xt=urn:btih:INVALIDMAGNET", "/tvshows");

        Assert.That(async () => await task, Throws.TypeOf<HttpRequestException>().With.Message.EqualTo(error));
    }

    [Test]
    public void AddTorrentsAsync_ThrowsHttpRequestException_WhenInvalidDownloadDirProvided()
    {
        const string expectedRequest =
            """{"method":"torrent-add","arguments":{"filename":"magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2","download-dir":"^\u0026*("}}""";

        const string unrecognizedInfoResponse = """{"arguments":{},"result":"download directory path is not absolute"}""";

        const string error = "Response from Transmission does not indicate success: 'download directory path is not absolute'";

        var client = CreateClient(
            new(HttpMethod.Post, new(_transmissionRpcUri), Content: expectedRequest),
            new(HttpStatusCode.OK, Content: unrecognizedInfoResponse));

        var task = client.AddTorrentUsingMagnetUriAsync("magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2", "^&*(");

        Assert.That(async () => await task, Throws.TypeOf<HttpRequestException>().With.Message.EqualTo(error));
    }

    private static void AssertUponTransmissionTorrentGetResponse(TransmissionTorrentGetResponse actual, string expected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);

        var deserialized = JsonSerializer.Deserialize(
            expected,
            TransmissionJsonSerializerContext.Default.TransmissionTorrentGetResponse);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(actual, Is.Not.Null);
        });

        Assert.Multiple(() =>
        {
            Assert.That(actual.Result, Is.EqualTo(deserialized.Result));
            Assert.That(actual.Arguments is null, Is.EqualTo(deserialized.Arguments is null));
            Assert.That(actual.Arguments?.Torrents is null, Is.EqualTo(deserialized.Arguments?.Torrents is null));
        });

        if (deserialized.Arguments?.Torrents is not null)
        {
            Assert.That(actual.Arguments.Torrents, Has.Length.EqualTo(deserialized.Arguments.Torrents.Length));
            Assert.Multiple(() =>
            {
                for (int i = 0; i < actual.Arguments.Torrents.Length; i++)
                {
                    var actualTorrent = actual.Arguments.Torrents[i];
                    var expectedTorrent = deserialized.Arguments.Torrents[i];
                    Assert.That(actualTorrent.DownloadDir, Is.EqualTo(expectedTorrent.DownloadDir));
                    Assert.That(actualTorrent.HashString, Is.EqualTo(expectedTorrent.HashString));
                    Assert.That(actualTorrent.Name, Is.EqualTo(expectedTorrent.Name));
                    Assert.That(actualTorrent.PercentDone, Is.EqualTo(expectedTorrent.PercentDone));
                    Assert.That(actualTorrent.SizeWhenDone, Is.EqualTo(expectedTorrent.SizeWhenDone));
                }
            });
        }
    }

    private static void AssertUponTransmissionTorrentAddResponse(
        TransmissionTorrentAddResponse actual,
        string expected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);

        var deserialized = JsonSerializer.Deserialize(
            expected,
            TransmissionJsonSerializerContext.Default.TransmissionTorrentAddResponse);

        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(actual, Is.Not.Null);
        });

        Assert.Multiple(() =>
        {
            Assert.That(actual.Result, Is.EqualTo(deserialized.Result));
            Assert.That(actual.Arguments is null, Is.EqualTo(deserialized.Arguments is null));
            if (actual.Arguments is not null && deserialized.Arguments is not null)
            {
                AssertUponTransmissionTorrentAddResponseItem(
                    actual.Arguments.TorrentAdded,
                    deserialized.Arguments.TorrentAdded);

                AssertUponTransmissionTorrentAddResponseItem(
                    actual.Arguments.TorrentDuplicate,
                    deserialized.Arguments.TorrentDuplicate);
            }
        });

        static void AssertUponTransmissionTorrentAddResponseItem(
            TransmissionTorrentAddResponseItem? actual,
            TransmissionTorrentAddResponseItem? expected)
        {
            Assert.That(actual is null, Is.EqualTo(expected is null));
            if (actual is not null && expected is not null)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(actual.HashString, Is.EqualTo(expected.HashString));
                    Assert.That(actual.Name, Is.EqualTo(expected.Name));
                });
            }
        }
    }

    private static TransmissionClient CreateClient(TestRequest request, TestResponse response)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(request, response))
        {
            BaseAddress = new(_options.CurrentValue.BaseAddress)
        };

        return new(_options, httpClient);
    }
}