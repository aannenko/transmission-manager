using System.Net.Http.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Serialization;
using TransmissionManager.Web.Dto;
using TransmissionManager.Web.Extensions;

namespace TransmissionManager.Web.Services;

/// <summary>
/// Calls the TransmissionManager API.
/// </summary>
/// <remarks>
/// Every method answers with a result rather than throwing for what the API said, the way the API's
/// own actions answer with an outcome. Only a request that never got an answer - a transport failure
/// or a cancellation - leaves as an exception, because there is no response to describe.
/// </remarks>
internal sealed class TransmissionManagerClient(HttpClient httpClient)
{
    public async Task<ApiResult<Version>> GetAppVersionAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(EndpointAddresses.AppVersion, UriKind.Relative);
        using var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await response
            .ToApiResultAsync(DtoJsonSerializerContext.Default.Version, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApiResult<TorrentDto>> GetTorrentByIdAsync(
        long torrentId,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"{EndpointAddresses.Torrents}/{torrentId}", UriKind.Relative);
        using var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await response
            .ToApiResultAsync(DtoJsonSerializerContext.Default.TorrentDto, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApiResult<GetTorrentPageResponse>> GetTorrentPageAsync(
        GetTorrentPageParameters request = default,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(request.ToPathAndQueryString(), UriKind.Relative);
        using var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await response
            .ToApiResultAsync(DtoJsonSerializerContext.Default.GetTorrentPageResponse, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApiResult<AddTorrentResponse>> AddTorrentAsync(
        AddTorrentRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(EndpointAddresses.Torrents, UriKind.Relative);
        using var response = await httpClient
            .PostAsJsonAsync(
                requestUri,
                request,
                DtoJsonSerializerContext.Default.AddTorrentRequest,
                cancellationToken)
            .ConfigureAwait(false);

        var result = await response
            .ToApiResultAsync(DtoJsonSerializerContext.Default.AddTorrentResponse, cancellationToken)
            .ConfigureAwait(false);

        return RequireTorrent(result, result.Value?.TorrentDto);
    }

    public async Task<ApiResult<RefreshTorrentByIdResponse>> RefreshTorrentByIdAsync(
        long torrentId,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"{EndpointAddresses.Torrents}/{torrentId}", UriKind.Relative);
        using var response = await httpClient
            .PostAsync(requestUri, null, cancellationToken)
            .ConfigureAwait(false);

        var result = await response
            .ToApiResultAsync(DtoJsonSerializerContext.Default.RefreshTorrentByIdResponse, cancellationToken)
            .ConfigureAwait(false);

        return RequireTorrent(result, result.Value?.TorrentDto);
    }

    public async Task<ApiResult> UpdateTorrentByIdAsync(
        long torrentId,
        long version,
        UpdateTorrentByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"{EndpointAddresses.Torrents}/{torrentId}?version={version}", UriKind.Relative);
        using var response = await httpClient
            .PatchAsJsonAsync(
                requestUri,
                request,
                DtoJsonSerializerContext.Default.UpdateTorrentByIdRequest,
                cancellationToken)
            .ConfigureAwait(false);

        return await response.ToApiResultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApiResult> DeleteTorrentByIdAsync(
        long torrentId,
        long version,
        DeleteTorrentByIdType deleteType,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(
            $"{EndpointAddresses.Torrents}/{torrentId}?version={version}&deleteType={deleteType}",
            UriKind.Relative);

        using var response = await httpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await response.ToApiResultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Rejects a success that carries no torrent.
    /// </summary>
    /// <param name="result">The result the response was read into.</param>
    /// <param name="torrentDto">The torrent that result should be carrying.</param>
    /// <returns>
    /// <paramref name="result"/> itself, or a failure of the same status if the torrent is missing.
    /// </returns>
    /// <remarks>
    /// The response records declare the torrent as required, but the serializer does not enforce a
    /// non-nullable annotation, so an answer that omits it arrives as a null the callers would
    /// dereference. A success carrying no torrent is reported the same way a body that could not be
    /// read at all is - both mean the caller asked for a torrent and did not get one.
    /// </remarks>
    private static ApiResult<T> RequireTorrent<T>(ApiResult<T> result, TorrentDto? torrentDto)
        where T : class
    {
        return result.Status is ApiResultStatus.Success && torrentDto is null
            ? ApiResult<T>.Failure(result.StatusCode, result.ProblemDetails)
            : result;
    }
}
