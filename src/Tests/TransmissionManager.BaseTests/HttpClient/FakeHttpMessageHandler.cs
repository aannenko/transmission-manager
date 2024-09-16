using System.Net;
using System.Net.Http.Headers;

namespace TransmissionManager.BaseTests.HttpClient;

public sealed class FakeHttpMessageHandler(IReadOnlyDictionary<TestRequest, TestResponse> requestToResponseMap)
    : HttpMessageHandler
{
    public FakeHttpMessageHandler(TestRequest testRequest, TestResponse testResponse)
        : this(new Dictionary<TestRequest, TestResponse> { [testRequest] = testResponse })
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        TestRequest testRequest;
        if (requestToResponseMap.TryGetValue(testRequest = request.ToTestRequest(), out var testResponse) &&
            testRequest.Method == request.Method &&
            testRequest.RequestUri == request.RequestUri &&
            AreHeadersEqual(testRequest.Headers, request.Headers) &&
            await IsContentEqualAsync(testRequest.Content, request.Content))
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

        return new HttpResponseMessage { StatusCode = (HttpStatusCode)418 };
    }

    private static bool AreHeadersEqual(Dictionary<string, string>? expected, HttpRequestHeaders actual)
    {
        var actualHeadersCount = 0;
        foreach (var (name, values) in actual)
        {
            actualHeadersCount++;
            var value = values.SingleOrDefault();
            if (value is null ||
                !(expected?.TryGetValue(name, out var expectedValue) ?? false) ||
                expectedValue != value)
            {
                return false;
            }
        }

        return (expected?.Count ?? 0) == actualHeadersCount;
    }

    private static async Task<bool> IsContentEqualAsync(string? expected, HttpContent? actual)
    {
        if (expected is null && actual is null)
            return true;

        if (expected is null || actual is null)
            return false;

        return expected == await actual.ReadAsStringAsync();
    }
}
