namespace TransmissionManager.BaseTests.HttpClient;

internal static class HttpRequestMessageExtensions
{
    public static TestRequest ToTestRequest(this HttpRequestMessage requestMessage)
    {
        return new TestRequest(
            requestMessage.Method,
            requestMessage.RequestUri,
            requestMessage.Headers.ToDictionary(static pair => pair.Key, static pair => pair.Value.Single()),
            requestMessage.Content?.ReadAsStringAsync().GetAwaiter().GetResult());
    }
}
