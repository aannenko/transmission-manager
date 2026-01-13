using System.Net;

namespace TransmissionManager.Transmission.Services;

public sealed class SessionHeaderHandler(SessionHeaderProvider headerProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var headerName = headerProvider.SessionHeaderName;

        if (!request.Headers.Contains(headerName))
            _ = request.Headers.TryAddWithoutValidation(headerName, headerProvider.SessionHeaderValue);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        string? newHeaderValue;
        if (response.StatusCode is HttpStatusCode.Conflict &&
            response.Headers.TryGetValues(headerName, out var newHeaderValues) &&
            (newHeaderValue = newHeaderValues?.FirstOrDefault()) is not null)
        {
            headerProvider.SessionHeaderValue = newHeaderValue;

            _ = request.Headers.Remove(headerName);
            _ = request.Headers.TryAddWithoutValidation(headerName, newHeaderValue);

            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }
}
