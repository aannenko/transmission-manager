using System.Net;

namespace TransmissionManager.BaseTests.HttpClient;

public sealed class FakeHttpMessageHandler(IReadOnlyDictionary<TestRequest, TestResponse> requestToResponseMap)
    : HttpMessageHandler
{
    public FakeHttpMessageHandler(TestRequest testRequest, TestResponse testResponse)
        : this(new Dictionary<TestRequest, TestResponse> { [testRequest] = testResponse })
    {
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendInternal(request.ToTestRequestAsync().GetAwaiter().GetResult());
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return SendInternal(await request.ToTestRequestAsync().ConfigureAwait(false));
    }

    private HttpResponseMessage SendInternal(TestRequest testRequest)
    {
        if (requestToResponseMap.TryGetValue(testRequest, out var testResponse))
        {
            var response = new HttpResponseMessage
            {
                StatusCode = testResponse.StatusCode,
                Content = testResponse.Content is null ? null : new StringContent(testResponse.Content)
            };

            if (testResponse.Headers?.Count > 0)
                foreach (var (name, value) in testResponse.Headers)
                    response.Headers.TryAddWithoutValidation(name, value);

            return response;
        }

        // My hope here is that none of the faked endpoints are expected to return "418 I'm a teapot".
        // This is done in order not to occupy an exception type which could be asserted in the tests.
        return new() { StatusCode = (HttpStatusCode)418 };
    }
}
