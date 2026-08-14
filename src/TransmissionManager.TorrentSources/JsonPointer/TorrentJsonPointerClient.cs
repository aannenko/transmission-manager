using Microsoft.Extensions.Options;
using Polly;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using TransmissionManager.TorrentSources.Dto;
using TransmissionManager.TorrentSources.Options;

namespace TransmissionManager.TorrentSources.JsonPointer;

public sealed class TorrentJsonPointerClient(
    IOptionsMonitor<TorrentJsonPointerClientOptions> options,
    HttpClient httpClient)
    : ITorrentSourceClient
{
    private const int _infoHashLength = 40;

    private const string _infoHashDescription =
        "a string of 40 hexadecimal characters, which is a BitTorrent v1 info hash";

    /// <summary>
    /// Finds a magnet link in a JSON document, at the JSON Pointer carried by the fragment of
    /// <paramref name="sourceUri"/>.
    /// </summary>
    /// <param name="sourceUri">
    /// The address of the document, ending with an RFC 6901 JSON Pointer as its fragment, as in
    /// <c>https://source.com/forum/1106#/result/6880555/7</c>.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="MagnetSearchOutcome"/> representing the result of the search, including failures.
    /// Cancellation requested through <paramref name="cancellationToken"/> still propagates.
    /// </returns>
    /// <remarks>
    /// In addition to <paramref name="cancellationToken"/>, getting a response with headers is
    /// bounded by the resilience pipeline, and reading the response body - by
    /// <see cref="TorrentSourcesOptions.ResponseReadTimeout"/>.
    /// </remarks>
    public async Task<MagnetSearchOutcome> FindMagnetUriAsync(
        Uri sourceUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceUri);

        if (!sourceUri.IsAbsoluteUri ||
            (sourceUri.Scheme != Uri.UriSchemeHttp && sourceUri.Scheme != Uri.UriSchemeHttps))
        {
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.InvalidSource,
                "The URI must be an absolute HTTP or HTTPS address.");
        }

        var currentOptions = options.CurrentValue;

        if (!JsonPointerParser.TryParsePointer(
                sourceUri.Fragment,
                currentOptions.MaxJsonTokenBytes,
                out var segments,
                out var pointerError))
        {
            return MagnetSearchOutcome.Failure(MagnetSearchResult.InvalidSelector, pointerError);
        }

        try
        {
            // ResponseHeadersRead makes sure await returns after getting the response headers.
            using var response = await httpClient
                .GetAsync(sourceUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return MagnetSearchOutcome.Failure(
                    MagnetSearchResult.RetrievalFailed,
                    $"The server responded with {(int)response.StatusCode} {response.StatusCode}");
            }

            return await FindMagnetUriInResponseAsync(
                    response,
                    segments,
                    currentOptions.MaxJsonTokenBytes,
                    currentOptions.ResponseReadTimeout,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception e) when (e // OperationCanceledException is not caught and should propagate
            is HttpRequestException
            or IOException // a connection dropped mid-body arrives as HttpIOException, not HttpRequestException
            or ExecutionRejectedException // Polly: attempt/total timeout, open circuit, rate limiter
            or JsonException) // the response is not the JSON document the source promised
        {
            return MagnetSearchOutcome.Failure(MagnetSearchResult.RetrievalFailed, e.Message);
        }
    }

    private static async Task<MagnetSearchOutcome> FindMagnetUriInResponseAsync(
        HttpResponseMessage response,
        string[] pointerSegments,
        int maxJsonTokenBytes,
        TimeSpan bodyReadTimeout,
        CancellationToken cancellationToken)
    {
        using var readTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readTimeoutCts.CancelAfter(bodyReadTimeout);

        (JsonPointerResolution, string?, JsonValueKind) result;
        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(readTimeoutCts.Token).ConfigureAwait(false);

            result = await JsonPointerResolver
                .ResolveAsync(stream, pointerSegments, maxJsonTokenBytes, readTimeoutCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.RetrievalFailed,
                $"The source did not deliver a complete response within {bodyReadTimeout}.");
        }

        var (resolution, value, valueKind) = result;

        if (resolution is JsonPointerResolution.NotFound)
        {
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.NotFound,
                "The document holds no value at the given JSON Pointer.");
        }

        // A pointer one field off is the likeliest mistake, so name what was found there.
        if (resolution is JsonPointerResolution.NotAString)
        {
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.InvalidSelector,
                $"The JSON Pointer addresses {DescribeKind(valueKind)}, but it must address {_infoHashDescription}.");
        }

        if (!TryGetInfoHash(value, out var infoHash))
        {
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.InvalidSelector,
                $"The JSON Pointer addresses a string that is not {_infoHashDescription}.");
        }

        return MagnetSearchOutcome.Found(new($"magnet:?xt=urn:btih:{infoHash}"));
    }

    private static string DescribeKind(JsonValueKind valueKind) => valueKind switch
    {
        JsonValueKind.Null => "a null value",
        JsonValueKind.Number => "a number",
        JsonValueKind.True or JsonValueKind.False => "a boolean",
        JsonValueKind.Object => "an object",
        JsonValueKind.Array => "an array",
        _ => "a non-string value",
    };

    /// <remarks>
    /// Lowercased because sources differ on case while the rest of this application does not, so
    /// the same torrent must not yield two different magnet links.
    /// </remarks>
    private static bool TryGetInfoHash(string? value, [NotNullWhen(true)] out string? infoHash)
    {
        infoHash = null;
        if (value?.Length is not _infoHashLength)
            return false;

        foreach (var character in value)
            if (!char.IsAsciiHexDigit(character))
                return false;

        infoHash = value.ToLowerInvariant();
        return true;
    }
}
