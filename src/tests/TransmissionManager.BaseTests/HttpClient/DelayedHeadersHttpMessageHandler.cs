using System.Net;

namespace TransmissionManager.BaseTests.HttpClient;

public sealed class DelayedHeadersHttpMessageHandler(TimeSpan headersDelay, string content)
    : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(headersDelay, cancellationToken).ConfigureAwait(false);

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) };
    }
}
