using Microsoft.Extensions.Options;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using TransmissionManager.TorrentWebPages.Constants;
using TransmissionManager.TorrentWebPages.Options;
using TransmissionManager.TorrentWebPages.Utils;

namespace TransmissionManager.TorrentWebPages.Services;

public sealed class TorrentWebPageClient(IOptionsMonitor<TorrentWebPageClientOptions> options, HttpClient httpClient)
{
    private const int _bufferSize = 2048;
    private const int _keepFromLastBuffer = _bufferSize / 16;

    public async Task<string?> FindMagnetUriAsync(
        Uri torrentWebPageUri,
        [StringSyntax(StringSyntaxAttribute.Regex)] string? regexPattern = null,
        CancellationToken cancellationToken = default)
    {
        var regex = GetMagnetSearchRegexWithValidation(regexPattern);

        using var stream = await httpClient.GetStreamAsync(torrentWebPageUri, cancellationToken).ConfigureAwait(false);

        using var bytesOwner = MemoryPool<byte>.Shared.Rent(_bufferSize);
        int read = 0;
        while ((read = await stream.ReadAsync(bytesOwner.Memory[_keepFromLastBuffer..], cancellationToken)) > 0)
        {
            var bytes = bytesOwner.Memory[..(_keepFromLastBuffer + read)];
            var indexOfMagnet = IndexOfStartOf(bytes.Span, "magnet:?"u8);
            if (indexOfMagnet is -1)
            {
                bytes[^_keepFromLastBuffer..].CopyTo(bytes);
                continue;
            }

            if (indexOfMagnet >= _keepFromLastBuffer * 4)
            {
                var toShiftToStart = bytes[(indexOfMagnet - _keepFromLastBuffer)..];
                toShiftToStart.CopyTo(bytes);
                var toFill = bytesOwner.Memory[toShiftToStart.Length..];
                read = await stream.ReadAsync(toFill, cancellationToken);
                bytes = bytesOwner.Memory[..(toShiftToStart.Length + read)];
            }

            using var charsOwner = MemoryPool<char>.Shared.Rent(_bufferSize);
            var chars = charsOwner.Memory;
            if (Encoding.UTF8.TryGetChars(bytes.Span, chars.Span, out var charsWritten) &&
                TryGetFirstRegexMatch(regex, chars.Span[..charsWritten], out var magnetRange))
            {
                return chars[magnetRange].ToString();
            }

            bytes[^_keepFromLastBuffer..].CopyTo(bytes);
        }

        return null;
    }

    private Regex GetMagnetSearchRegexWithValidation(string? regexPattern)
    {
        if (regexPattern is null)
            return options.CurrentValue.DefaultMagnetRegex;

        if (!TorrentRegex.IsFindMagnetRegex().IsMatch(regexPattern))
            throw new ArgumentException(
                $"Invalid magnet-matching regex provided. The value must match '{TorrentRegex.IsFindMagnet}'.",
                nameof(regexPattern));

        return RegexUtils.CreateRegex(regexPattern, options.CurrentValue.RegexMatchTimeout);
    }

    private static int IndexOfStartOf(ReadOnlySpan<byte> span, ReadOnlySpan<byte> value)
    {
        var index = span.IndexOf(value);
        if (index is not -1)
            return index;

        for (int i = 1; i < value.Length; i++)
        {
            var valueStart = value[..^i];
            if (span.EndsWith(valueStart))
                return span.Length - valueStart.Length;
        }

        return -1;
    }

    private static bool TryGetFirstRegexMatch(Regex regex, ReadOnlySpan<char> span, out Range matchRange)
    {
        foreach (var match in regex.EnumerateMatches(span))
        {
            matchRange = new(match.Index, match.Index + match.Length);
            return true;
        }

        matchRange = Range.EndAt(0);
        return false;
    }
}
