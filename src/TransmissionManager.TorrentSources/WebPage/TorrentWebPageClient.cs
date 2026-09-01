using Microsoft.Extensions.Options;
using Polly;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using TransmissionManager.TorrentSources.Dto;

namespace TransmissionManager.TorrentSources.WebPage;

public sealed class TorrentWebPageClient(
    IOptionsMonitor<TorrentWebPageClientOptions> options,
    HttpClient httpClient)
    : ITorrentSourceClient
{
    private const int _bufferSize = 2048;
    private const int _defaultPadding = _bufferSize / 16;
    private const int _maxBufferFreeSpace = _bufferSize / 8;
    private const string _magnetScheme = "magnet";

    private static ReadOnlySpan<byte> Magnet => "magnet:?"u8;

    /// <summary>
    /// Finds a magnet URI on the specified torrent web page.
    /// </summary>
    /// <param name="torrentWebPageUri">The URI of the torrent web page to search.</param>
    /// <param name="regexPattern">An optional regular expression pattern to match the magnet URI.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="MagnetSearchOutcome"/> representing the result of the search, including failures.
    /// Cancellation requested through <paramref name="cancellationToken"/> still propagates.
    /// </returns>
    /// <remarks>
    /// In addition to <paramref name="cancellationToken"/>, getting a response with headers is
    /// bounded by the resilience pipeline, and reading the response body - by
    /// <see cref="TorrentWebPageClientOptions.ResponseReadTimeout"/>.
    /// </remarks>
    public async Task<MagnetSearchOutcome> FindMagnetUriAsync(
        Uri torrentWebPageUri,
        [StringSyntax(StringSyntaxAttribute.Regex)] string? regexPattern = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(torrentWebPageUri);

        if (!torrentWebPageUri.IsAbsoluteUri ||
            (torrentWebPageUri.Scheme != Uri.UriSchemeHttp && torrentWebPageUri.Scheme != Uri.UriSchemeHttps))
        {
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.InvalidSource,
                "The URI must be an absolute HTTP or HTTPS address.");
        }

        var currentOptions = options.CurrentValue;

        // Ensure that the user-supplied regexPattern, if provided, has expected shape.
        if (!string.IsNullOrEmpty(regexPattern) && !TorrentRegex.IsFindMagnetRegex().IsMatch(regexPattern))
        {
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.InvalidSelector,
                $"Invalid magnet-matching regex provided. The value must match '{TorrentRegex.IsFindMagnet}'.");
        }

        try
        {
            // ResponseHeadersRead makes sure await returns after getting the response headers.
            using var response = await httpClient
                .GetAsync(torrentWebPageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return MagnetSearchOutcome.Failure(
                    MagnetSearchResult.RetrievalFailed,
                    $"The server responded with {(int)response.StatusCode} {response.StatusCode}");
            }

            return await FindMagnetUriInResponseAsync(
                    response,
                    currentOptions,
                    regexPattern,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RegexParseException e)
        {
            return MagnetSearchOutcome.Failure(MagnetSearchResult.InvalidSelector, e.Message);
        }
        catch (RegexMatchTimeoutException)
        {
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.InvalidSelector,
                GetRegexTimeoutError(regexPattern));
        }
        catch (Exception e) when (e // OperationCanceledException is not caught and should propagate
            is HttpRequestException
            or IOException // a connection dropped mid-body arrives as HttpIOException, not HttpRequestException
            or ExecutionRejectedException) // Polly: attempt/total timeout, open circuit, rate limiter
        {
            // The message can quote what the server sent, so it is the source's text, not ours.
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.RetrievalFailed,
                RemoteTextUtils.Summarize(e.Message));
        }
    }

    private static async Task<MagnetSearchOutcome> FindMagnetUriInResponseAsync(
        HttpResponseMessage response,
        TorrentWebPageClientOptions currentOptions,
        string? regexPattern,
        CancellationToken cancellationToken)
    {
        var bodyReadTimeout = currentOptions.ResponseReadTimeout;

        using var readTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readTimeoutCts.CancelAfter(bodyReadTimeout);

        MagnetSearchOutcome? outcome;
        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(readTimeoutCts.Token).ConfigureAwait(false);

            outcome = await FindMagnetUriInStreamAsync(stream, currentOptions, regexPattern, readTimeoutCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.RetrievalFailed,
                $"The source did not deliver a complete response within {bodyReadTimeout}.");
        }

        return outcome ?? MagnetSearchOutcome.Failure(
            MagnetSearchResult.NotFound,
            "No magnet link was found on the page");
    }

    private static async Task<MagnetSearchOutcome?> FindMagnetUriInStreamAsync(
        Stream stream,
        TorrentWebPageClientOptions currentOptions,
        string? regexPattern,
        CancellationToken cancellationToken)
    {
        var byteBuffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        var reader = new PaddedBytesReader(stream, byteBuffer, _maxBufferFreeSpace);
        try
        {
            var padding = 0;
            while (await reader.ReadNextAsync(padding, cancellationToken).ConfigureAwait(false))
            {
                var bytes = reader.Bytes;
                var indexOfMagnet = bytes.IndexOf(Magnet);

                // magnet not found, but there may be more to read - continue
                if (indexOfMagnet is -1)
                {
                    padding = Math.Min(_defaultPadding, bytes.Length);
                    continue;
                }

                // magnet found too deep in the buffer - read once more to shift the magnet to the padding position
                if (indexOfMagnet >= _defaultPadding * 4)
                {
                    _ = await reader.ReadNextAsync(bytes.Length - indexOfMagnet + _defaultPadding, cancellationToken)
                        .ConfigureAwait(false);

                    bytes = reader.Bytes;
                    indexOfMagnet = bytes.IndexOf(Magnet);
                }

                // magnet found, but may not match the regex - ensure it matches the regex and return it
                var outcome = FindMagnetUriInBytes(bytes, currentOptions, regexPattern);
                if (outcome is not null)
                    return outcome;

                // magnet did not match the regex, set padding to shift it out of the buffer and continue
                padding = bytes.Length - indexOfMagnet - Magnet.Length;
            }

            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuffer);
        }
    }

    private static MagnetSearchOutcome? FindMagnetUriInBytes(
        ReadOnlySpan<byte> bytes,
        TorrentWebPageClientOptions currentOptions,
        string? regexPattern)
    {
        var charBuffer = ArrayPool<char>.Shared.Rent(bytes.Length);
        try
        {
            var chars = charBuffer.AsSpan(0, bytes.Length);
            if (!Encoding.UTF8.TryGetChars(bytes, chars, out var charsWritten) ||
                !TryGetFirstMatch(currentOptions, regexPattern, chars[..charsWritten], out var magnetRange))
            {
                return null;
            }

            // A pattern whose quantifiers are all optional matches an empty string, which is neither
            // a magnet link nor a reason to stop looking for one.
            var match = chars[magnetRange];
            if (match.IsEmpty)
                return null;

            var matchText = new string(match);

            // Ensure that the matched text is a valid magnet link.
            return Uri.TryCreate(matchText, UriKind.Absolute, out var magnetUri) &&
                magnetUri.Scheme == _magnetScheme
                    ? MagnetSearchOutcome.Found(magnetUri)
                    : MagnetSearchOutcome.Failure(
                        MagnetSearchResult.InvalidSelector,
                        $"The match is not a magnet link: '{RemoteTextUtils.Summarize(match)}' " +
                            $"({match.Length} characters). Check the magnet regex.");
        }
        finally
        {
            ArrayPool<char>.Shared.Return(charBuffer);
        }
    }

    /// <remarks>
    /// Names which pattern timed out rather than quoting it: the two are set in different places, so
    /// which one it was decides whether this torrent or the whole deployment needs attention.
    /// </remarks>
    private static string GetRegexTimeoutError(string? regexPattern) =>
        string.IsNullOrEmpty(regexPattern)
            ? $"The configured {nameof(TorrentWebPageClientOptions.DefaultMagnetRegexPattern)} timed out on this page."
            : "This torrent's magnetRegexPattern timed out on this page.";

    private static bool TryGetFirstMatch(
        TorrentWebPageClientOptions currentOptions,
        string? regexPattern,
        ReadOnlySpan<char> chars,
        out Range matchRange)
    {
        return string.IsNullOrEmpty(regexPattern)
            ? currentOptions.DefaultMagnetRegex.TryGetFirstMatch(chars, out matchRange)
            : chars.TryGetFirstMatch(regexPattern, currentOptions.RegexMatchTimeout, out matchRange);
    }
}
