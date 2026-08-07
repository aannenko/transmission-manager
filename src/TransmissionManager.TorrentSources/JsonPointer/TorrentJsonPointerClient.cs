using Microsoft.Extensions.Options;
using Polly;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using TransmissionManager.TorrentSources.Dto;
using TransmissionManager.TorrentSources.Options;

namespace TransmissionManager.TorrentSources.JsonPointer;

public sealed class TorrentJsonPointerClient(
    IOptionsMonitor<TorrentSourcesOptions> sourcesOptions,
    IOptionsMonitor<TorrentJsonPointerClientOptions> options,
    HttpClient httpClient)
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
    /// The document is never held in full, so its size is bounded only by
    /// <see cref="TorrentSourcesOptions.MagnetSearchTimeout"/>, which covers reading the response
    /// body as well as obtaining it.
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

        // Read once: the limit that accepts a segment and the limit the document is read through
        // must be the same number, or a reload between them would apply one and then the other.
        var maxJsonTokenBytes = options.CurrentValue.MaxJsonTokenBytes;

        if (!JsonPointerParser.TryParsePointer(
                sourceUri.Fragment,
                maxJsonTokenBytes,
                out var segments,
                out var pointerError))
        {
            return MagnetSearchOutcome.Failure(MagnetSearchResult.InvalidSelector, pointerError);
        }

        var searchTimeout = sourcesOptions.CurrentValue.MagnetSearchTimeout;

        // The resilience pipeline's timeouts end at the response headers, so only this token can
        // stop a source that sends headers and then stalls.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(searchTimeout);

        try
        {
            return await FindMagnetUriInDocumentAsync(
                    sourceUri,
                    segments,
                    maxJsonTokenBytes,
                    timeoutCts.Token)
                .ConfigureAwait(false);
        }
        catch (Exception e) when (e is
            OperationCanceledException or // the budget expired, or the caller cancelled
            HttpRequestException or
            IOException or // a connection dropped mid-body surfaces here, not as HttpRequestException
            ExecutionRejectedException or // Polly: attempt/total timeout, open circuit, rate limiter
            JsonException) // the response is not the JSON document the source promised
        {
            cancellationToken.ThrowIfCancellationRequested();

            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.RetrievalFailed,
                timeoutCts.IsCancellationRequested
                    ? $"The source did not deliver a complete response within {searchTimeout}."
                    : e.Message);
        }
    }

    private async Task<MagnetSearchOutcome> FindMagnetUriInDocumentAsync(
        Uri sourceUri,
        string[] pointerSegments,
        int maxJsonTokenBytes,
        CancellationToken cancellationToken)
    {
        // ResponseHeadersRead leaves the body unbuffered, so the response must outlive the stream.
        using var response = await httpClient
            .GetAsync(sourceUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.RetrievalFailed,
                $"The server responded with {(int)response.StatusCode} {response.StatusCode}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        var (resolution, value, valueKind) = await JsonPointerResolver
            .ResolveAsync(stream, pointerSegments, maxJsonTokenBytes, cancellationToken)
            .ConfigureAwait(false);

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
