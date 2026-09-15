using System.Net;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.BaseTests.Options;
using TransmissionManager.Transmission.Options;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Transmission.Tests;

[Parallelizable(ParallelScope.Self)]
internal sealed class SessionHeaderHandlerTests
{
    private const string _requestUri = "http://transmission:9091/transmission/rpc";
    private const string _sessionHeaderName = "X-Transmission-Session-Id";
    private const string _firstSessionHeaderValue = "FirstSessionHeaderValue";
    private const string _secondSessionHeaderValue = "SecondSessionHeaderValue";

    private static readonly FakeOptionsMonitor<SessionHeaderProviderOptions> _options = new(new()
    {
        SessionHeaderName = _sessionHeaderName
    });

    [Test]
    public async Task SendAsync_WhenConflictCarriesANewSessionHeader_RetriesWithItAndStoresIt()
    {
        var requestToResponseMap = new Dictionary<TestRequest, TestResponse>
        {
            [new(HttpMethod.Get, new(_requestUri), SessionHeader(string.Empty))] =
                new(HttpStatusCode.Conflict, SessionHeader(_firstSessionHeaderValue)),
            [new(HttpMethod.Get, new(_requestUri), SessionHeader(_firstSessionHeaderValue))] =
                new(HttpStatusCode.OK)
        };

        var provider = new SessionHeaderProvider(_options);
        using var request = new HttpRequestMessage(HttpMethod.Get, _requestUri);

        Assert.That(provider.SessionHeaderValue, Is.Empty);

        var (status, sendCount) = await SendThroughHandlerAsync(provider, requestToResponseMap, request)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(provider.SessionHeaderValue, Is.EqualTo(_firstSessionHeaderValue));
            Assert.That(sendCount, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task SendAsync_WhenConflictCarriesNoSessionHeader_ReturnsItWithoutRetrying()
    {
        var requestToResponseMap = new Dictionary<TestRequest, TestResponse>
        {
            [new(HttpMethod.Get, new(_requestUri), SessionHeader(string.Empty))] = new(HttpStatusCode.Conflict)
        };

        var provider = new SessionHeaderProvider(_options);
        using var request = new HttpRequestMessage(HttpMethod.Get, _requestUri);

        var (status, sendCount) = await SendThroughHandlerAsync(provider, requestToResponseMap, request)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(status, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(provider.SessionHeaderValue, Is.Empty);
            Assert.That(sendCount, Is.EqualTo(1));
        }
    }

    /// <remarks>
    /// Pins the single retry: a handler that re-sent on every conflict would answer a Transmission
    /// stuck on 409 with a request storm.
    /// </remarks>
    [Test]
    public async Task SendAsync_WhenTheRetryAlsoConflicts_ReturnsItAndKeepsTheFirstHeader()
    {
        var requestToResponseMap = new Dictionary<TestRequest, TestResponse>
        {
            [new(HttpMethod.Get, new(_requestUri), SessionHeader(string.Empty))] =
                new(HttpStatusCode.Conflict, SessionHeader(_firstSessionHeaderValue)),
            [new(HttpMethod.Get, new(_requestUri), SessionHeader(_firstSessionHeaderValue))] =
                new(HttpStatusCode.Conflict, SessionHeader(_secondSessionHeaderValue))
        };

        var provider = new SessionHeaderProvider(_options);
        using var request = new HttpRequestMessage(HttpMethod.Get, _requestUri);

        var (status, sendCount) = await SendThroughHandlerAsync(provider, requestToResponseMap, request)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(status, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(provider.SessionHeaderValue, Is.EqualTo(_firstSessionHeaderValue));
            Assert.That(sendCount, Is.EqualTo(2));
        }
    }

    /// <remarks>
    /// The retry re-sends the original <see cref="HttpRequestMessage"/>, so anything that consumed
    /// or replaced its content would put an empty body on the wire the second time.
    /// </remarks>
    [Test]
    public async Task SendAsync_WhenRetryingARequestWithABody_ResendsTheSameBody()
    {
        const string body = """{"method":"torrent-get"}""";

        var requestToResponseMap = new Dictionary<TestRequest, TestResponse>
        {
            [new(HttpMethod.Post, new(_requestUri), SessionHeader(string.Empty), body)] =
                new(HttpStatusCode.Conflict, SessionHeader(_firstSessionHeaderValue)),
            [new(HttpMethod.Post, new(_requestUri), SessionHeader(_firstSessionHeaderValue), body)] =
                new(HttpStatusCode.OK)
        };

        var provider = new SessionHeaderProvider(_options);
        using var content = new StringContent(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, _requestUri) { Content = content };

        var (status, sendCount) = await SendThroughHandlerAsync(provider, requestToResponseMap, request)
            .ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(sendCount, Is.EqualTo(2));
        }
    }

    private static Dictionary<string, string> SessionHeader(string value)
    {
        return new() { [_sessionHeaderName] = value };
    }

    private static async Task<(HttpStatusCode Status, int SendCount)> SendThroughHandlerAsync(
        SessionHeaderProvider provider,
        IReadOnlyDictionary<TestRequest, TestResponse> requestToResponseMap,
        HttpRequestMessage request)
    {
        using var fakeMessageHandler = new FakeHttpMessageHandler(requestToResponseMap);
        using var countingHandler = new CountingHttpMessageHandler { InnerHandler = fakeMessageHandler };
        using var sessionHandler = new SessionHeaderHandler(provider) { InnerHandler = countingHandler };
        using var client = new HttpClient(sessionHandler);
        using var response = await client.SendAsync(request).ConfigureAwait(false);

        return (response.StatusCode, countingHandler.SendCount);
    }
}
