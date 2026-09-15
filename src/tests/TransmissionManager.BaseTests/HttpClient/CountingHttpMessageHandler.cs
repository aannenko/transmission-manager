namespace TransmissionManager.BaseTests.HttpClient;

/// <summary>
/// Counts the requests that reach the handler below it, so a test can tell how many round trips an
/// outer handler made out of one call.
/// </summary>
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
