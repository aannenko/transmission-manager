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

        if (!TryGetMagnetRegex(currentOptions, regexPattern, out var regex, out var regexError))
            return MagnetSearchOutcome.Failure(MagnetSearchResult.InvalidSelector, regexError);

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
                    regex,
                    currentOptions.ResponseReadTimeout,
                    cancellationToken)
                .ConfigureAwait(false);
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
            return MagnetSearchOutcome.Failure(MagnetSearchResult.RetrievalFailed, e.Message);
        }
    }

    private static async Task<MagnetSearchOutcome> FindMagnetUriInResponseAsync(
        HttpResponseMessage response,
        Regex regex,
        TimeSpan bodyReadTimeout,
        CancellationToken cancellationToken)
    {
        using var readTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readTimeoutCts.CancelAfter(bodyReadTimeout);

        MagnetSearchOutcome? outcome;
        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(readTimeoutCts.Token).ConfigureAwait(false);

            outcome = await FindMagnetUriInStreamAsync(stream, regex, readTimeoutCts.Token).ConfigureAwait(false);
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
        Regex regex,
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
                var outcome = FindMagnetUriInBytes(bytes, regex);
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

    private static MagnetSearchOutcome? FindMagnetUriInBytes(ReadOnlySpan<byte> bytes, Regex regex)
    {
        var charBuffer = ArrayPool<char>.Shared.Rent(bytes.Length);
        try
        {
            var chars = charBuffer.AsSpan(0, bytes.Length);
            if (!Encoding.UTF8.TryGetChars(bytes, chars, out var charsWritten) ||
                !regex.TryGetFirstMatch(chars[..charsWritten], out var magnetRange))
            {
                return null;
            }

            // A pattern whose quantifiers are all optional matches an empty string, which is neither
            // a magnet link nor a reason to stop looking for one.
            var match = chars[magnetRange];
            if (match.IsEmpty)
                return null;

            var matchText = new string(match);
            return Uri.TryCreate(matchText, UriKind.Absolute, out var magnetUri) &&
                magnetUri.Scheme == _magnetScheme
                    ? MagnetSearchOutcome.Found(magnetUri)
                    : MagnetSearchOutcome.Failure(
                        MagnetSearchResult.InvalidSelector,
                        $"'{matchText}' is not a magnet link. Check the magnet regex.");
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

    private static bool TryGetMagnetRegex(
        TorrentWebPageClientOptions currentOptions,
        string? regexPattern,
        [NotNullWhen(true)] out Regex? magnetRegex,
        [NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrEmpty(regexPattern))
        {
            magnetRegex = currentOptions.DefaultMagnetRegex;
            error = null;
            return true;
        }

        if (!TorrentRegex.IsFindMagnetRegex().IsMatch(regexPattern))
        {
            magnetRegex = null;
            error = $"Invalid magnet-matching regex provided. The value must match '{TorrentRegex.IsFindMagnet}'.";
            return false;
        }

        try
        {
            magnetRegex = RegexUtils.CreateInterpretedRegex(regexPattern, currentOptions.RegexMatchTimeout);
            error = null;
            return true;
        }
        catch (RegexParseException e)
        {
            magnetRegex = null;
            error = e.Message;
            return false;
        }
    }
}
