using System.Net;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.BaseTests.Options;
using TransmissionManager.TorrentWebPages.Options;
using TransmissionManager.TorrentWebPages.Services;

namespace TransmissionManager.TorrentWebPages.Tests;

[Parallelizable(ParallelScope.Self)]
public sealed class TorrentWebPageClientTests
{
    private const string _webPageAddress = "https://torrentTracker.com/forum/viewtopic.php?t=1234567";

    private static readonly Uri _webPageUri = new(_webPageAddress);

    private static readonly FakeOptionsMonitor<TorrentWebPageClientOptions> _options = new(new()
    {
        DefaultMagnetRegexPattern = @"magnet:\?[^""]*",
        RegexMatchTimeout = TimeSpan.FromMilliseconds(100),
    });

    [Test]
    public async Task FindMagnetUriAsync_FindsMagnetUri_IfGivenProperWebPage()
    {
        const string magnetUri = "magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2&dn=Example+Name";
        const string webPageContentWithMagnet = $"""
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
                <a href="{magnetUri}">Download via Magnet</a>
            </body>
            </html>
            """;

        var client = CreateClient(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: webPageContentWithMagnet));

        var result = await client.FindMagnetUriAsync(_webPageUri);

        Assert.That(result, Is.EqualTo(magnetUri));
    }

    [Test]
    public async Task FindMagnetUriAsync_FindsMagnetUri_IfGivenWebPageWithoutMagnet()
    {
        const string webPageContentWithoutMagnet = """
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

        var client = CreateClient(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: webPageContentWithoutMagnet));

        var result = await client.FindMagnetUriAsync(_webPageUri);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindMagnetUriAsync_ThrowsHttpRequestException_IfGivenNonExistentWebPage()
    {
        var client = CreateClient();

        Assert.That(
            async () => await client.FindMagnetUriAsync(new("https://seemingly.valid.though.non.existent.page")),
            Throws.TypeOf<HttpRequestException>());
    }

    private static TorrentWebPageClient CreateClient() =>
        new(_options, new HttpClient());

    private static TorrentWebPageClient CreateClient(TestRequest request, TestResponse response) =>
        new(_options, new HttpClient(new FakeHttpMessageHandler(request, response)));
}
