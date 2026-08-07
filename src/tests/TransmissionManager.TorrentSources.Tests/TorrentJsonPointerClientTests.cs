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
    private const string _lowerCaseHash = "36b04e5b0123456789abcdef0123456789ab46ff";

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
    public async Task FindMagnetUriAsync_WhenPointerAddressesAnInfoHash_ReturnsLowerCasedMagnet()
    {
        var outcome = await FindAsync(_pointer, _document).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.Found));
            Assert.That(outcome.MagnetUri, Is.EqualTo(new Uri($"magnet:?xt=urn:btih:{_lowerCaseHash}")));
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
    public async Task FindMagnetUriAsync_WhenPointerAddressesAStringThatIsNotAHash_ReturnsInvalidSelector()
    {
        var outcome = await FindAsync("#/result/6880557/2", _document).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.InvalidSelector));
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
            Assert.That(outcome.MagnetUri, Is.EqualTo(new Uri($"magnet:?xt=urn:btih:{_lowerCaseHash}")));
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
        }
    }

    /// <remarks>
    /// This client reads until the pointer resolves and imposes no size limit of its own, so the
    /// budget is the only thing between it and a source that answers forever.
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
            .FindMagnetUriAsync(new($"{_documentAddress}{_pointer}"))
            .WaitAsync(TimeSpan.FromSeconds(10))
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Result, Is.EqualTo(MagnetSearchResult.RetrievalFailed));
            Assert.That(outcome.Error, Does.Contain(magnetSearchTimeout.ToString()));
        }
    }

    /// <remarks>
    /// The budget here is long enough that only the caller's token can end the wait. The second
    /// case covers an abort that does not surface as an <see cref="OperationCanceledException"/>,
    /// which the retrieval-failure clause would otherwise claim as an outcome.
    /// </remarks>
    [TestCase(false)]
    [TestCase(true)]
    public void FindMagnetUriAsync_WhenCallerCancels_Throws(bool abortAsIoException)
    {
        using var handler = new StallingBodyHttpMessageHandler(abortAsIoException);
        using var httpClient = new HttpClient(handler);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        Assert.That(
            async () => await CreateClient(httpClient, TimeSpan.FromMinutes(1))
                .FindMagnetUriAsync(new($"{_documentAddress}{_pointer}"), cancellationTokenSource.Token)
                .ConfigureAwait(false),
            Throws.InstanceOf<OperationCanceledException>());
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

    private static TorrentJsonPointerClient CreateClient(HttpClient httpClient, TimeSpan? magnetSearchTimeout = null) =>
        new(
            new FakeOptionsMonitor<TorrentJsonPointerClientOptions>(new()
            {
                MagnetSearchTimeout = magnetSearchTimeout ?? TimeSpan.FromSeconds(30),
                MaxJsonTokenBytes = _maxJsonTokenBytes,
            }),
            httpClient);
}
