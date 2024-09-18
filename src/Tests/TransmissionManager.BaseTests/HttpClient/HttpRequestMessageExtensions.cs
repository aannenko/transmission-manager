namespace TransmissionManager.BaseTests.HttpClient;

internal static class HttpRequestMessageExtensions
{
    public static async Task<TestRequest> ToTestRequestAsync(this HttpRequestMessage request)
    {
        return new(
            request.Method,
            request.RequestUri,
            request.Headers.ToDictionary(static pair => pair.Key, static pair => pair.Value.Single()),
            request.Content is null ? null : await request.Content.ReadAsStringAsync().ConfigureAwait(false));
    }
}
