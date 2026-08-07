using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.BaseTests.Options;
using TransmissionManager.TorrentSources.Dto;
using TransmissionManager.TorrentSources.WebPage;

namespace TransmissionManager.TorrentSources.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentWebPageClientTests
{
    private const string _webPageAddress = "https://torrentTracker.com/forum/viewtopic.php?t=1234567";
    private const string _magnetUri = "magnet:?xt=urn:btih:3A81AAA70E75439D332C146ABDE899E546356BE2&dn=Example+Name";

    private const string _pageWithMagnet = $"""
        <!DOCTYPE html>
        <html lang="en">
        <body>
            <a href="{_magnetUri}">Download via Magnet</a>
        </body>
        </html>
        """;

    private const string _pageWithoutMagnet = """
        <!DOCTYPE html>
        <html lang="en">
        <body>
            <p>No Magnet URI for you today :(</p>
        </body>
        </html>
        """;

    private static readonly Uri _webPageUri = new(_webPageAddress);

    // Catastrophic backtracking: passes the shape check, compiles, then times out on this page.
    private const string _catastrophicPattern = @"magnet:\?(x|xx)+$";

    private static readonly string _catastrophicPage = $"magnet:?{new string('x', 2000)}!";

    private static readonly Dictionary<TestRequest, TestResponse> _noExpectedRequests = [];

    private static TorrentWebPageClient CreateClient(HttpClient httpClient, TimeSpan? magnetSearchTimeout = null) =>
        new(
            new FakeOptionsMonitor<TorrentWebPageClientOptions>(new()
            {
                MagnetSearchTimeout = magnetSearchTimeout ?? TimeSpan.FromSeconds(30),
                DefaultMagnetRegexPattern = @"magnet:\?xt=urn:btih:[^""]+",
                RegexMatchTimeout = TimeSpan.FromMilliseconds(100),
            }),
            httpClient);

