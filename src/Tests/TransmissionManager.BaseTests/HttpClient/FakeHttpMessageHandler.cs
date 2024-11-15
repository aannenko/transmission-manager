namespace TransmissionManager.BaseTests.HttpClient;

public class FakeHttpMessageHandler(IReadOnlyDictionary<TestRequest, TestResponse> requestToResponseMap)
    : HttpMessageHandler
{
    public FakeHttpMessageHandler(TestRequest testRequest, TestResponse testResponse)
        : this(new Dictionary<TestRequest, TestResponse> { [testRequest] = testResponse })
    {
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendInternal(request.ToTestRequestAsync().GetAwaiter().GetResult());
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendInternal(await request.ToTestRequestAsync().ConfigureAwait(false));
    }

    private HttpResponseMessage SendInternal(TestRequest testRequest)
    {
        if (!requestToResponseMap.TryGetValue(testRequest, out var testResponse))
            throw new UnexpectedTestRequestException(testRequest);

        var response = new HttpResponseMessage(testResponse.StatusCode)
        {
            Content = testResponse.Content is null ? null : new StringContent(testResponse.Content)
        };

        if (testResponse.Headers?.Count > 0)
        {
            foreach (var (name, value) in testResponse.Headers)
                response.Headers.TryAddWithoutValidation(name, value);
        }

        return response;
    }
}
