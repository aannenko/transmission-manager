using Microsoft.Extensions.Options;
using System.Text.Json.Serialization.Metadata;
using TransmissionManager.Api.Serialization;
using TransmissionManager.Api.Transmission.Extensions;
using TransmissionManager.Api.Transmission.Models;
using TransmissionManager.Api.Transmission.Options;

namespace TransmissionManager.Api.Transmission.Services;

public sealed class TransmissionClient(IOptionsMonitor<TransmissionClientOptions> options, HttpClient httpClient)
{
    private static readonly TorrentGetRequestFields[] _defaultRequestFields =
        Enum.GetValues<TorrentGetRequestFields>();

    public async Task<TorrentGetResponse> GetTorrentsAsync(
        long[]? ids = null,
        TorrentGetRequestFields[]? requestFields = null,
        CancellationToken cancellationToken = default)
    {
        return await GetResultWithValidationAsync(
            new TorrentGetRequest
            {
                Arguments = new()
                {
                    Fields = requestFields ?? _defaultRequestFields,
                    Ids = ids,
                }
            },
            AppJsonSerializerContext.Default.TorrentGetRequest,
            AppJsonSerializerContext.Default.TorrentGetResponse,
            cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TorrentAddResponse> AddTorrentMagnetAsync(
        string magnetUri,
        string downloadDir,
        CancellationToken cancellationToken = default)
    {
        return await GetResultWithValidationAsync(
            new TorrentAddRequest
            {
                Arguments = new()
                {
                    Filename = magnetUri,
                    DownloadDir = downloadDir,
                }
            },
            AppJsonSerializerContext.Default.TorrentAddRequest,
            AppJsonSerializerContext.Default.TorrentAddResponse,
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
        var endpoint = options.CurrentValue.RpcEndpointAddressSuffix;
        var response = await httpClient.PostAsJsonAsync(endpoint, request, requestTypeInfo, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var responseObject = await response.Content
            .ReadFromJsonAsync(responseTypeInfo, cancellationToken)
            .ConfigureAwait(false);

        if (responseObject is null)
        {
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                "Unexpected response from Transmission. " +
                $"Cannot deserialize the following content to {typeof(TResponse).FullName}: '{responseString}'.");
        }

        if (!responseObject.IsSuccess())
            throw new BadHttpRequestException(
                $"Response from Transmission does not indicate success: '{responseObject.Result}'");

        return responseObject;
    }
}
