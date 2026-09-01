namespace TransmissionManager.BaseTests.HttpClient;

/// <summary>
/// Fails the request with an <see cref="HttpRequestException"/> carrying the given message, as a
/// transport failure does when the text it quotes - a status line, a header name - came from the
/// remote server.
/// </summary>
public sealed class ThrowingHttpMessageHandler(string message) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        throw new HttpRequestException(message);
    }
}
