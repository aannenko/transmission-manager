using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.AppInfo;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Serialization;

namespace TransmissionManager.Web.Services;

internal sealed class TransmissionManagerClient(HttpClient httpClient)
{
    public async Task<GetAppInfoResponse> GetAppInfoAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(EndpointAddresses.AppInfo, UriKind.Relative);
        var appInfo = await httpClient
            .GetFromJsonAsync(requestUri, DtoJsonSerializerContext.Default.GetAppInfoResponse, cancellationToken)
            .ConfigureAwait(false);

        return appInfo == default
            ? throw new HttpRequestException("Failed to retrieve app info.")
            : appInfo;
    }

    public async Task<GetTorrentPageResponse> GetTorrentPageAsync(
        GetTorrentPageParameters request = default,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(request.ToPathAndQueryString(), UriKind.Relative);
        var torrentPage = await httpClient
            .GetFromJsonAsync(requestUri, DtoJsonSerializerContext.Default.GetTorrentPageResponse, cancellationToken)
            .ConfigureAwait(false);

        return torrentPage ?? throw new HttpRequestException("Failed to retrieve torrent page.");
    }

    public async Task<AddTorrentResponse> AddTorrentAsync(
        AddTorrentRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(EndpointAddresses.Torrents, UriKind.Relative);
        var response = await httpClient
            .PostAsJsonAsync(requestUri, request, cancellationToken)
            .ConfigureAwait(false);

        return await response
            .EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<AddTorrentResponse>(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<RefreshTorrentByIdResponse> RefreshTorrentByIdAsync(
        long torrentId,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"{EndpointAddresses.Torrents}/{torrentId}", UriKind.Relative);
        using var response = await httpClient
            .PostAsJsonAsync(requestUri, string.Empty, cancellationToken)
            .ConfigureAwait(false);

        return await response
            .EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<RefreshTorrentByIdResponse>(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteTorrentByIdAsync(
        long torrentId,
        DeleteTorrentByIdType deleteType,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"{EndpointAddresses.Torrents}/{torrentId}?deleteType={deleteType}", UriKind.Relative);
        using var response = await httpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
