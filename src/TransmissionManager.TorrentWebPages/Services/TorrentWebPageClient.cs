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
    private const int _keepFromLastBuffer = _bufferSize / 16;

    public async Task<Uri?> FindMagnetUriAsync(
        Uri torrentWebPageUri,
        [StringSyntax(StringSyntaxAttribute.Regex)] string? regexPattern = null,
        CancellationToken cancellationToken = default)
    {
        var regex = GetMagnetRegex(regexPattern);

        using var stream = await httpClient.GetStreamAsync(torrentWebPageUri, cancellationToken).ConfigureAwait(false);

        var bytesBuffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        char[]? charsBuffer = null;
        try
        {
            var bytes = bytesBuffer.AsMemory();
            var read = 0;
            while ((read = await stream.ReadAsync(bytes[_keepFromLastBuffer..], cancellationToken).ConfigureAwait(false)) > 0)
            {
                var bytesToSearchIn = bytes[..(_keepFromLastBuffer + read)];
                var indexOfMagnet = ((ReadOnlySpan<byte>)bytesToSearchIn.Span).IndexOfStartOf("magnet:?"u8);
                if (indexOfMagnet is -1)
                {
                    bytesToSearchIn[^_keepFromLastBuffer..].CopyTo(bytes);
                    continue;
                }

                if (indexOfMagnet >= _keepFromLastBuffer * 4)
                {
                    var bytesToShiftToStart = bytesToSearchIn[(indexOfMagnet - _keepFromLastBuffer)..];
                    bytesToShiftToStart.CopyTo(bytes);
                    read = await stream.ReadAsync(bytes[bytesToShiftToStart.Length..], cancellationToken)
                        .ConfigureAwait(false);

                    bytesToSearchIn = bytes[..(bytesToShiftToStart.Length + read)];
                }

                charsBuffer = ArrayPool<char>.Shared.Rent(_bufferSize);
                var chars = charsBuffer.AsMemory();
                if (Encoding.UTF8.TryGetChars(bytesToSearchIn.Span, chars.Span, out var charsWritten) &&
                    regex.TryGetFirstMatch(chars.Span[..charsWritten], out var magnetRange))
                {
                    return new(chars[magnetRange].ToString());
                }

                bytesToSearchIn[^_keepFromLastBuffer..].CopyTo(bytes);
            }

            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytesBuffer);
            if (charsBuffer is not null)
                ArrayPool<char>.Shared.Return(charsBuffer);
        }
    }

    private Regex GetMagnetRegex(string? regexPattern)
    {
        if (regexPattern is null)
            return options.CurrentValue.DefaultMagnetRegex;

        if (!TorrentRegex.IsFindMagnetRegex().IsMatch(regexPattern))
            throw new ArgumentException(
                $"Invalid magnet-matching regex provided. The value must match '{TorrentRegex.IsFindMagnet}'.",
                nameof(regexPattern));

        return RegexUtils.CreateRegex(regexPattern, options.CurrentValue.RegexMatchTimeout);
    }
}
