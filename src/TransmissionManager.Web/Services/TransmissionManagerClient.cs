using System.Net.Http.Json;
using System.Text.Json;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Serialization;
using TransmissionManager.Web.Dto;

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

    public async Task<MutationOutcome> UpdateTorrentByIdAsync(
        long torrentId,
        long version,
        UpdateTorrentByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri($"{EndpointAddresses.Torrents}/{torrentId}?version={version}", UriKind.Relative);
        using var response = await httpClient
            .PatchAsJsonAsync(requestUri, request, cancellationToken)
            .ConfigureAwait(false);

        return await ToMutationOutcomeAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MutationOutcome> DeleteTorrentByIdAsync(
        long torrentId,
        long version,
        DeleteTorrentByIdType deleteType,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(
            $"{EndpointAddresses.Torrents}/{torrentId}?version={version}&deleteType={deleteType}",
            UriKind.Relative);
        using var response = await httpClient.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
        return await ToMutationOutcomeAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<MutationOutcome> ToMutationOutcomeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is System.Net.HttpStatusCode.NotFound)
            return MutationOutcome.NotFound;

        if (response.StatusCode is System.Net.HttpStatusCode.Conflict)
        {
            long? currentVersion = null;
            try
            {
                using var stream = await response.Content
                    .ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);

                using var document = await JsonDocument
                    .ParseAsync(stream, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (document.RootElement.ValueKind is JsonValueKind.Object
                    && document.RootElement.TryGetProperty(ProblemDetailsKeys.CurrentVersion, out var element)
                    && element.ValueKind is JsonValueKind.Number
                    && element.TryGetInt64(out var v))
                {
                    currentVersion = v;
                }
            }
            catch (JsonException)
            {
                // Body absent or malformed; fall through with null currentVersion.
            }

            return MutationOutcome.Conflict(currentVersion);
        }

        _ = response.EnsureSuccessStatusCode();
        return MutationOutcome.Success;
    }
}
