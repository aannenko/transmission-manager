using Microsoft.Extensions.Options;
using Polly;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using TransmissionManager.TorrentSources.Constants;
using TransmissionManager.TorrentSources.Dto;
using TransmissionManager.TorrentSources.Options;
using TransmissionManager.TorrentSources.Utils;

namespace TransmissionManager.TorrentSources.Services;

public sealed class TorrentWebPageClient(IOptionsMonitor<TorrentWebPageClientOptions> options, HttpClient httpClient)
{
    private const int _bufferSize = 2048;
    private const int _defaultPadding = _bufferSize / 16;
    private const int _maxBufferFreeSpace = _bufferSize / 8;

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

        if (!TryGetMagnetRegex(regexPattern, out var regex, out var regexError))
            return MagnetSearchOutcome.Failure(MagnetSearchResult.InvalidSelector, regexError);

        try
        {
            return await FindMagnetUriOnWebPageAsync(torrentWebPageUri, regex, cancellationToken).ConfigureAwait(false);
        }
        catch (RegexMatchTimeoutException e) when (regexPattern is not null)
        {
            return MagnetSearchOutcome.Failure(MagnetSearchResult.InvalidSelector, e.Message);
        }
        catch (Exception e) when (e is
            HttpRequestException or
            IOException or // a connection dropped mid-body surfaces here, not as HttpRequestException
            ExecutionRejectedException) // Polly: attempt/total timeout, open circuit, rate limiter
        {
            return MagnetSearchOutcome.Failure(MagnetSearchResult.RetrievalFailed, e.Message);
        }
        catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            return MagnetSearchOutcome.Failure(MagnetSearchResult.RetrievalFailed, e.Message);
        }
    }

    private async Task<MagnetSearchOutcome> FindMagnetUriOnWebPageAsync(
        Uri torrentWebPageUri,
        Regex regex,
        CancellationToken cancellationToken)
    {
        // ResponseHeadersRead keeps the body streamed rather than buffered; the response must
        // therefore outlive the scan, so it is disposed after the stream it owns.
        using var response = await httpClient
            .GetAsync(torrentWebPageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return MagnetSearchOutcome.Failure(
                MagnetSearchResult.RetrievalFailed,
                $"The server responded with {(int)response.StatusCode} {response.StatusCode}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        var magnetUri = await FindMagnetUriInStreamAsync(stream, regex, cancellationToken).ConfigureAwait(false);

        return magnetUri is null
            ? MagnetSearchOutcome.Failure(MagnetSearchResult.NotFound, "No magnet link was found on the page")
            : MagnetSearchOutcome.Found(magnetUri);
    }

    private static async Task<Uri?> FindMagnetUriInStreamAsync(
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

                // magnet found too deep in the buffer - shift the magnet to the start of the buffer + padding
                if (indexOfMagnet >= _defaultPadding * 4)
                {
                    _ = await reader.ReadNextAsync(bytes.Length - indexOfMagnet + _defaultPadding, cancellationToken)
                        .ConfigureAwait(false);

                    bytes = reader.Bytes;
                    indexOfMagnet = bytes.IndexOf(Magnet);
                }

                // magnet found, but may not match the regex - ensure it matches the regex and return it
                var magnetUri = FindMagnetUriInBytes(bytes, regex);
                if (magnetUri is not null)
                    return magnetUri;

                // magnet not found, but there may be more to read - continue
                padding = bytes.Length - indexOfMagnet - Magnet.Length;
            }

            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuffer);
        }
    }

    private static Uri? FindMagnetUriInBytes(ReadOnlySpan<byte> bytes, Regex regex)
    {
        var charBuffer = ArrayPool<char>.Shared.Rent(bytes.Length);
        try
        {
            var chars = charBuffer.AsSpan(0, bytes.Length);
            if (Encoding.UTF8.TryGetChars(bytes, chars, out var charsWritten) &&
                regex.TryGetFirstMatch(chars[..charsWritten], out var magnetRange))
            {
                return new(new string(chars[magnetRange]));
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(charBuffer);
        }

        return null;
    }

    private bool TryGetMagnetRegex(
        string? regexPattern,
        [NotNullWhen(true)] out Regex? magnetRegex,
        [NotNullWhen(false)] out string? error)
    {
        if (regexPattern is null)
        {
            magnetRegex = options.CurrentValue.DefaultMagnetRegex;
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
            magnetRegex = RegexUtils.CreateRegex(regexPattern, options.CurrentValue.RegexMatchTimeout);
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
