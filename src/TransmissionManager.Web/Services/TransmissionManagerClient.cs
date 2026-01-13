using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Serialization;

namespace TransmissionManager.Web.Services;

internal sealed class TransmissionManagerClient(HttpClient httpClient)
{
    public async Task<Version> GetAppVersionAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(EndpointAddresses.AppVersion, UriKind.Relative);
        var version = await httpClient
            .GetFromJsonAsync(requestUri, DtoJsonSerializerContext.Default.Version, cancellationToken)
            .ConfigureAwait(false);

        return version is null
            ? throw new HttpRequestException("Failed to retrieve app version.")
            : version;
    }

    public async Task<TorrentDto> GetTorrentById(long torrentId, CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"{EndpointAddresses.Torrents}/{torrentId}", UriKind.Relative);
        var torrent = await httpClient
            .GetFromJsonAsync(requestUri, DtoJsonSerializerContext.Default.TorrentDto, cancellationToken)
            .ConfigureAwait(false);

        return torrent ?? throw new HttpRequestException($"Failed to retrieve torrent with id {torrentId}.");
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

        var addTorrentResponse = await response
            .EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<AddTorrentResponse>(cancellationToken)
            .ConfigureAwait(false);

        return addTorrentResponse ?? throw new HttpRequestException("Failed to add torrent.");
    }

    public async Task<RefreshTorrentByIdResponse> RefreshTorrentByIdAsync(
        long torrentId,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"{EndpointAddresses.Torrents}/{torrentId}", UriKind.Relative);
        using var response = await httpClient
            .PostAsJsonAsync(requestUri, string.Empty, cancellationToken)
            .ConfigureAwait(false);

        var refreshResponse = await response
            .EnsureSuccessStatusCode()
            .Content.ReadFromJsonAsync<RefreshTorrentByIdResponse>(cancellationToken)
            .ConfigureAwait(false);

        return refreshResponse ?? throw new HttpRequestException($"Failed to refresh torrent with id {torrentId}.");
    }

    public async Task UpdateTorrentByIdAsync(
        long torrentId,
        UpdateTorrentByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"{EndpointAddresses.Torrents}/{torrentId}", UriKind.Relative);
        using var response = await httpClient
            .PatchAsJsonAsync(requestUri, request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();
    }

    public async Task DeleteTorrentByIdAsync(
        long torrentId,
        DeleteTorrentByIdType deleteType,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"{EndpointAddresses.Torrents}/{torrentId}?deleteType={deleteType}", UriKind.Relative);
        using var response = await httpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
    }
}
