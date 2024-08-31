using System.Net;

namespace TransmissionManager.Transmission.Services;

public sealed class TransmissionHeadersHandler(TransmissionHeadersProvider headersService)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var headerName = headersService.SessionHeaderName;

        if (!request.Headers.Contains(headerName))
            request.Headers.TryAddWithoutValidation(headerName, headersService.SessionHeaderValue);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        string? newHeaderValue;
        if (response.StatusCode is HttpStatusCode.Conflict &&
            response.Headers.TryGetValues(headerName, out var newHeaderValues) &&
            (newHeaderValue = newHeaderValues?.SingleOrDefault()) is not null)
        {
            headersService.SessionHeaderValue = newHeaderValue;

            request.Headers.Remove(headerName);
            request.Headers.TryAddWithoutValidation(headerName, newHeaderValue);

            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }
}
