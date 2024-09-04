using System.Net;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.BaseTests.Options;
using TransmissionManager.TorrentTrackerClient.Options;
using TransmissionManager.TorrentTrackerClient.Services;

namespace TransmissionManager.TorrentTrackerClient.Tests;

[Parallelizable(ParallelScope.Self)]
public sealed class TorrentWebPageServiceTests
{
    private const string _webPageUri = "https://torrentTracker.com/forum/viewtopic.php?t=1234567";
    private const string _magnetUri = "magnet:?xt=urn:btih:EXAMPLEHASH&dn=Example+Name";
    private const string _webPageContentWithMagnet = $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Magnet Link Example</title>
        </head>
        <body>
            <h1>Magnet Link Example</h1>
            <p>Click the link below to open the Magnet URI:</p>
            <a href="{_magnetUri}">Download via Magnet</a>
        </body>
        </html>
        """;

    private const string _webPageContentWithoutMagnet = $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Magnet Link Example</title>
        </head>
        <body>
            <h1>Magnet Link Example</h1>
            <p>No Magnet URI for you today :(</p>
        </body>
        </html>
        """;

    private static readonly MockOptionsMonitor<TorrentWebPageServiceOptions> _options = new(new()
    {
        // language=regex
        DefaultRegexPattern = @"\""(?<magnet>magnet:\?.*?)\""",
        RegexMatchTimeoutMilliseconds = 100,
    });

    [Test]
    public async Task FindMagnetUriAsync_FindsMagnetUri_IfGivenProperWebPage()
    {
        var service = CreateService(
            new(HttpMethod.Get, new(_webPageUri)),
            new(HttpStatusCode.OK, Content: _webPageContentWithMagnet));

        var result = await service.FindMagnetUriAsync(_webPageUri);

        Assert.That(result, Is.EqualTo(_magnetUri));
    }

    [Test]
    public async Task FindMagnetUriAsync_FindsMagnetUri_IfGivenWebPageWithoutMagnet()
    {
        var service = CreateService(
            new(HttpMethod.Get, new(_webPageUri)),
            new(HttpStatusCode.OK, Content: _webPageContentWithoutMagnet));

        var result = await service.FindMagnetUriAsync(_webPageUri);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindMagnetUriAsync_ThrowsHttpRequestException_IfGivenNonExistentWebPage()
    {
        var service = CreateService();

        Assert.That(
            async () => await service.FindMagnetUriAsync("https://seemingly.valid.though.non.existent.page"),
            Throws.TypeOf<HttpRequestException>());
    }

    [Test]
    public void FindMagnetUriAsync_ThrowsInvalidOperationException_IfGivenMalformedUri()
    {
        var service = CreateService();

        Assert.That(
            async () => await service.FindMagnetUriAsync("Oops! Bad URI."),
            Throws.TypeOf<InvalidOperationException>());
    }

    private static TorrentWebPageService CreateService() =>
        new(_options, new HttpClient());

    private static TorrentWebPageService CreateService(TestRequest request, TestResponse response) =>
        new(_options, new HttpClient(new MockHttpMessageHandler(request, response)));
}