using Microsoft.Extensions.Options;
using Polly;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TransmissionManager.TorrentSources.Dto;

namespace TransmissionManager.TorrentSources.JsonPointer;

public sealed class TorrentJsonPointerClient(
    IOptionsMonitor<TorrentJsonPointerClientOptions> options,
    HttpClient httpClient)
    : ITorrentSourceClient
{
    private const string _magnetScheme = "magnet";

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
    /// <see cref="TorrentJsonPointerClientOptions.ResponseReadTimeout"/>.
    /// </remarks>
    public async Task<MagnetSearchOutcome> FindMagnetUriAsync(
        Uri sourceUri,
        [StringSyntax(StringSyntaxAttribute.Regex)] string? jsonValueRegexPattern = null,
        string? jsonValueFormat = null,
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

        if (!TryGetJsonValueFormat(currentOptions, jsonValueFormat, out var valueFormat, out var formatError))
            return MagnetSearchOutcome.Failure(MagnetSearchResult.InvalidSelector, formatError);

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
                    currentOptions,
                    jsonValueRegexPattern,
                    valueFormat,
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
        TorrentJsonPointerClientOptions currentOptions,
        string? valueRegexPattern,
        CompositeFormat? valueFormat,
        CancellationToken cancellationToken)
    {
        var bodyReadTimeout = currentOptions.ResponseReadTimeout;

        using var readTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readTimeoutCts.CancelAfter(bodyReadTimeout);

        (JsonPointerResolution, string?, JsonValueKind) result;
        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(readTimeoutCts.Token).ConfigureAwait(false);

            result = await JsonPointerResolver
                .ResolveAsync(stream, pointerSegments, currentOptions.MaxJsonTokenBytes, readTimeoutCts.Token)
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
                $"The JSON Pointer addresses {DescribeKind(valueKind)}, but it must address a string.");
        }

        return BuildMagnetUri(value!, currentOptions, valueRegexPattern, valueFormat);
    }

    /// <remarks>
    /// The pattern and <paramref name="valueFormat"/> are optional and independent: with neither, the
    /// addressed string is already the magnet link.
    /// <para>
    /// The pattern's whole match is the value, so a pattern that needs surrounding context to find
    /// the right place uses zero-width lookarounds for it rather than a capturing group.
    /// </para>
    /// </remarks>
    private static MagnetSearchOutcome BuildMagnetUri(
        string value,
        TorrentJsonPointerClientOptions currentOptions,
        string? valueRegexPattern,
        CompositeFormat? valueFormat)
    {
        var isSupplied = !string.IsNullOrEmpty(valueRegexPattern);
        var defaultRegex = currentOptions.DefaultJsonValueRegex;

        if (isSupplied || defaultRegex is not null)
        {
            bool found;
            Range matchRange;
            try
            {
                found = isSupplied
                    ? value.AsSpan()
                        .TryGetFirstMatch(valueRegexPattern!, currentOptions.RegexMatchTimeout, out matchRange)
                    : defaultRegex!.TryGetFirstMatch(value, out matchRange);
            }
            catch (RegexParseException e)
            {
                return MagnetSearchOutcome.Failure(MagnetSearchResult.InvalidSelector, e.Message);
            }
            catch (RegexMatchTimeoutException)
            {
                return MagnetSearchOutcome.Failure(
                    MagnetSearchResult.InvalidSelector,
                    GetRegexTimeoutError(valueRegexPattern));
            }

            // A pattern whose quantifiers are all optional matches an empty string, which would go on
            // to build a magnet link with nothing where the hash belongs.
            var (_, matchLength) = matchRange.GetOffsetAndLength(value.Length);
            if (!found || matchLength is 0)
            {
                var patternText = isSupplied ? valueRegexPattern : defaultRegex!.ToString();
                return MagnetSearchOutcome.Failure(
                    MagnetSearchResult.NotFound,
                    $"The string at the JSON Pointer holds no match for '{patternText}'.");
            }

            value = value[matchRange];
        }

        if (valueFormat is not null)
            value = string.Format(CultureInfo.InvariantCulture, valueFormat, value);

        // The application cannot know the URI dialects the trackers or the user may supply,
        // but it can at least enforce what it knows: that the URI is absolute and uses the magnet scheme.
        if (!Uri.TryCreate(value, UriKind.Absolute, out var magnetUri) || magnetUri.Scheme != _magnetScheme)
        {
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.InvalidSelector,
                $"'{value}' is not a magnet link. Check the value pattern and the magnet format.");
        }

        return MagnetSearchOutcome.Found(magnetUri);
    }

    /// <remarks>
    /// Names which pattern timed out rather than quoting it: the two are set in different places, so
    /// which one it was decides whether this torrent or the whole deployment needs attention.
    /// </remarks>
    private static string GetRegexTimeoutError(string? valueRegexPattern) =>
        string.IsNullOrEmpty(valueRegexPattern)
            ? $"The configured {nameof(TorrentJsonPointerClientOptions.DefaultJsonValueRegexPattern)} " +
                "timed out on the string at the JSON Pointer."
            : "This torrent's magnetRegexPattern timed out on the string at the JSON Pointer.";

    private static bool TryGetJsonValueFormat(
        TorrentJsonPointerClientOptions currentOptions,
        string? format,
        out CompositeFormat? valueFormat,
        [NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrEmpty(format))
        {
            valueFormat = currentOptions.DefaultJsonValueCompositeFormat;
            error = null;
            return true;
        }

        if (!JsonValueRegex.IsJsonValueFormatRegex().IsMatch(format))
        {
            valueFormat = null;
            error = $"Invalid magnet format provided. The value must match '{JsonValueRegex.IsJsonValueFormat}'.";
            return false;
        }

        valueFormat = CompositeFormat.Parse(format);
        error = null;
        return true;
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
}
