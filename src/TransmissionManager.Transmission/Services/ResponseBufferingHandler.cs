namespace TransmissionManager.Transmission.Services;

/// <remarks>
/// Bounds the body only when added after the resilience handler, where buffering it runs under each attempt's
/// timeout; left to <see cref="HttpClient"/>, the body is buffered after the whole pipeline has returned.
/// It buffers without a size limit, and <see cref="HttpClient"/> does not buffer a body twice, so
/// <see cref="HttpClient.MaxResponseContentBufferSize"/> has no effect on a client this handler serves.
/// </remarks>
internal sealed class ResponseBufferingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        try
        {
            await response.Content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            response.Dispose();
            throw;
        }

        return response;
    }
}
