namespace TransmissionManager.Web.Tests.Helpers;

/// <summary>
/// Hands out clients over one handler. The clients do not own it, so disposing one leaves the
/// handler usable for the next.
/// </summary>
internal sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new(handler, disposeHandler: false);
    }
}
