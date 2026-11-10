using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using TransmissionManager.Http;
using TransmissionManager.Transmission.Dto;
using TransmissionManager.Transmission.Options;
using TransmissionManager.Transmission.Serialization;

namespace TransmissionManager.Transmission.Services;

/// <summary>Calls the Transmission RPC API.</summary>
/// <param name="options">The monitored client options.</param>
/// <param name="httpClient">The configured HTTP client.</param>
public sealed class TransmissionClient(IOptionsMonitor<TransmissionClientOptions> options, HttpClient httpClient)
{
    private static readonly TransmissionTorrentGetRequestFields[] _defaultRequestFields =
        Enum.GetValues<TransmissionTorrentGetRequestFields>();

    /// <summary>Gets torrents from Transmission.</summary>
    /// <param name="hashStrings">The info hashes to select, or <see langword="null"/> for all torrents.</param>
    /// <param name="requestFields">The fields to request, or <see langword="null"/> for every supported field.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>Transmission's <c>torrent-get</c> response.</returns>
    /// <exception cref="HttpRequestException">
    /// The HTTP exchange fails, the response cannot be deserialized, or Transmission reports an unsuccessful RPC result.
    /// </exception>
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

    /// <summary>Adds a torrent to Transmission by magnet URI.</summary>
    /// <param name="magnetUri">The magnet URI.</param>
    /// <param name="downloadDir">The target download directory.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>Transmission's <c>torrent-add</c> response.</returns>
    /// <exception cref="HttpRequestException">
    /// The HTTP exchange fails, the response cannot be deserialized, or Transmission reports an unsuccessful RPC result.
    /// </exception>
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

    /// <summary>Removes torrents from Transmission.</summary>
    /// <param name="hashStrings">The info hashes to remove, or <see langword="null"/> for all torrents.</param>
    /// <param name="deleteLocalData">Whether to delete downloaded data.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>Transmission's <c>torrent-remove</c> response.</returns>
    /// <exception cref="HttpRequestException">
    /// The HTTP exchange fails, the response cannot be deserialized, or Transmission reports an unsuccessful RPC result.
    /// </exception>
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new HttpRequestException(
                $"Request to Transmission failed unexpectedly: '{RemoteTextUtils.Summarize(e.Message)}'.",
                e);
        }

        using (response)
        {
            // EnsureSuccessStatusCode would quote the reason phrase, which is the server's text.
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    RemoteTextUtils.DescribeStatus(response.StatusCode),
                    null,
                    response.StatusCode);
            }

            using var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            TResponse? responseObject = default;
            JsonException? jsonException = null;
            try
            {
                // JSON is UTF-8 whatever charset the response declares (RFC 8259, section 8.1), while
                // ReadFromJsonAsync decodes by that charset and throws on one it does not support.
                responseObject = await JsonSerializer
                    .DeserializeAsync(body, responseTypeInfo, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (JsonException e)
            {
                jsonException = e;
            }

            if (responseObject is null)
            {
                var excerpt = await SummarizeBodyAsync(body, cancellationToken).ConfigureAwait(false);
                throw new HttpRequestException(
                    "Unexpected response from Transmission. " +
                    $"Cannot deserialize the following content to {typeof(TResponse).FullName}: '{excerpt}'.",
                    jsonException);
            }

            return responseObject.IsSuccess()
                ? responseObject
                : throw new HttpRequestException(
                    "Response from Transmission does not indicate success: " +
                    $"'{RemoteTextUtils.Summarize(responseObject.Result)}'");
        }
    }

    private static async Task<string> SummarizeBodyAsync(Stream bufferedBody, CancellationToken cancellationToken)
    {
        // Only a buffered body can be rewound: streaming the response instead would make this throw.
        bufferedBody.Position = 0;

        using var reader = new StreamReader(
            bufferedBody,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        // One character past the budget, or Summarize cannot tell that it cut.
        var excerpt = new char[RemoteTextUtils.DefaultSummaryLength + 1];
        var excerptLength = await reader.ReadBlockAsync(excerpt, cancellationToken).ConfigureAwait(false);

        return RemoteTextUtils.Summarize(excerpt.AsSpan(0, excerptLength));
    }
}
