﻿using Microsoft.Extensions.Options;
using System.Text.Json.Serialization.Metadata;
using TransmissionManager.Api.Transmission.Extensions;
using TransmissionManager.Api.Transmission.Dto;
using TransmissionManager.Api.Transmission.Options;
using TransmissionManager.Api.Transmission.Serialization;

namespace TransmissionManager.Api.Transmission.Services;

public sealed class TransmissionClient(IOptionsMonitor<TransmissionClientOptions> options, HttpClient httpClient)
{
    private static readonly TransmissionTorrentGetRequestFields[] _defaultRequestFields =
        Enum.GetValues<TransmissionTorrentGetRequestFields>();

    public async Task<TransmissionTorrentGetResponse> GetTorrentsAsync(
        long[]? ids = null,
        TransmissionTorrentGetRequestFields[]? requestFields = null,
        CancellationToken cancellationToken = default)
    {
        return await GetResultWithValidationAsync(
            new TransmissionTorrentGetRequest
            {
                Arguments = new()
                {
                    Fields = requestFields ?? _defaultRequestFields,
                    Ids = ids,
                }
            },
            TransmissionJsonSerializerContext.Default.TransmissionTorrentGetRequest,
            TransmissionJsonSerializerContext.Default.TransmissionTorrentGetResponse,
            cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TransmissionTorrentAddResponse> AddTorrentUsingMagnetUriAsync(
        string magnetUri,
        string downloadDir,
        CancellationToken cancellationToken = default)
    {
        return await GetResultWithValidationAsync(
            new TransmissionTorrentAddRequest
            {
                Arguments = new()
                {
                    Filename = magnetUri,
                    DownloadDir = downloadDir,
                }
            },
            TransmissionJsonSerializerContext.Default.TransmissionTorrentAddRequest,
            TransmissionJsonSerializerContext.Default.TransmissionTorrentAddResponse,
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
        var response = await httpClient
            .PostAsJsonAsync(endpoint, request, requestTypeInfo, cancellationToken)
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
            throw new HttpRequestException(
                $"Response from Transmission does not indicate success: '{responseObject.Result}'");

        return responseObject;
    }
}
