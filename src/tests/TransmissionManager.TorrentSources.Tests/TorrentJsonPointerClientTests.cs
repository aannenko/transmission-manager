using System.Globalization;
using System.Net;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.BaseTests.Options;
using TransmissionManager.TorrentSources.Dto;
using TransmissionManager.TorrentSources.JsonPointer;

namespace TransmissionManager.TorrentSources.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class TorrentJsonPointerClientTests
{
    private const string _documentAddress = "https://torrentTracker.com/v1/static/pvc/f/1106";
    private const string _pointer = "#/result/6880555/2";
    private const int _maxJsonTokenBytes = 4096;
    private const string _upperCaseHash = "36B04E5B0123456789ABCDEF0123456789AB46FF";

    private const string _document = $$"""
        {
          "update_time": 1785441955,
          "result": {
            "6880554": [0, 12, "0000000000000000000000000000000000000000"],
            "6880555": [0, 50, "{{_upperCaseHash}}"],
            "6880556": [0, 7, null],
            "6880557": [0, 7, "not-a-hash"]
          }
        }
        """;

    private static readonly Uri _documentUri = new(_documentAddress);

    private static readonly Dictionary<TestRequest, TestResponse> _noExpectedRequests = [];

    [Test]
    public async Task FindMagnetUriAsync_WhenPointerAddressesAnInfoHash_ReturnsMagnetBuiltFromIt()
    {
        var outcome = await FindAsync(_pointer, _document).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            // The case the source used is kept, as the web page source keeps a page's.
            Assert.That(outcome.MagnetUri, Is.EqualTo(new Uri($"magnet:?xt=urn:btih:{_upperCaseHash}")));
            Assert.That(outcome.Error, Is.Null);
        }
    }

    [TestCase("#/result/9999999/2", TestName =
        "FindMagnetUriAsync_WhenDocumentHoldsNoValueAtThePointer_ReturnsNotFound(topic absent)")]
    [TestCase("#/result/6880555/9", TestName =
        "FindMagnetUriAsync_WhenDocumentHoldsNoValueAtThePointer_ReturnsNotFound(index out of range)")]
    public async Task FindMagnetUriAsync_WhenDocumentHoldsNoValueAtThePointer_ReturnsNotFound(string pointer)
    {
        var outcome = await FindAsync(pointer, _document).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.NotFound));
            Assert.That(outcome.Error, Is.Not.Empty);
        }
    }

    /// <remarks>
    /// An off-by-one index is the likeliest mistake in a hand-written pointer, and it lands on a
    /// neighbouring field rather than on nothing - so this must fault the pointer, not the source,
    /// and the message must name what was found there.
    /// <para>
    /// A <c>null</c> belongs here too. The pointer resolved, so the document is not silent about
    /// that element; whether the source will later publish a hash there is not something one read
    /// can know, so no guess is made either way.
    /// </para>
    /// </remarks>
    [TestCase("#/result/6880555/1", "number", TestName =
        "FindMagnetUriAsync_WhenPointerAddressesANonString_ReturnsInvalidSelector(a number)")]
    [TestCase("#/result/6880555", "array", TestName =
        "FindMagnetUriAsync_WhenPointerAddressesANonString_ReturnsInvalidSelector(an array)")]
    [TestCase("#/result", "object", TestName =
        "FindMagnetUriAsync_WhenPointerAddressesANonString_ReturnsInvalidSelector(an object)")]
    [TestCase("#/result/6880556/2", "null", TestName =
        "FindMagnetUriAsync_WhenPointerAddressesANonString_ReturnsInvalidSelector(a null)")]
    [TestCase("#/update_time", "number", TestName =
        "FindMagnetUriAsync_WhenPointerAddressesANonString_ReturnsInvalidSelector(a number at the root)")]
    public async Task FindMagnetUriAsync_WhenPointerAddressesANonString_ReturnsInvalidSelector(
        string pointer,
        string expectedInError)
    {
        var outcome = await FindAsync(pointer, _document).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
            Assert.That(outcome.Error, Does.Contain(expectedInError));
        }
    }

    [Test]
    public async Task FindMagnetUriAsync_WhenPointerAddressesAStringTheValuePatternRejects_ReturnsNotFound()
    {
        var outcome = await FindAsync("#/result/6880557/2", _document).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            // The source answered and the pointer resolved; only the value was not what was sought,
            // which is the same outcome the web page source reports for a page holding no magnet.
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.NotFound));
            Assert.That(outcome.Error, Is.Not.Empty);
        }
    }

    [TestCase("", TestName =
        "FindMagnetUriAsync_WhenPointerIsUnusable_ReturnsInvalidSelectorWithoutRequesting(no fragment)")]
    [TestCase("#result/1", TestName =
        "FindMagnetUriAsync_WhenPointerIsUnusable_ReturnsInvalidSelectorWithoutRequesting(no leading slash)")]
    [TestCase("#/~2", TestName =
        "FindMagnetUriAsync_WhenPointerIsUnusable_ReturnsInvalidSelectorWithoutRequesting(incomplete escape)")]
    public async Task FindMagnetUriAsync_WhenPointerIsUnusable_ReturnsInvalidSelectorWithoutRequesting(
        string pointer)
    {
        using var handler = new FakeHttpMessageHandler(_noExpectedRequests);
        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient)
            .FindMagnetUriAsync(new($"{_documentAddress}{pointer}"))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
            Assert.That(outcome.Error, Is.Not.Empty);
        }
    }

    [TestCase("/v1/static/pvc/f/1106#/result/1/2", UriKind.Relative)]
    [TestCase("ftp://torrentTracker.com/doc#/result/1/2", UriKind.Absolute)]
    [TestCase("file:///c:/doc.json#/result/1/2", UriKind.Absolute)]
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

    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    public async Task FindMagnetUriAsync_WhenResponseIsNotSuccessful_ReturnsRetrievalFailed(
        HttpStatusCode statusCode)
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _documentUri),
            new(statusCode));

        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient)
            .FindMagnetUriAsync(new($"{_documentAddress}{_pointer}"))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.RetrievalFailed));
            Assert.That(outcome.Error, Does.Contain(((int)statusCode).ToString(CultureInfo.InvariantCulture)));
        }
    }

    /// <remarks>
    /// A challenge page or an error page served with a successful status is the shape this covers:
    /// the response arrived, but it is not the document the pointer describes.
    /// </remarks>
    [TestCase("<html>Verify you are human</html>", TestName =
        "FindMagnetUriAsync_WhenResponseIsNotJson_ReturnsRetrievalFailed(html)")]
    [TestCase("", TestName = "FindMagnetUriAsync_WhenResponseIsNotJson_ReturnsRetrievalFailed(empty)")]
    [TestCase("{ \"result\": ", TestName =
        "FindMagnetUriAsync_WhenResponseIsNotJson_ReturnsRetrievalFailed(truncated)")]
    public async Task FindMagnetUriAsync_WhenResponseIsNotJson_ReturnsRetrievalFailed(string content)
    {
        var outcome = await FindAsync(_pointer, content).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.RetrievalFailed));
            Assert.That(outcome.Error, Is.Not.Empty);
        }
    }

    [Test]
    public async Task FindMagnetUriAsync_WhenPointerAddressesTheWholeDocument_ReturnsMagnet()
    {
        var outcome = await FindAsync("#", $"\"{_upperCaseHash}\"").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(outcome.MagnetUri, Is.EqualTo(new Uri($"magnet:?xt=urn:btih:{_upperCaseHash}")));
        }
    }

    [Test]
    public async Task FindMagnetUriAsync_WhenDocumentDoesNotExist_ReturnsRetrievalFailed()
    {
        var nonExistentAddress = new Uri("https://seemingly.valid.though.non.existent.address#/a");

        using var httpClient = new HttpClient();

        var outcome = await CreateClient(httpClient).FindMagnetUriAsync(nonExistentAddress).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.RetrievalFailed));
            Assert.That(outcome.MagnetUri, Is.Null);
            Assert.That(outcome.Error, Is.Not.Empty);
            Assert.That(outcome.Error, Does.Not.Contain("did not deliver")); // not a timed out body read
        }
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

        using var handler = new DelayedHeadersHttpMessageHandler(TimeSpan.FromSeconds(1), _document);
        using var httpClient = new HttpClient(handler);

        var outcome = await CreateClient(httpClient, responseReadTimeout)
            .FindMagnetUriAsync(new($"{_documentAddress}{_pointer}"))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(outcome.MagnetUri, Is.EqualTo(new Uri($"magnet:?xt=urn:btih:{_upperCaseHash}")));
        }
    }

    /// <remarks>
    /// This client reads until the pointer resolves and imposes no size limit of its own, so the
    /// budget is the only thing between it and a source that answers forever.
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
            .FindMagnetUriAsync(new($"{_documentAddress}{_pointer}"))
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
                .FindMagnetUriAsync(new($"{_documentAddress}{_pointer}"), cancellationToken: callerCts.Token)
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
            .FindMagnetUriAsync(new($"{_documentAddress}{_pointer}"))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.RetrievalFailed));
            Assert.That(outcome.Error, Does.Contain(FailingBodyHttpMessageHandler.ErrorMessage));
            Assert.That(outcome.Error, Does.Not.Contain("did not deliver")); // not a timed out body read
        }
    }

    /// <remarks>
    /// A segment this long could never be compared against a member name, because no name that
    /// long can be held either.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenPointerSegmentExceedsTheTokenLimit_ReturnsInvalidSelector()
    {
        var outcome = await FindAsync($"#/{new string('n', _maxJsonTokenBytes)}", _document).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
            Assert.That(outcome.MagnetUri, Is.Null);
            Assert.That(outcome.Error, Does.Contain(nameof(TorrentJsonPointerClientOptions.MaxJsonTokenBytes)));
        }
    }

    /// <remarks>
    /// A source failure rather than a bad pointer, even though the pointer never addresses that
    /// token.
    /// </remarks>
    [Test]
    public async Task FindMagnetUriAsync_WhenDocumentTokenExceedsTheLimit_ReturnsRetrievalFailed()
    {
        var document = $$"""
            { "junk": "{{new string('a', _maxJsonTokenBytes)}}", "result": { "6880555": [0, 0, "x"] } }
            """;

        var outcome = await FindAsync(_pointer, document).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.RetrievalFailed));
            Assert.That(outcome.MagnetUri, Is.Null);
        }
    }

    // A source whose JSON already holds whole magnet links: a pattern that captures the magnet, and
    // a format that asks for nothing but what it captured.
    [Test]
    public async Task FindMagnetUriAsync_WhenValueIsAWholeMagnet_ReturnsItUnchanged()
    {
        const string magnet = $"magnet:?xt=urn:btih:{_upperCaseHash}&dn=TV+Show&tr=http%3A%2F%2Ft.co%2Fa";

        var outcome = await FindWithOverridesAsync(
                DocumentHolding(magnet),
                @"magnet:\?.+",
                "{0}")
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(outcome.MagnetUri!.OriginalString, Is.EqualTo(magnet));
        }
    }

    // Empty is not a third state meaning "no extraction": the store writes an empty setting as
    // absent, so both have to land on the configured default or a search would disagree with the
    // one after it.
    [Test]
    public async Task FindMagnetUriAsync_WhenSettingsAreEmpty_UsesTheConfiguredDefaults()
    {
        var outcome = await FindWithOverridesAsync(_document, "", "").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(outcome.MagnetUri, Is.EqualTo(new Uri($"magnet:?xt=urn:btih:{_upperCaseHash}")));
        }
    }

    [Test]
    public async Task FindMagnetUriAsync_WhenValueEmbedsAMagnet_ExtractsItWithAPassThroughFormat()
    {
        const string magnet = $"magnet:?xt=urn:btih:{_upperCaseHash}";

        var outcome = await FindWithOverridesAsync(
                DocumentHolding($"Download here: {magnet} (seeders: 12)"),
                @"magnet:\?[^\s]+",
                "{0}")
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(outcome.MagnetUri!.OriginalString, Is.EqualTo(magnet));
        }
    }

    // The whole match is the value, so a pattern that has to look at surrounding text keeps that
    // text out of the match with a zero-width lookbehind rather than with a capturing group.
    [Test]
    public async Task FindMagnetUriAsync_WhenValueIsAPrefixedHash_ExtractsItWithALookbehind()
    {
        var outcome = await FindWithOverridesAsync(
                DocumentHolding($"btih:{_upperCaseHash}"),
                @"(?<=btih:)[a-fA-F0-9]{40}",
                "magnet:?xt=urn:btih:{0}&dn=name")
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(
                outcome.MagnetUri!.OriginalString,
                Is.EqualTo($"magnet:?xt=urn:btih:{_upperCaseHash}&dn=name"));
        }
    }

    // The other side of the same rule: without a lookbehind the prefix is part of the match, and the
    // format has to account for it. Pins that no capture is consulted.
    [Test]
    public async Task FindMagnetUriAsync_WhenPatternMatchesMoreThanTheHash_UsesTheWholeMatch()
    {
        var outcome = await FindWithOverridesAsync(
                DocumentHolding($"btih:{_upperCaseHash}"),
                @"btih:[a-fA-F0-9]{40}",
                "magnet:?xt=urn:{0}")
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(outcome.MagnetUri!.OriginalString, Is.EqualTo($"magnet:?xt=urn:btih:{_upperCaseHash}"));
        }
    }

    // A v2 source: the format is the only thing that decides which URN a magnet claims.
    [Test]
    public async Task FindMagnetUriAsync_WhenFormatBuildsAVersionTwoMagnet_ReturnsIt()
    {
        const string v2Hash = "caf1e1c30e81cb361b8f0d7a5c9e2f4a6b3d5c7e91a2b3c4d5e6f708192a3b4c";

        var outcome = await FindWithOverridesAsync(
                DocumentHolding(v2Hash),
                @"[a-f0-9]{64}",
                "magnet:?xt=urn:btmh:1220{0}")
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(outcome.MagnetUri!.OriginalString, Is.EqualTo($"magnet:?xt=urn:btmh:1220{v2Hash}"));
        }
    }

    [TestCase(@"(?<value>[a-fA-F0-9]{40}", TestName =
        "FindMagnetUriAsync_WhenValuePatternIsUnusable_ReturnsInvalidSelector(does not compile)")]
    public async Task FindMagnetUriAsync_WhenValuePatternIsUnusable_ReturnsInvalidSelector(string pattern)
    {
        var outcome = await FindWithOverridesAsync(_document, pattern, null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
            Assert.That(outcome.Error, Is.Not.Empty);
        }
    }

    [TestCase("magnet:?xt=urn:btih:", TestName =
        "FindMagnetUriAsync_WhenFormatIsUnusable_ReturnsInvalidSelector(no placeholder)")]
    [TestCase("magnet:?xt=urn:btih:{0}&y={bad}", TestName =
        "FindMagnetUriAsync_WhenFormatIsUnusable_ReturnsInvalidSelector(stray brace)")]
    public async Task FindMagnetUriAsync_WhenFormatIsUnusable_ReturnsInvalidSelector(string format)
    {
        var outcome = await FindWithOverridesAsync(_document, null, format).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
            Assert.That(outcome.Error, Is.Not.Empty);
        }
    }

    // A pattern whose quantifiers are all optional matches an empty string wherever it is applied,
    // and the magnet that would build - one with nothing where the hash belongs - is a well-formed
    // absolute URI, so the check on the built value does not catch it.
    [Test]
    public async Task FindMagnetUriAsync_WhenThePatternMatchesNothing_ReturnsNotFound()
    {
        var outcome = await FindWithOverridesAsync(_document, "x*", null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.NotFound));
            Assert.That(outcome.Error, Is.Not.Empty);
        }
    }

    // Nothing constrains a tracker's magnet dialect, so what the format built is only checkable once
    // it exists.
    [Test]
    public async Task FindMagnetUriAsync_WhenTheBuiltValueIsNotAMagnet_ReturnsInvalidSelector()
    {
        var outcome = await FindWithOverridesAsync(_document, null, "https://example.com/{0}")
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
            Assert.That(outcome.Error, Does.Contain("is not a magnet link"));
        }
    }

    private static async Task<MagnetSearchOutcome> FindAsync(string pointer, string content)
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _documentUri),
            new(HttpStatusCode.OK, Content: content));

        using var httpClient = new HttpClient(handler);

        return await CreateClient(httpClient)
            .FindMagnetUriAsync(new($"{_documentAddress}{pointer}"))
            .ConfigureAwait(false);
    }

    private static async Task<MagnetSearchOutcome> FindWithOverridesAsync(
        string document,
        string? valueRegexPattern,
        string? valueFormat)
    {
        using var handler = new FakeHttpMessageHandler(
            new(HttpMethod.Get, _documentUri),
            new(HttpStatusCode.OK, Content: document));

        using var httpClient = new HttpClient(handler);

        return await CreateClient(httpClient)
            .FindMagnetUriAsync(new($"{_documentAddress}{_pointer}"), valueRegexPattern, valueFormat)
            .ConfigureAwait(false);
    }

    private static string DocumentHolding(string value) => $$"""
        { "result": { "6880555": [0, 0, {{System.Text.Json.JsonSerializer.Serialize(value)}}] } }
        """;

    private static TorrentJsonPointerClient CreateClient(HttpClient httpClient, TimeSpan? responseReadTimeout = null) =>
        new(
            new FakeOptionsMonitor<TorrentJsonPointerClientOptions>(new()
            {
                ResponseReadTimeout = responseReadTimeout ?? TimeSpan.FromSeconds(30),
                RegexMatchTimeout = TimeSpan.FromMilliseconds(100),
                MaxJsonTokenBytes = _maxJsonTokenBytes,
                DefaultJsonValueRegexPattern = @"[a-fA-F0-9]{40}",
                DefaultJsonValueFormat = "magnet:?xt=urn:btih:{0}",
            }),
            httpClient);
}
