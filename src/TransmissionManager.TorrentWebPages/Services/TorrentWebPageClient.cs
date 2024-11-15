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

        var byteBuffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        try
        {
            var read = 0;
            while ((read = await stream.ReadAsync(byteBuffer.AsMemory(_keepFromLastBuffer), cancellationToken)
                .ConfigureAwait(false)) > 0)
            {
                var bytesToSearchIn = byteBuffer.AsSpan(0, _keepFromLastBuffer + read);
                var indexOfMagnet = bytesToSearchIn.IndexOfStartOf("magnet:?"u8);
                if (indexOfMagnet is -1)
                {
                    bytesToSearchIn[^_keepFromLastBuffer..].CopyTo(byteBuffer);
                    continue;
                }

                if (indexOfMagnet >= _keepFromLastBuffer * 4)
                {
                    var bytesToShiftToStart = bytesToSearchIn[(indexOfMagnet - _keepFromLastBuffer)..];
                    var shiftedBytesLength = bytesToShiftToStart.Length;
                    bytesToShiftToStart.CopyTo(byteBuffer);
                    read = await stream.ReadAsync(byteBuffer.AsMemory(shiftedBytesLength), cancellationToken)
                        .ConfigureAwait(false);

                    bytesToSearchIn = byteBuffer.AsSpan(0, shiftedBytesLength + read);
                }

                var charBuffer = ArrayPool<char>.Shared.Rent(bytesToSearchIn.Length);
                try
                {
                    var chars = charBuffer.AsSpan(0, bytesToSearchIn.Length);
                    if (Encoding.UTF8.TryGetChars(bytesToSearchIn, chars, out var charsWritten) &&
                        regex.TryGetFirstMatch(chars[..charsWritten], out var magnetRange))
                    {
                        return new(chars[magnetRange].ToString());
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(charBuffer);
                }

                bytesToSearchIn[^_keepFromLastBuffer..].CopyTo(byteBuffer);
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
