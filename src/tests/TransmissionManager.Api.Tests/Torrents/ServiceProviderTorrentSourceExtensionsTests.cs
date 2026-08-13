using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TransmissionManager.Api.Actions.Torrents;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.BaseTests.Options;
using TransmissionManager.Database.Dto;
using TransmissionManager.TorrentSources.Dto;
using TransmissionManager.TorrentSources.JsonPointer;
using TransmissionManager.TorrentSources.WebPage;

namespace TransmissionManager.Api.Tests.Torrents;

[Parallelizable(ParallelScope.Self)]
internal sealed class ServiceProviderTorrentSourceExtensionsTests
{
    private const string _hash = "0bda511316a069e86dd8ee8a3610475d2013a7fa";
    private const string _sourceAddress = "https://torrenttracker.com/forum/viewtopic.php?t=1";
    private const string _pointer = "#/a/b";

    private const string _webPageBody =
        $"""<html><body><a href="magnet:?xt=urn:btih:{_hash}&dn=Web+Page">m</a></body></html>""";

    private const string _jsonBody = $$$"""{"a":{"b":"{{{_hash}}}"}}""";

    private static readonly Uri _sourceUri = new(_sourceAddress);

    /// <remarks>
    /// Both clients are given the same address and both would succeed, so the magnet identifies
    /// which one ran: the web page client returns the page's raw match, carrying <c>dn=</c>, while
    /// the JSON Pointer client synthesises a bare magnet from the value at the pointer.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenKindIsWebPage_ResolvesTheWebPageClient()
    {
        var (result, magnetUri, _) = await FindAsync(TorrentSourceKind.WebPage, null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(magnetUri!.OriginalString, Does.Contain("dn=Web+Page"));
        }
    }

    [Test]
    public async Task FindMagnetUriAsync_WhenKindIsJsonPointer_ResolvesTheJsonPointerClient()
    {
        var (result, magnetUri, _) = await FindAsync(TorrentSourceKind.JsonPointer, null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(magnetUri, Is.EqualTo(new Uri($"magnet:?xt=urn:btih:{_hash}")));
        }
    }

    /// <remarks>
    /// A torrent keeps its magnet regex across a change of kind, so a JSON Pointer source can carry
    /// one that would match the page. Supplying it must not change the outcome.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenKindIsJsonPointerAndARegexIsSupplied_DoesNotPassItOn()
    {
        var (result, magnetUri, _) = await FindAsync(TorrentSourceKind.JsonPointer, @"magnet:\?xt=[^""]+")
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(magnetUri, Is.EqualTo(new Uri($"magnet:?xt=urn:btih:{_hash}")));
        }
    }

    /// <remarks>
    /// Request validation rejects undefined values before they reach here, so this guards the
    /// remaining path: a member added to the enum without a branch must fault loudly rather than
    /// resolve whichever client happens to be listed first.
    /// </remarks>
    [Test]
    public void FindMagnetUriAsync_WhenKindIsNotADefinedMember_Throws() =>
        Assert.That(
            async () => await FindAsync((TorrentSourceKind)999, null).ConfigureAwait(false),
            Throws.TypeOf<ArgumentOutOfRangeException>());

    /// <remarks>
    /// Resolution is lazy, so the client a search does not need is never constructed - which is the
    /// reason this dispatches through the provider instead of injecting both clients.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenKindIsJsonPointer_DoesNotResolveTheWebPageClient()
    {
        using var jsonHandler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _sourceUri),
            new(HttpStatusCode.OK, Content: _jsonBody));

        using var jsonHttpClient = new HttpClient(jsonHandler);

        var services = new ServiceCollection();
        _ = services.AddSingleton(new TorrentJsonPointerClient(JsonPointerOptions(), jsonHttpClient));

        // TorrentWebPageClient is deliberately absent: resolving it would throw.
        using var provider = services.BuildServiceProvider();

        var (result, _, _) = await provider
            .FindMagnetUriAsync(new($"{_sourceAddress}{_pointer}"), TorrentSourceKind.JsonPointer, null)
            .ConfigureAwait(false);

        Assert.That(result, Is.EqualTo(MagnetSearchResult.Found));
    }

    private static async Task<MagnetSearchOutcome> FindAsync(TorrentSourceKind sourceKind, string? regexPattern)
    {
        using var webPageHandler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _sourceUri),
            new(HttpStatusCode.OK, Content: _webPageBody));

        using var jsonHandler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _sourceUri),
            new(HttpStatusCode.OK, Content: _jsonBody));

        using var webPageHttpClient = new HttpClient(webPageHandler);
        using var jsonHttpClient = new HttpClient(jsonHandler);

        var services = new ServiceCollection();

        _ = services
            .AddSingleton(new TorrentWebPageClient(WebPageOptions(), webPageHttpClient))
            .AddSingleton(new TorrentJsonPointerClient(JsonPointerOptions(), jsonHttpClient));

        using var provider = services.BuildServiceProvider();

        return await provider
            .FindMagnetUriAsync(new($"{_sourceAddress}{_pointer}"), sourceKind, regexPattern)
            .ConfigureAwait(false);
    }

    private static FakeOptionsMonitor<TorrentWebPageClientOptions> WebPageOptions() =>
        new(new()
        {
            DefaultMagnetRegexPattern = @"magnet:\?xt=urn:btih:[^""]+",
            RegexMatchTimeout = TimeSpan.FromMilliseconds(100),
            MagnetSearchTimeout = TimeSpan.FromSeconds(30),
        });

    private static FakeOptionsMonitor<TorrentJsonPointerClientOptions> JsonPointerOptions() =>
        new(new()
        {
            MaxJsonTokenBytes = 4096,
            MagnetSearchTimeout = TimeSpan.FromSeconds(30),
        });
}
