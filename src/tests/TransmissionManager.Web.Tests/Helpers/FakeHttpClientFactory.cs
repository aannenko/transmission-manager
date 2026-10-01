namespace TransmissionManager.Web.Tests.Helpers;

internal sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new(handler, disposeHandler: false);
    }
}
