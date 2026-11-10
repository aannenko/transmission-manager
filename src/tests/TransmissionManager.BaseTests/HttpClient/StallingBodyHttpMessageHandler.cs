using System.Net;

namespace TransmissionManager.BaseTests.HttpClient;

public sealed class StallingBodyHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new StallingStream()),
        });
    }
}
