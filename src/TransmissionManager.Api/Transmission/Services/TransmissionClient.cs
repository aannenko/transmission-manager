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

    private static JsonTypeInfo<TorrentGetRequest> GetRequestInfo =>
        AppJsonSerializerContext.Default.TorrentGetRequest;

    private static JsonTypeInfo<TorrentGetResponse> GetResponseInfo =>
        AppJsonSerializerContext.Default.TorrentGetResponse;

    private static JsonTypeInfo<TorrentAddRequest> AddRequestInfo =>
        AppJsonSerializerContext.Default.TorrentAddRequest;

    private static JsonTypeInfo<TorrentAddResponse> AddResponseInfo =>
        AppJsonSerializerContext.Default.TorrentAddResponse;

    private string Endpoint => options.CurrentValue.RpcEndpointAddressSuffix;

    public async Task<TorrentGetResponse> GetTorrentsAsync(
        long[]? ids = null,
        TorrentGetRequestFields[]? requestFields = null,
        CancellationToken cancellationToken = default)
    {
        var request = new TorrentGetRequest
        {
            Arguments = new()
            {
                Fields = requestFields ?? _defaultRequestFields,
                Ids = ids,
            }
        };

        var response = await httpClient.PostAsJsonAsync(Endpoint, request, GetRequestInfo, cancellationToken)
            .ConfigureAwait(false);

        return await GetResultWithValidationAsync(response, GetResponseInfo, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TorrentAddResponse> AddTorrentMagnetAsync(
        string magnetUri,
        string downloadDir,
        CancellationToken cancellationToken = default)
    {
        var request = new TorrentAddRequest
        {
            Arguments = new()
            {
                Filename = magnetUri,
                DownloadDir = downloadDir,
            }
        };

        var response = await httpClient.PostAsJsonAsync(Endpoint, request, AddRequestInfo, cancellationToken)
            .ConfigureAwait(false);

        return await GetResultWithValidationAsync(response, AddResponseInfo, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<T> GetResultWithValidationAsync<T>(
        HttpResponseMessage response,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
        where T : ITransmissionResponse
    {
        response.EnsureSuccessStatusCode();

        T? responseObject = await response.Content
            .ReadFromJsonAsync(jsonTypeInfo, cancellationToken)
            .ConfigureAwait(false);

        if (responseObject is null)
        {
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                "Unexpected response from Transmission. " +
                $"Cannot deserialize the following content to {typeof(T).FullName}: '{responseString}'.");
        }

        if (!responseObject.IsSuccess())
            throw new BadHttpRequestException(
                $"Response from Transmission does not indicate success: '{responseObject.Result}'");

        return responseObject;
    }
}
