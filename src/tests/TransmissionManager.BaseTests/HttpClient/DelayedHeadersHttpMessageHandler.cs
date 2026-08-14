using System.Net;

namespace TransmissionManager.BaseTests.HttpClient;

/// <summary>
/// Waits before answering with response headers, then delivers the body immediately.
/// </summary>
/// <param name="headersDelay">How long to wait before the response headers arrive.</param>
/// <param name="content">The body, delivered without further delay once the headers are out.</param>
/// <remarks>
/// The mirror image of <see cref="StallingBodyHttpMessageHandler"/>, and what tells an additive
/// response-read budget from an inclusive one: a client that arms its budget before the request
/// spends it waiting here, while one that arms it when the headers arrive does not.
/// </remarks>
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
