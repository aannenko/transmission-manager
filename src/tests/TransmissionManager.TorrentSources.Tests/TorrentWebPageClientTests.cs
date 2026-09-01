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

    private const string _pageWithMagnetAndTorrentLink = $"""
        <!DOCTYPE html>
        <html lang="en">
        <body>
            <a href="{_magnetUri}">Download via Magnet</a>
            <a href="https://torrentTracker.com/download/1234567.torrent">Download the torrent file</a>
        </body>
        </html>
        """;

    private const string _pageWithTorrentLinkThenMagnet = $"""
        <!DOCTYPE html>
        <html lang="en">
        <body>
            <a href="https://torrentTracker.com/download/1234567.torrent">Download the torrent file</a>
            <a href="{_magnetUri}">Download via Magnet</a>
        </body>
        </html>
        """;

    private static readonly Uri _webPageUri = new(_webPageAddress);

    // Catastrophic backtracking: passes the shape check, compiles, then times out on this page.
    private const string _catastrophicPattern = @"magnet:\?(x|xx)+$";

    private static readonly string _catastrophicPage = $"magnet:?{new string('x', 2000)}!";

    private static readonly Dictionary<TestRequest, TestResponse> _noExpectedRequests = [];

    private static TorrentWebPageClient CreateClient(HttpClient httpClient, TimeSpan? responseReadTimeout = null) =>
        new(
            new FakeOptionsMonitor<TorrentWebPageClientOptions>(new()
            {
                ResponseReadTimeout = responseReadTimeout ?? TimeSpan.FromSeconds(30),
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
    /// The shape check reads the pattern's own text, so one that contains <c>magnet:\?</c> and still
    /// matches something else passes it. Building a <see cref="Uri"/> out of whatever such a pattern
    /// matched used to throw <see cref="UriFormatException"/> and reach the caller as HTTP 500 -
    /// pulling the bare info hash out of a magnet link is the case a user runs into, because
    /// extracting just the hash is a natural thing to try.
    /// </remarks>
    [TestCase(@"(?<=magnet:\?xt=urn:btih:)[0-9A-Fa-f]{40}", _pageWithMagnet,
        "3A81AAA70E75439D332C146ABDE899E546356BE2",
        TestName = "FindMagnetUriAsync_WhenPatternMatchesSomethingOtherThanAMagnet_ReturnsInvalidSelector(bare info hash)")]
    [TestCase(@"magnet:\?zzz|https://\S+?\.torrent", _pageWithMagnetAndTorrentLink,
        "https://torrentTracker.com/download/1234567.torrent",
        TestName = "FindMagnetUriAsync_WhenPatternMatchesSomethingOtherThanAMagnet_ReturnsInvalidSelector(http torrent link)")]
    public async Task FindMagnetUriAsync_WhenPatternMatchesSomethingOtherThanAMagnet_ReturnsInvalidSelector(
        string regexPattern,
        string content,
        string expectedInError)
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: content));

        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient)
            .FindMagnetUriAsync(_webPageUri, regexPattern)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
            Assert.That(outcome.MagnetUri, Is.Null);
            // Quoting what the pattern really matched is the whole diagnostic - without it the
            // message cannot tell its author why a pattern they believed in produced nothing.
            Assert.That(outcome.Error, Does.Contain(expectedInError));
        }
    }

    /// <remarks>
    /// A transport failure quotes what the server sent too - a status line, a header name - so its
    /// message is the source's text and reaches the same log line as a magnet match does.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenTheTransportFailureMessageHoldsControlCharacters_SummarizesIt()
    {
        using var handler = new ThrowingHttpMessageHandler("boom\r\n\u001b[31mFORGED");
        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient).FindMagnetUriAsync(_webPageUri).ConfigureAwait(false);

        Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.RetrievalFailed));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Error!.Any(char.IsControl), Is.False);
            Assert.That(outcome.Error, Does.Contain("boom___[31mFORGED"));
        }
    }

    /// <remarks>
    /// A page is a third party's bytes, and a pattern whose match is not a magnet quotes them back.
    /// Left raw they reach a log line, where a newline forges a record and an escape sequence drives
    /// the operator's terminal - so this pins the whole path through the client rather than just the
    /// helper: control characters replaced, the quote truncated, and the length that of what was
    /// really matched rather than of the summary.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenTheMatchHoldsControlCharacters_QuotesItSummarized()
    {
        const string anchor = "<a\r\nclass=\"x\" href=\"";
        const string page = $"<div>\r\n{anchor}{_magnetUri}\">m</a>\r\n</div>";
        const string expectedMatch = $"{anchor}{_magnetUri}";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: page));

        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient)
            .FindMagnetUriAsync(_webPageUri, @"<a[^>]*href=""magnet:\?[^""]+")
            .ConfigureAwait(false);

        Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Error!.Any(char.IsControl), Is.False);
            Assert.That(outcome.Error, Does.Contain("<a__class="));
            Assert.That(outcome.Error, Does.Contain($"({expectedMatch.Length} characters)"));
            Assert.That(outcome.Error, Does.Contain("..."));
        }
    }

    /// <remarks>
    /// The first match wins, as it does for the JSON source. The window is anchored on the literal
    /// <c>magnet:?</c>, but the pattern runs over the whole of it, so a pattern can match text
    /// earlier than the magnet that brought the window into view. Selecting that earlier text is the
    /// pattern saying what it wants; a magnet further down does not make it right, and searching on
    /// for one would turn a pattern into a suggestion.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenAnEarlierMatchIsNotAMagnetButALaterOneIs_ReturnsInvalidSelector()
    {
        const string pattern = @"https://[^""]+|magnet:\?xt=urn:btih:[0-9A-Fa-f]{40}";

        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: _pageWithTorrentLinkThenMagnet));

        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient)
            .FindMagnetUriAsync(_webPageUri, pattern)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
            Assert.That(outcome.Error, Does.Contain("https://torrentTracker.com/download/1234567.torrent"));
        }
    }

    /// <remarks>
    /// A pattern whose quantifiers are all optional matches an empty string wherever it is first
    /// tried. That is not a magnet link, and it is not a reason to stop reading the page either.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenPatternMatchesEmptyString_ReturnsNotFound()
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: _pageWithMagnet));

        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient)
            .FindMagnetUriAsync(_webPageUri, @"(?:magnet:\?)?")
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.NotFound));
            Assert.That(outcome.MagnetUri, Is.Null);
        }
    }

    /// <remarks>
    /// An add request carries the pattern to this client unchanged, so an empty one has to mean what
    /// sending no pattern at all means - the JSON source has always read it that way. An update
    /// instead clears a stored pattern to NULL, so a refresh never delivers an empty one.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenPatternIsEmpty_FallsBackToTheConfiguredDefault()
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: _pageWithMagnet));

        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient)
            .FindMagnetUriAsync(_webPageUri, string.Empty)
            .ConfigureAwait(false);

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
            Assert.That(outcome.Error, Does.Not.Contain("did not deliver")); // not a timed out body read
        }
    }

    [TestCase("not a magnet pattern at all")]
    [TestCase("[0-9A-Fa-f]{40}")] // finds an info hash, but not the magnet link around it
    [TestCase(@"magnet:[?]xt=urn:btih:[0-9A-Fa-f]{40}")] // a question mark spelled another way
    public async Task FindMagnetUriAsync_WhenRegexPatternDoesNotLookForAMagnet_ReturnsInvalidSelectorWithoutRequesting(
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
    /// A supplied pattern is built at its first match, and a match is only attempted inside a window
    /// the literal <c>magnet:?</c> opened - so on a page holding none, a pattern that cannot be built
    /// is never built, and the page is reported as holding no magnet link rather than the pattern as
    /// broken. What is said therefore depends on what the page happens to hold. Deliberate: the API
    /// refuses an unbuildable pattern when one is added or updated, so only a row written before it
    /// did so can reach here.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenPatternDoesNotParseAndThePageHoldsNoMagnet_ReturnsNotFound()
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: _pageWithoutMagnet));

        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient)
            .FindMagnetUriAsync(_webPageUri, @"magnet:\?xt=(")
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.NotFound));
            Assert.That(outcome.Error, Does.Not.Contain("Not enough"));
        }
    }

    /// <remarks>
    /// Both of these satisfy the API's <c>[MagnetRegex]</c> shape check, yet throw when built - so a
    /// pattern that does not parse reaches the client no matter how thoroughly the request was
    /// validated. It is reported once the page has been fetched, because a supplied pattern is built
    /// at its first match rather than up front.
    /// </remarks>
    [TestCase(@"magnet:\?xt=(")]
    [TestCase(@"magnet:\?xt=[")]
    public async Task FindMagnetUriAsync_WhenRegexPatternDoesNotParse_ReturnsInvalidSelector(string regexPattern)
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: _pageWithMagnet));

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

    [Test]
    public async Task FindMagnetUriAsync_WhenCallerRegexTimesOut_ReturnsInvalidSelector()
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: _catastrophicPage));

        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient)
            .FindMagnetUriAsync(_webPageUri, _catastrophicPattern)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
            // The other half of what the configured-default case pins: this one must not send an
            // operator looking at deployment configuration for a pattern the torrent supplied.
            Assert.That(
                outcome.Error,
                Does.Not.Contain(nameof(TorrentWebPageClientOptions.DefaultMagnetRegexPattern)));
        }
    }

    /// <remarks>
    /// A default that fails to compile is caught at startup, so the application never runs with one;
    /// a default that merely times out cannot be found that way, because whether it does depends on
    /// the page it runs against. Throwing it would reach an interactive caller as HTTP 500 and, on
    /// the scheduled path, be swallowed by the scheduler - leaving a refresh that fails every cycle
    /// with nothing in this application's own logs. Both sources report it instead and name the
    /// pattern, which is what identifies the configured default as the culprit.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenConfiguredDefaultRegexTimesOut_ReturnsInvalidSelector()
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _webPageUri),
            new(HttpStatusCode.OK, Content: _catastrophicPage));

        using var httpClient = new HttpClient(handler);
        var client = new TorrentWebPageClient(
            new FakeOptionsMonitor<TorrentWebPageClientOptions>(new()
            {
                ResponseReadTimeout = TimeSpan.FromSeconds(30),
                DefaultMagnetRegexPattern = _catastrophicPattern,
                RegexMatchTimeout = TimeSpan.FromMilliseconds(10),
            }),
            httpClient);

        var outcome = await client.FindMagnetUriAsync(_webPageUri).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
            Assert.That(outcome.Error, Does.Contain(nameof(TorrentWebPageClientOptions.DefaultMagnetRegexPattern)));
        }
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
    /// Ensures the budget is additive to the resilience pipeline rather than inclusive of it: the
    /// headers here take longer than the whole read budget, so arming it before the request - as
    /// this client once did - fails the search that this one completes.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenResponseHeadersAreSlowerThanTheReadBudget_StillReturnsFound()
    {
        var responseReadTimeout = TimeSpan.FromMilliseconds(200);

        using var handler = new DelayedHeadersHttpMessageHandler(TimeSpan.FromSeconds(1), _pageWithMagnet);
        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient, responseReadTimeout)
            .FindMagnetUriAsync(_webPageUri)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(outcome.MagnetUri?.OriginalString, Is.EqualTo(_magnetUri));
        }
    }

    /// <remarks>
    /// Nothing else bounds this. The resilience pipeline's timeouts elapse once the response
    /// headers arrive, and it sets <c>HttpClient.Timeout</c> to <see cref="Timeout.InfiniteTimeSpan"/>,
    /// so a source that stalls its body used to block the caller forever.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenSourceStallsResponseBody_ReturnsRetrievalFailed()
    {
        var responseReadTimeout = TimeSpan.FromMilliseconds(200);

        using var handler = new StallingBodyHttpMessageHandler();
        using var httpClient = new HttpClient(handler);

        // Should the budget ever stop covering the body read, this search never completes. Bounding
        // the wait turns that regression into a failure instead of a test run that hangs.
        var outcome = await CreateClient(httpClient, responseReadTimeout)
            .FindMagnetUriAsync(_webPageUri)
            .WaitAsync(TimeSpan.FromSeconds(10))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.RetrievalFailed));
            Assert.That(outcome.Error, Does.Contain(responseReadTimeout.ToString()));
        }
    }

    /// <remarks>
    /// The read budget must not swallow the caller's cancellation - the budget here is long enough
    /// that only the caller's token can end the wait. That token stands for the caller giving up
    /// (an aborted HTTP request, a host shutting down), and the timer is only a way to trip it
    /// mid-read; bounding the wait fails a regression that ignores it instead of waiting the budget out.
    /// </remarks>
    [Test]
    public void FindMagnetUriAsync_WhenCallerCancels_Throws()
    {
        using var handler = new StallingBodyHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        using var callerCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        Assert.That(
            async () => await CreateClient(httpClient, TimeSpan.FromMinutes(1))
                .FindMagnetUriAsync(_webPageUri, cancellationToken: callerCts.Token)
                .WaitAsync(TimeSpan.FromSeconds(10))
                .ConfigureAwait(false),
            Throws.InstanceOf<OperationCanceledException>());
    }

    /// <remarks>
    /// A dropped connection is not an expired budget, and the budget here is far too long to have
    /// expired. Reporting one as the other would send the user chasing a timeout that never happened.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenBodyReadFails_ReturnsRetrievalFailedWithTransportMessage()
    {
        using var handler = new FailingBodyHttpMessageHandler();
        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient, TimeSpan.FromMinutes(1))
            .FindMagnetUriAsync(_webPageUri)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.RetrievalFailed));
            Assert.That(outcome.Error, Does.Contain(FailingBodyHttpMessageHandler.ErrorMessage));
            Assert.That(outcome.Error, Does.Not.Contain("did not deliver")); // not a timed out body read
        }
    }
}
