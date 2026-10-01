namespace TransmissionManager.BaseTests.HttpClient;

public sealed class ThrowingHttpMessageHandler(string message) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        throw new HttpRequestException(message);
    }
}
