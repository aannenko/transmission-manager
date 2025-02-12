using Microsoft.Extensions.Options;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using TransmissionManager.TorrentWebPages.Constants;
using TransmissionManager.TorrentWebPages.Extensions;
using TransmissionManager.TorrentWebPages.Options;
using TransmissionManager.TorrentWebPages.Utils;

namespace TransmissionManager.TorrentWebPages.Services;

public sealed class TorrentWebPageClient(IOptionsMonitor<TorrentWebPageClientOptions> options, HttpClient httpClient)
{
    private const int _bufferSize = 2048;
    private const int _defaultPadding = _bufferSize / 16;

    private static ReadOnlySpan<byte> Magnet => "magnet:?"u8;

    public async Task<Uri?> FindMagnetUriAsync(
        Uri torrentWebPageUri,
        [StringSyntax(StringSyntaxAttribute.Regex)] string? regexPattern = null,
        CancellationToken cancellationToken = default)
    {
        var regex = GetMagnetRegex(regexPattern);

        using var stream = await httpClient.GetStreamAsync(torrentWebPageUri, cancellationToken).ConfigureAwait(false);
        var byteBuffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        var reader = new PaddedBytesReader(stream, byteBuffer);
        try
        {
            var padding = 0;
            while (await reader.ReadNextAsync(padding, cancellationToken).ConfigureAwait(false))
            {
                var bytes = reader.Bytes;
                var indexOfMagnet = bytes.IndexOf(Magnet);
                if (indexOfMagnet is -1)
                {
                    padding = _defaultPadding;
                    continue;
                }

                if (indexOfMagnet >= _defaultPadding * 4)
                {
                    await reader.ReadNextAsync(bytes.Length - indexOfMagnet + _defaultPadding, cancellationToken)
                        .ConfigureAwait(false);

                    bytes = reader.Bytes;
                }

                var charBuffer = ArrayPool<char>.Shared.Rent(bytes.Length);
                try
                {
                    var chars = charBuffer.AsSpan();
                    if (Encoding.UTF8.TryGetChars(bytes, chars, out var charsWritten) &&
                        regex.TryGetFirstMatch(chars[..charsWritten], out var magnetRange))
                    {
                        return new(chars[magnetRange].ToString());
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(charBuffer);
                }

                padding = bytes.Length - indexOfMagnet - Magnet.Length;
            }

            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuffer);
        }
    }

    private Regex GetMagnetRegex(string? regexPattern)
    {
        if (regexPattern is null)
            return options.CurrentValue.DefaultMagnetRegex;

        if (TorrentRegex.IsFindMagnetRegex().IsMatch(regexPattern))
            return RegexUtils.CreateRegex(regexPattern, options.CurrentValue.RegexMatchTimeout);

        throw new ArgumentException(
            $"Invalid magnet-matching regex provided. The value must match '{TorrentRegex.IsFindMagnet}'.",
            nameof(regexPattern));
    }
}
