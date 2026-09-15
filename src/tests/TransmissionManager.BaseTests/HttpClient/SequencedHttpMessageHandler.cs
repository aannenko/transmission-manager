namespace TransmissionManager.BaseTests.HttpClient;

/// <summary>
/// Answers each request with the next queued response, so a test can drive a caller that retries or
/// polls through a different answer every time, and count how many it asked for.
/// </summary>
public sealed class SequencedHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>> _responses = new();
    private int _callCount;

    public int CallCount => _callCount;

    public void Enqueue(Func<HttpRequestMessage, HttpResponseMessage> response) =>
        _responses.Enqueue(request => Task.FromResult(response(request)));

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _ = Interlocked.Increment(ref _callCount);

        if (_responses.Count == 0)
            throw new InvalidOperationException("No more queued responses.");

        var factory = _responses.Dequeue();

        return await factory(request).ConfigureAwait(false);
    }
}
