namespace TransmissionManager.BaseTests.HttpClient;

public sealed class CountingHttpMessageHandler : DelegatingHandler
{
    private int _sendCount;

    public int SendCount => _sendCount;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _ = Interlocked.Increment(ref _sendCount);

        return base.SendAsync(request, cancellationToken);
    }
}
