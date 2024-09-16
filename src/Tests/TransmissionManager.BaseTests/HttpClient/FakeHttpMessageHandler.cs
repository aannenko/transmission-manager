using System.Net;

namespace TransmissionManager.BaseTests.HttpClient;

public sealed class FakeHttpMessageHandler(IReadOnlyDictionary<TestRequest, TestResponse> requestToResponseMap)
    : HttpMessageHandler
{
    public FakeHttpMessageHandler(TestRequest testRequest, TestResponse testResponse)
        : this(new Dictionary<TestRequest, TestResponse> { [testRequest] = testResponse })
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (requestToResponseMap.TryGetValue(request.ToTestRequest(), out var testResponse))
        {
            var response = new HttpResponseMessage
            {
                StatusCode = testResponse.StatusCode,
                Content = testResponse.Content is null ? null : new StringContent(testResponse.Content)
            };

            if (testResponse.Headers?.Count > 0)
                foreach (var (name, value) in testResponse.Headers)
                    response.Headers.TryAddWithoutValidation(name, value);

            return Task.FromResult(response);
        }

        return Task.FromResult(new HttpResponseMessage { StatusCode = (HttpStatusCode)418 });
    }
}