    [Test]
    public async Task FindMagnetUriAsync_WhenWebPageContainsMagnet_ReturnsFoundWithMagnetUri()
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: _pageWithMagnet));

        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient).FindMagnetUriAsync(_webPageUri).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(outcome.MagnetUri, Is.EqualTo(new Uri(_magnetUri)));
            Assert.That(outcome.Error, Is.Null);
        }
    }

    /// <remarks>
    /// The short-document cases are regression guards: the scan asks the reader to carry a fixed
    /// overlap into the next chunk, which used to throw whenever the whole document was shorter
    /// than that overlap.
    /// </remarks>
    [TestCase(_pageWithoutMagnet)]
    [TestCase("<html></html>")] // shorter than the scan's overlap
    [TestCase("")]
    public async Task FindMagnetUriAsync_WhenWebPageDoesNotContainMagnet_ReturnsNotFound(string content)
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: content));

        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient).FindMagnetUriAsync(_webPageUri).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.NotFound));
            Assert.That(outcome.MagnetUri, Is.Null);
        }
    }

    /// <remarks>
    /// A magnet past the scan's re-anchor threshold (512 bytes) in a page that fits in a single
    /// read: the scan re-anchors its window and tops the buffer up, but the stream is already
    /// exhausted, so the top-up brings in nothing. The window must survive that.
    /// </remarks>
    [TestCase(800)] // whole page arrives in one read, well under the buffer
    [TestCase(1792)] // exactly the point at which a fill stops without seeing end-of-stream
    [TestCase(1900)]
    [TestCase(2048)] // buffer capacity
    [TestCase(5000)] // several fills
    public async Task FindMagnetUriAsync_WhenMagnetIsFarIntoThePage_ReturnsFoundWithMagnetUri(int pageLength)
    {
        const string prefix = "<html><body>";
        var padding = new string('x', 600);
        var link = $"<a href=\"{_magnetUri}\">m</a>";
        var page = prefix + padding + link;

        Assert.That(page.IndexOf(_magnetUri, StringComparison.Ordinal), Is.GreaterThan(512));
        Assert.That(page, Has.Length.LessThanOrEqualTo(pageLength));

        page = page.PadRight(pageLength, 'y');

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: page));

        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient).FindMagnetUriAsync(_webPageUri).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(outcome.MagnetUri, Is.EqualTo(new Uri(_magnetUri)));
        }
    }

    /// <remarks>
    /// Anti-bot challenges are deliberately not distinguished from any other rejection - see
    /// <see cref="MagnetSearchResult.RetrievalFailed"/> - so a challenged response must be reported
    /// exactly like a plain one, on whatever status the challenge happens to use.
    /// </remarks>
    [TestCase(HttpStatusCode.Forbidden, null)]
    [TestCase(HttpStatusCode.Forbidden, "challenge")] // Cloudflare's Cf-Mitigated, ignored on purpose
    [TestCase(HttpStatusCode.ServiceUnavailable, "challenge")]
    [TestCase(HttpStatusCode.NotFound, null)]
    [TestCase(HttpStatusCode.InternalServerError, null)]
    public async Task FindMagnetUriAsync_WhenResponseIsNotSuccessful_ReturnsRetrievalFailed(
        HttpStatusCode statusCode,
        string? mitigatedHeaderValue)
    {
        var headers = mitigatedHeaderValue is null
            ? null
            : new Dictionary<string, string> { ["Cf-Mitigated"] = mitigatedHeaderValue };

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(statusCode, headers));

        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient).FindMagnetUriAsync(_webPageUri).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.RetrievalFailed));
            Assert.That(outcome.MagnetUri, Is.Null);
            Assert.That(outcome.Error, Does.Contain(((int)statusCode).ToString(CultureInfo.InvariantCulture)));
        }
    }

    [Test]
    public async Task FindMagnetUriAsync_WhenWebPageDoesNotExist_ReturnsRetrievalFailed()
    {
        var nonExistentAddress = new Uri("https://seemingly.valid.though.non.existent.address");

        using var httpClient = new HttpClient();

        var outcome = await CreateClient(httpClient).FindMagnetUriAsync(nonExistentAddress).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.RetrievalFailed));
            Assert.That(outcome.Error, Is.Not.Empty);
        }
    }

    /// <remarks>
    /// <c>magnet:\?xt=(</c> satisfies the shape check that both this client and the API's
    /// <c>[MagnetRegex]</c> attribute apply, yet throws when compiled - so an uncompilable pattern
    /// reaches the client no matter how thoroughly the request was validated.
    /// </remarks>
    [TestCase(@"magnet:\?xt=(")]
    [TestCase(@"magnet:\?xt=[")]
    [TestCase("not a magnet pattern at all")]
    public async Task FindMagnetUriAsync_WhenRegexPatternIsUnusable_ReturnsInvalidSelectorWithoutRequesting(
        string regexPattern)
    {
        using var handler = new FakeHttpMessageHandler(_noExpectedRequests);
        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient)
            .FindMagnetUriAsync(_webPageUri, regexPattern)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
            Assert.That(outcome.Error, Is.Not.Empty);
        }
    }

    /// <remarks>
    /// A selector that times out is only the caller's fault when the caller supplied it. A
    /// configured default that times out is this application's misconfiguration and must surface
    /// rather than be reported as invalid input - the same rule that applies to a default which
    /// fails to compile.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenCallerRegexTimesOut_ReturnsInvalidSelector()
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: _catastrophicPage));

        using var httpClient = new System.Net.Http.HttpClient(handler);

        var outcome = await CreateClient(httpClient)
            .FindMagnetUriAsync(_webPageUri, _catastrophicPattern)
            .ConfigureAwait(false);

        Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
    }

    [Test]
    public void FindMagnetUriAsync_WhenConfiguredDefaultRegexTimesOut_Throws()
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: _catastrophicPage));

        using var httpClient = new System.Net.Http.HttpClient(handler);
        var client = new TorrentWebPageClient(
            new FakeOptionsMonitor<TorrentWebPageClientOptions>(new()
            {
                MagnetSearchTimeout = TimeSpan.FromSeconds(30),
                DefaultMagnetRegexPattern = _catastrophicPattern,
                RegexMatchTimeout = TimeSpan.FromMilliseconds(10),
            }),
            httpClient);

        Assert.That(
            async () => await client.FindMagnetUriAsync(_webPageUri).ConfigureAwait(false),
            Throws.TypeOf<RegexMatchTimeoutException>());
    }

    [TestCase("/forum/viewtopic.php?t=1234567", UriKind.Relative)]
    [TestCase("ftp://torrentTracker.com/file", UriKind.Absolute)]
    [TestCase("file:///c:/torrents/page.html", UriKind.Absolute)]
    public async Task FindMagnetUriAsync_WhenUriIsNotFetchable_ReturnsInvalidSourceWithoutRequesting(
        string address,
        UriKind uriKind)
    {
        using var handler = new FakeHttpMessageHandler(_noExpectedRequests);
        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient)
            .FindMagnetUriAsync(new(address, uriKind))
            .ConfigureAwait(false);

        Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSource));
    }

    /// <remarks>
    /// Nothing else bounds this. The resilience pipeline's timeouts elapse once the response
    /// headers arrive, and it sets <c>HttpClient.Timeout</c> to <see cref="Timeout.InfiniteTimeSpan"/>,
    /// so a source that stalls its body used to block the caller forever.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenSourceStallsResponseBody_ReturnsRetrievalFailed()
    {
        var magnetSearchTimeout = TimeSpan.FromMilliseconds(200);

        using var handler = new StallingBodyHttpMessageHandler();
        using var httpClient = new HttpClient(handler);

        // Should the budget ever stop covering the body read, this search never completes. Bounding
        // the wait turns that regression into a failure instead of a test run that hangs.
        var outcome = await CreateClient(httpClient, magnetSearchTimeout)
            .FindMagnetUriAsync(_webPageUri)
            .WaitAsync(TimeSpan.FromSeconds(10))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.RetrievalFailed));
            Assert.That(outcome.Error, Does.Contain(magnetSearchTimeout.ToString()));
        }
    }

    /// <remarks>
    /// The search budget must not swallow the caller's cancellation - the budget here is long
    /// enough that only the caller's token can end the wait.
    /// </remarks>
    [Test]
    public void FindMagnetUriAsync_WhenCallerCancels_Throws()
    {
        using var handler = new StallingBodyHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        Assert.That(
            async () => await CreateClient(httpClient, TimeSpan.FromMinutes(1))
                .FindMagnetUriAsync(_webPageUri, cancellationToken: cancellationTokenSource.Token)
                .ConfigureAwait(false),
            Throws.InstanceOf<OperationCanceledException>());
    }

    /// <remarks>
    /// An aborted read does not have to surface as an <see cref="OperationCanceledException"/>. The
    /// caller still asked to stop, so the retrieval-failure clauses must not claim it as an outcome.
    /// </remarks>
    [Test]
    public void FindMagnetUriAsync_WhenCallerCancelsAndAbortSurfacesAsIoException_Throws()
    {
        using var handler = new StallingBodyHttpMessageHandler(abortAsIoException: true);
        using var httpClient = new HttpClient(handler);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        Assert.That(
            async () => await CreateClient(httpClient, TimeSpan.FromMinutes(1))
                .FindMagnetUriAsync(_webPageUri, cancellationToken: cancellationTokenSource.Token)
                .ConfigureAwait(false),
            Throws.InstanceOf<OperationCanceledException>());
    }
}
