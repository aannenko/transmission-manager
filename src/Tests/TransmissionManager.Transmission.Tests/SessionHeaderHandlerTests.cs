using System.Net;
using TransmissionManager.BaseTests.HttpClient;
using TransmissionManager.BaseTests.Options;
using TransmissionManager.Transmission.Options;
using TransmissionManager.Transmission.Services;

namespace TransmissionManager.Transmission.Tests;

[Parallelizable(ParallelScope.Self)]
public sealed class SessionHeaderHandlerTests
{
    private const string _requestUri = "http://transmission:9091/transmission/rpc";
    private const string _sessionHeaderName = "X-Transmission-Session-Id";
    private const string _sessionHeaderValue = "TestSessionHeaderValue";

    private static readonly Dictionary<string, string> _emptyHeaders = new()
    {
        [_sessionHeaderName] = string.Empty
    };

    private static readonly Dictionary<string, string> _filledHeaders = new()
    {
        [_sessionHeaderName] = _sessionHeaderValue
    };

    private static readonly FakeOptionsMonitor<SessionHeaderProviderOptions> _options = new(new()
    {
        SessionHeaderName = _sessionHeaderName
    });

    [Test]
    public async Task SendAsync_HandlesSessionHeaderUpdate_WhenSessionHeaderIsInvalid()
    {
        var requestToResponseMap = new Dictionary<TestRequest, TestResponse>
        {
            [new(HttpMethod.Get, new(_requestUri), _emptyHeaders)] = new(HttpStatusCode.Conflict, _filledHeaders),
            [new(HttpMethod.Get, new(_requestUri), _filledHeaders)] = new(HttpStatusCode.OK)
        };

        var (client, provider) = CreateClientProviderPair(requestToResponseMap);

        Assert.That(provider.SessionHeaderValue, Is.Empty);

        var result = await client.GetAsync(_requestUri);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccessStatusCode);
            Assert.That(provider.SessionHeaderValue, Is.EqualTo(_sessionHeaderValue));
        });
    }

    private static (HttpClient client, SessionHeaderProvider provider) CreateClientProviderPair(
        IReadOnlyDictionary<TestRequest, TestResponse> requestToResponseMap)
    {
        var provider = new SessionHeaderProvider(_options);
        var fakeMessageHandler = new FakeHttpMessageHandler(requestToResponseMap);
        var sessionHandler = new SessionHeaderHandler(provider) { InnerHandler = fakeMessageHandler };
        return (new(sessionHandler), provider);
    }
}