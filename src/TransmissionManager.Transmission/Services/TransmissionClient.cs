using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using TransmissionManager.Transmission.Dto;
using TransmissionManager.Transmission.Extensions;
using TransmissionManager.Transmission.Options;
using TransmissionManager.Transmission.Serialization;

namespace TransmissionManager.Transmission.Services;

public sealed class TransmissionClient(IOptionsMonitor<TransmissionClientOptions> options, HttpClient httpClient)
{
    private static readonly TransmissionTorrentGetRequestFields[] _defaultRequestFields =
        Enum.GetValues<TransmissionTorrentGetRequestFields>();

    public async Task<TransmissionTorrentGetResponse> GetTorrentsAsync(
        string[]? hashStrings = null,
        TransmissionTorrentGetRequestFields[]? requestFields = null,
        CancellationToken cancellationToken = default)
    {
        return await GetResultWithValidationAsync(
            new TransmissionTorrentGetRequest
            {
                Arguments = new()
                {
                    HashStrings = hashStrings,
                    Fields = requestFields ?? _defaultRequestFields,
                }
            },
            TransmissionJsonSerializerContext.Default.TransmissionTorrentGetRequest,
            TransmissionJsonSerializerContext.Default.TransmissionTorrentGetResponse,
            cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TransmissionTorrentAddResponse> AddTorrentUsingMagnetUriAsync(
        Uri magnetUri,
        string downloadDir,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(magnetUri);

        return await GetResultWithValidationAsync(
            new TransmissionTorrentAddRequest
            {
                Arguments = new()
                {
                    Filename = magnetUri.OriginalString,
                    DownloadDir = downloadDir,
                }
            },
            TransmissionJsonSerializerContext.Default.TransmissionTorrentAddRequest,
            TransmissionJsonSerializerContext.Default.TransmissionTorrentAddResponse,
            cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TransmissionTorrentRemoveResponse> RemoveTorrentsAsync(
        string[]? hashStrings = null,
        bool deleteLocalData = false,
        CancellationToken cancellationToken = default)
    {
        return await GetResultWithValidationAsync(
            new TransmissionTorrentRemoveRequest
            {
                Arguments = new()
                {
                    HashStrings = hashStrings,
                    DeleteLocalData = deleteLocalData
                }
            },
            TransmissionJsonSerializerContext.Default.TransmissionTorrentRemoveRequest,
            TransmissionJsonSerializerContext.Default.TransmissionTorrentRemoveResponse,
            cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<TResponse> GetResultWithValidationAsync<TRequest, TResponse>(
        TRequest request,
        JsonTypeInfo<TRequest> requestTypeInfo,
        JsonTypeInfo<TResponse> responseTypeInfo,
        CancellationToken cancellationToken)
        where TResponse : ITransmissionResponse
    {
        var endpoint = options.CurrentValue.RpcEndpointAddressSuffixUri;

        HttpResponseMessage response;
        try
        {
            response = await httpClient
                .PostAsJsonAsync(endpoint, request, requestTypeInfo, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            throw new HttpRequestException($"Request to Transmission failed unexpectedly: '{e.Message}'.", e);
        }

        var responseObject = await response.EnsureSuccessStatusCode().Content
            .ReadFromJsonAsync(responseTypeInfo, cancellationToken)
            .ConfigureAwait(false);

        if (responseObject is null)
        {
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                "Unexpected response from Transmission. " +
                $"Cannot deserialize the following content to {typeof(TResponse).FullName}: '{responseString}'.");
        }

        return responseObject.IsSuccess()
            ? responseObject
            : throw new HttpRequestException(
                $"Response from Transmission does not indicate success: '{responseObject.Result}'");
    }
}
